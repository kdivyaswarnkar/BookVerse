using BookStore.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BookStore.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log)
{
    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        // Expected business errors thrown from our own code (409, 401, ...)
        catch (AppException ex)
        {
            await Write(ctx, ex.StatusCode, ex.Message);
        }
        // Business errors raised by stored procedures (50001-50099)
        catch (SqlException ex) when (ex.Number is >= 50001 and <= 50099)
        {
            await Write(ctx, ex.Number == 50001 ? 409 : 400, ex.Message);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Unhandled error on {Path}", ctx.Request.Path);
            await Write(ctx, 500, "Something went wrong. Please try again.");
        }
    }

    private static Task Write(HttpContext ctx, int status, string message)
    {
        ctx.Response.StatusCode = status;
        return ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Detail = message
        });
    }
}