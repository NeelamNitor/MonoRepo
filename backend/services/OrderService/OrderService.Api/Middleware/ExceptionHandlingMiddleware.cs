using FluentValidation;
using OrderService.Domain.Exceptions;

namespace OrderService.Api.Middleware;

/// <summary>Maps domain/validation exceptions to the HTTP status codes documented in
/// contracts/order-service.openapi.yaml (400 validation, 404 not found, 409 conflict).</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblem(context, StatusCodes.Status400BadRequest, "Validation failed",
                ex.Errors.Select(e => e.ErrorMessage));
        }
        catch (DomainException ex)
        {
            await WriteProblem(context, StatusCodes.Status400BadRequest, "Invalid request", new[] { ex.Message });
        }
        catch (NotFoundException ex)
        {
            await WriteProblem(context, StatusCodes.Status404NotFound, "Not found", new[] { ex.Message });
        }
        catch (InvalidOrderStatusTransitionException ex)
        {
            await WriteProblem(context, StatusCodes.Status409Conflict, "Invalid status transition", new[] { ex.Message });
        }
        catch (InsufficientStockException ex)
        {
            await WriteProblem(context, StatusCodes.Status409Conflict, "Insufficient stock", new[] { ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteProblem(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred", Array.Empty<string>());
        }
    }

    private static Task WriteProblem(HttpContext context, int statusCode, string title, IEnumerable<string> errors)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(new { title, status = statusCode, errors });
    }
}
