namespace Backend.Helpers;

public sealed class NaturalRegionNameComparer : IComparer<string>
{
    public static NaturalRegionNameComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        var xIsNumber = decimal.TryParse(x, out var xNumber);
        var yIsNumber = decimal.TryParse(y, out var yNumber);

        if (xIsNumber && yIsNumber)
        {
            return xNumber.CompareTo(yNumber);
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
