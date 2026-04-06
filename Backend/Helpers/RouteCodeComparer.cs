namespace Backend.Helpers;

public sealed class RouteCodeComparer : IComparer<string>
{
    public static RouteCodeComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        var xIsNumber = int.TryParse(x, out var xRouteNumber);
        var yIsNumber = int.TryParse(y, out var yRouteNumber);

        if (xIsNumber && yIsNumber)
        {
            return xRouteNumber.CompareTo(yRouteNumber);
        }

        if (xIsNumber)
        {
            return -1;
        }

        if (yIsNumber)
        {
            return 1;
        }

        return StringComparer.OrdinalIgnoreCase.Compare(x, y);
    }
}
