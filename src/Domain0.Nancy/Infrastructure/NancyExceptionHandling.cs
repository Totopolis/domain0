using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Domain0.Exceptions;
using Domain0.Repository;
using Domain0.Repository.Model;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.ModelBinding;
using Nancy.Responses.Negotiation;
using Nancy.Validation;
using NLog;

namespace Domain0.Nancy.Infrastructure
{
    public class NancyExceptionHandling
    {
        public const string SensitiveInfoReplacement = "***";

        public static void Enable(
            ILifetimeScope requestContainer, 
            IPipelines pipelines, 
            NancyContext context)
        {
            context.Trace.Items.Add("StartProcessingTime", DateTime.UtcNow);

            pipelines.OnError.AddItemToEndOfPipeline((ctx, ex) =>
            {
                Task.Run(() => LogRequest(ctx, requestContainer, MapExceptionToHttpCode(ex)));
                return null;
            });

            pipelines.OnError.AddItemToEndOfPipeline((ctx, ex) => ProcessException(requestContainer, ctx, ex));

            pipelines.AfterRequest.AddItemToEndOfPipeline((ctx, token) => LogRequest(ctx, requestContainer));
        }

        private static async Task LogRequest(
            NancyContext ctx,
            ILifetimeScope requestContainer,
            HttpStatusCode? statusCode = null)
        {
            var repository = requestContainer.Resolve<IAccessLogRepository>();
            var logger = requestContainer.Resolve<ILogger>();
            try
            {
                var processingTime = CalculateProcessingTime(ctx);

                var sensitiveInfoInPathRegex = ctx.GetRegexForSensitiveInfoInPath();
                var actionStr = sensitiveInfoInPathRegex != null
                    ? sensitiveInfoInPathRegex.Replace(ctx.Request.Path, SensitiveInfoReplacement)
                    : ctx.Request.Path;

                var logEntry = new AccessLogEntry
                {
                    Action = Truncate(actionStr, 255),
                    Method = Truncate(ctx.Request.Method, 15),
                    ClientIp = Truncate(ctx.GetClientHost(), 255),
                    StatusCode = (int?)(statusCode ?? ctx.Response?.StatusCode),
                    ProcessedAt = DateTime.UtcNow,
                    UserAgent = Truncate(string.Join(", ", ctx.Request.Headers["User-Agent"]), 255),
                    UserId = Truncate(ctx.CurrentUser?.Identity?.Name, 128),
                    ProcessingTime = processingTime,
                    Referer = Truncate(ctx.Request.Headers.Referrer, 255)
                };

                await repository.Insert(logEntry);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "LogRequest error");
            }
        }

        public static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private static int? CalculateProcessingTime(NancyContext ctx)
        {
            int? processingTime = null;
            if (ctx.Trace.Items.TryGetValue("StartProcessingTime", out object startTime))
            {
                processingTime = (int) (DateTime.UtcNow - (DateTime) startTime).TotalMilliseconds;
            }

            return processingTime;
        }

        private static dynamic ProcessException(
            ILifetimeScope requestContainer,
            NancyContext ctx,
            Exception ex)
        {
            switch (ex)
            {
                case ArgumentException bad:
                    return new Negotiator(ctx)
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithHeader("X-Status-Reason", "validation error")
                        .WithReasonPhrase("validation error")
                        .WithMediaRangeModel("application/json", new ModelValidationError (bad.ParamName, bad.Message));
                case BadModelException bad:
                    return new Negotiator(ctx)
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithHeader("X-Status-Reason", "validation error")
                        .WithReasonPhrase("validation error")
                        .WithMediaRangeModel("application/json", bad.ValidationResult.Errors.SelectMany(e => e.Value));
                case NotFoundException _:
                    return new Negotiator(ctx)
                        .WithStatusCode(HttpStatusCode.NotFound)
                        .WithReasonPhrase("not found error");
                case ModelBindingException binding:
                    return new Negotiator(ctx)
                        .WithStatusCode(HttpStatusCode.BadRequest)
                        .WithHeader("X-Status-Reason", "validation error")
                        .WithReasonPhrase("validation error")
                        .WithMediaRangeModel("application/json", new List<ModelValidationError> { new ModelValidationError(binding.BoundType.Name, "could`t deserialize") });
                case TokenSecurityException _:
                    return new Negotiator(ctx)
                        .WithStatusCode(HttpStatusCode.Unauthorized)
                        .WithReasonPhrase("no luck");
                case ForbiddenSecurityException _:
                    return new Negotiator(ctx)
                        .WithStatusCode(HttpStatusCode.Forbidden)
                        .WithReasonPhrase("have no rights");
                case UserLockedSecurityException _:
                    return new Negotiator(ctx)
                        .WithStatusCode(HttpStatusCode.Locked)
                        .WithReasonPhrase("User locked");
                default:
                    var logger = requestContainer.Resolve<ILogger>();
                    logger.Error(ex, "Unexpected exception");
                    return new Negotiator(ctx)
                        .WithStatusCode(HttpStatusCode.InternalServerError)
                        .WithReasonPhrase("unexpected exception");
            }
        }

        private static HttpStatusCode MapExceptionToHttpCode(Exception exception)
        {
            switch (exception)
            {
                case ArgumentException _:
                    return HttpStatusCode.BadRequest;
                case BadModelException _:
                    return HttpStatusCode.BadRequest;
                case NotFoundException _:
                    return HttpStatusCode.NotFound;
                case ModelBindingException _:
                    return HttpStatusCode.BadRequest;
                case TokenSecurityException _:
                    return HttpStatusCode.Unauthorized;
                case ForbiddenSecurityException _:
                    return HttpStatusCode.Forbidden;
                case UserLockedSecurityException _:
                    return HttpStatusCode.Locked;
                default:
                    return HttpStatusCode.InternalServerError;
            }
        }

    }
}