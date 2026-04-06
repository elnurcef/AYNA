namespace Backend.Models;

public sealed class LiveRouteProviderException : Exception
{
    public LiveRouteProviderException(string message)
        : base(message)
    {
    }

    public LiveRouteProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
