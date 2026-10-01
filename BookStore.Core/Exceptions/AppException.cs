namespace BookStore.Core.Exceptions;

// Throw this for expected business errors (409 email exists, 401 bad login, ...).
// ExceptionMiddleware turns it into a clean JSON response.
public class AppException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}