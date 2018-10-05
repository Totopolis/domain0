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
        public static void Enable(
            ILifetimeScope requestContainer, 
            IPipelines pipelines, 
            NancyContext context)
        {
            context.Trace.Items.Add("StartProcessingTime", DateTime.UtcNow);

            pipelines.OnError.AddItemToStartOfPipeline((ctx, ex) =>
            {
                return ProcessException(requestContainer, ctx, ex);
            });

            pipelines.OnError.AddItemToStartOfPipeline((ctx, ex) =>
            {
                Task.Run(() => LogRequest(ctx, requestContainer, MapExceptionToHttpCode(ex)));
                return null;
            });

            pipelines.AfterRequest.AddItemToEndOfPipeline(async ctx =>
            {
                await LogRequest(ctx, requestContainer);
            });
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

                var logEntry = new AccessLogEntry
                {
                    Action = ctx.Request.Path,
                    Method = ctx.Request.Method,
                    ClientIp = ctx.Request.UserHostAddress,
                    StatusCode = (int?)(statusCode ?? ctx.Response?.StatusCode),
                    ProcessedAt = DateTime.UtcNow,
                    UserAgent = string.Join(", ", ctx.Request.Headers["User-Agent"]),
                    UserId = ctx.CurrentUser?.Identity?.Name,
                    ProcessingTime = processingTime,
                    Referer = ctx.Request.Headers.Referrer
                };

                await repository.Insert(logEntry);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "LogRequest error");
            }
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
                        .WithMediaRangeModel("application/json", new List<ModelValidationError> { new ModelValidationError(binding.BoundType.Name, "couldnt deserialize") });
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
                case BadModelException _:
                    return HttpStatusCode.BadRequest;
                case NotFoundException _:
                    return HttpStatusCode.NotFound;
                case ModelBindingException _:
                    return HttpStatusCode.BadRequest;
                default:
                    return HttpStatusCode.InternalServerError;
            }
        }

    }
}