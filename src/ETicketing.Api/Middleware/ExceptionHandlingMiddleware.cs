using ETicketing.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ETicketing.Api.Middleware;

/// <summary>
/// Turns exceptions into consistent ProblemDetails responses and logs them. Unknown failures
/// become a generic 500, so stack traces and internals never reach the client.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private const string ProblemContentType = "application/problem+json";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request {Method} {Path} was cancelled by the client", context.Request.Method, context.Request.Path);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var problem = ToProblemDetails(exception);
        problem.Instance = context.Request.Path;

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogWarning("{Method} {Path} rejected with {StatusCode}: {Reason}", context.Request.Method, context.Request.Path, problem.Status, exception.Message);
        }

        context.Response.StatusCode = problem.Status!.Value;
        await context.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, contentType: ProblemContentType);
    }

    private static ProblemDetails ToProblemDetails(Exception exception) => exception switch
    {
        ValidationException validation => ToValidationProblem(validation),
        UnsupportedPaymentMethodException => Problem(StatusCodes.Status400BadRequest, "Unsupported payment method", exception),
        NotFoundException => Problem(StatusCodes.Status404NotFound, "Resource not found", exception),
        TicketSoldOutException => Problem(StatusCodes.Status409Conflict, "Sold out", exception),
        PaymentFailedException => Problem(StatusCodes.Status409Conflict, "Payment conflict", exception),
        InvalidOrderStateException or InvalidTransactionStateException =>
            Problem(StatusCodes.Status422UnprocessableEntity, "Invalid state", exception),
        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Server error",
            Detail = "An unexpected error occurred."
        }
    };

    private static ProblemDetails Problem(int status, string title, Exception exception) => new()
    {
        Status = status,
        Title = title,
        Detail = exception.Message
    };

    private static ValidationProblemDetails ToValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed"
        };
    }
}
