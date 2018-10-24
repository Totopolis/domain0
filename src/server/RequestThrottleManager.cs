using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Domain0.Nancy.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Extensions;
using NLog;

namespace Domain0.Service.Throttling
{
    public enum ThrottlingPeriod
    {
        Second,
        Minute,
        Hour,
        Day
    }

    [Flags]
    public enum ThrottlingProperties
    {
        None,
        RemoteIp,
        Path,
        Method
    }

    public interface IRequestThrottleManager
    {
        void RequiresThrottlingByPathAndIp(
            IPipelines pipeline,
            ThrottlingPeriod period,
            int requestCountLimit);

        void RequiresThrottling(
            IPipelines pipelines,
            ThrottlingProperties propertiesSet,
            ThrottlingPeriod period,
            int requestCountLimit,
            params string[] requestKeys);

        void RequiresThrottlingByPathAndIp(
            INancyModule module, 
            ThrottlingPeriod period, 
            int requestCountLimit);

        void RequiresThrottling(
            INancyModule module,
            ThrottlingProperties propertiesSet,
            ThrottlingPeriod period,
            int requestCountLimit,
            params string[] requestKeys);
    }

    public class RequestThrottleManager : IRequestThrottleManager
    {
        public RequestThrottleManager(
            IMemoryCache memoryCache,
            ILogger loggerInstance)
        {
            cache = memoryCache;
            logger = loggerInstance;
        }

        public void RequiresThrottlingByPathAndIp(
            IPipelines pipeline, 
            ThrottlingPeriod period, 
            int requestCountLimit)
        {
            RequiresThrottling(
                pipeline,
                ThrottlingProperties.RemoteIp
                | ThrottlingProperties.Method
                | ThrottlingProperties.Path,
                period,
                requestCountLimit);
        }

        public void RequiresThrottling(
            IPipelines pipelines, 
            ThrottlingProperties propertiesSet, 
            ThrottlingPeriod period,
            int requestCountLimit, 
            params string[] requestKeys)
        {
            pipelines.BeforeRequest.AddItemToStartOfPipeline(
                ctx => CheckThrottlingLimitHook(ctx, propertiesSet, period, requestCountLimit, requestKeys));
        }

        public void RequiresThrottlingByPathAndIp(
            INancyModule module,
            ThrottlingPeriod period,
            int requestCountLimit)
        {
            RequiresThrottling(
                module,
                ThrottlingProperties.RemoteIp
                | ThrottlingProperties.Method
                | ThrottlingProperties.Path,
                period,
                requestCountLimit);
        }

        public void RequiresThrottling(
            INancyModule module,
            ThrottlingProperties propertiesSet,
            ThrottlingPeriod period,
            int requestCountLimit,
            params string[] requestKeys)
        {
            module.AddBeforeHookOrExecute(
                ctx => CheckThrottlingLimitHook(ctx, propertiesSet, period, requestCountLimit, requestKeys), 
                "RequiresThrottling");
        }

        private Response CheckThrottlingLimitHook(
            NancyContext context, 
            ThrottlingProperties propertiesSet,
            ThrottlingPeriod period,
            int requestCountLimit,
            params string[] requestKeys)
        {
            var key = BuildRequestKey(propertiesSet, period, context, requestKeys);
            var expirationTime = CalculateExpirationTime(period);

            var counterValue = IncrementCounter(key, expirationTime);

            if (IsLimitExceeded(requestCountLimit, counterValue))
            {
                logger.Error($"Flood detected ({counterValue} in {period.ToString()} allowed {requestCountLimit})" +
                             $" on path: {context?.Request?.Path}" +
                             $" ip: {context?.GetClientHost()}");
                return new Response
                {
                    StatusCode = HttpStatusCode.TooManyRequests
                };
            }

            return null;
        }

        private class RequestCounter
        {
            public DateTime Start;

            public long Count;
        }

        private long IncrementCounter(object key, TimeSpan expiration)
        {
            var newCounter = new RequestCounter
            {
                Start = DateTime.Now,
                Count = 0
            };

            var counter = cache.GetOrCreate(key, 
                entry =>
                {
                    entry
                        .SetAbsoluteExpiration(newCounter.Start.Add(expiration))
                        .SetSize(32);

                    return newCounter;
                });

            Interlocked.Increment(ref counter.Count);

            return counter.Count;
        }

        private static string BuildRequestKey(
            ThrottlingProperties propertiesSet,
            ThrottlingPeriod period,
            NancyContext context,
            params string[] requestKeys)
        {
            using (var ms = new MemoryStream())
            using (var sw = new StreamWriter(ms, Encoding.UTF8))
            {
                if (propertiesSet.HasFlag(ThrottlingProperties.RemoteIp))
                {
                    sw.Write(context.GetClientHost());
                }

                if (propertiesSet.HasFlag(ThrottlingProperties.Method))
                {
                    sw.Write(context.Request.Method);
                }

                if (propertiesSet.HasFlag(ThrottlingProperties.Path))
                {
                    sw.Write(context.Request.Path);
                }

                sw.Write(period);

                if (requestKeys != null)
                {
                    foreach (var key in requestKeys)
                    {
                        sw.Write(key);
                    }
                }

                sw.Flush();
                ms.Position = 0;
                using (var algorithm = new SHA1Managed())
                {
                    var hash = algorithm.ComputeHash(ms);
                    return Convert.ToBase64String(hash);
                }
            }
        }

        private static bool IsLimitExceeded(int requestCountLimit, long counterValue)
        {
            if (counterValue > requestCountLimit)
            {
                return true;
            }

            return false;
        }

        private static TimeSpan CalculateExpirationTime(ThrottlingPeriod period)
        {
            switch (period)
            {
                case ThrottlingPeriod.Second:
                    return TimeSpan.FromSeconds(1);

                case ThrottlingPeriod.Minute:
                    return TimeSpan.FromMinutes(1);

                case ThrottlingPeriod.Hour:
                    return TimeSpan.FromHours(1);

                case ThrottlingPeriod.Day:
                    return TimeSpan.FromDays(1);

                default:
                    throw new ArgumentException(period.ToString());
            }
        }

        private readonly IMemoryCache cache;
        private readonly ILogger logger;
    }
}