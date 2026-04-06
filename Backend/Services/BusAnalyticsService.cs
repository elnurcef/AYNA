using System.Globalization;
using Backend.Helpers;
using Backend.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace Backend.Services;

public sealed class BusAnalyticsService : IBusAnalyticsService
{
    private readonly IReadOnlyList<BusAnalyticsCsvRow> _rows;

    public BusAnalyticsService(string csvFilePath, ILogger<BusAnalyticsService> logger)
    {
        if (!File.Exists(csvFilePath))
        {
            throw new FileNotFoundException("Bus analytics CSV file was not found.", csvFilePath);
        }

        using var stream = File.OpenRead(csvFilePath);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.Trim()
        });

        csv.Context.RegisterClassMap<BusAnalyticsCsvRowMap>();

        _rows = csv.GetRecords<BusAnalyticsCsvRow>().ToArray();

        logger.LogInformation(
            "Loaded {Count} bus analytics records from {CsvFilePath}",
            _rows.Count,
            csvFilePath);
    }

    public Task<PagedResult<BusAnalyticsItemDto>> GetAsync(
        BusAnalyticsQueryParameters query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<BusAnalyticsCsvRow> filteredRows = _rows;

        if (!string.IsNullOrWhiteSpace(query.Route))
        {
            filteredRows = filteredRows.Where(row =>
                string.Equals(row.Route, query.Route.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.OperatorName))
        {
            filteredRows = filteredRows.Where(row =>
                string.Equals(row.Operator, query.OperatorName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (query.Date is not null)
        {
            filteredRows = filteredRows.Where(row =>
                DateOnly.FromDateTime(row.Date.UtcDateTime) == query.Date.Value);
        }

        var orderedRows = ApplySorting(filteredRows, query.SortBy, query.SortDir);
        var total = orderedRows.Count();

        var items = orderedRows
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(row => new BusAnalyticsItemDto(
                Date: DateOnly.FromDateTime(row.Date.UtcDateTime),
                Hour: row.Hour,
                Route: row.Route,
                TotalCount: row.TotalCount,
                BySmartCard: row.BySmartCard,
                ByQr: row.ByQr,
                NumberOfBusses: row.NumberOfBusses,
                Operator: row.Operator))
            .ToArray();

        return Task.FromResult(new PagedResult<BusAnalyticsItemDto>(total, items));
    }

    private static IOrderedEnumerable<BusAnalyticsCsvRow> ApplySorting(
        IEnumerable<BusAnalyticsCsvRow> rows,
        string? sortBy,
        string? sortDir)
    {
        var normalizedSortBy = NormalizeSortBy(sortBy);
        var sortDescending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        return normalizedSortBy switch
        {
            "hour" => sortDescending
                ? rows.OrderByDescending(row => row.Hour)
                    .ThenByDescending(row => row.Date)
                    .ThenBy(row => row.Route, RouteCodeComparer.Instance)
                : rows.OrderBy(row => row.Hour)
                    .ThenBy(row => row.Date)
                    .ThenBy(row => row.Route, RouteCodeComparer.Instance),
            "route" => sortDescending
                ? rows.OrderByDescending(row => row.Route, RouteCodeComparer.Instance)
                    .ThenByDescending(row => row.Date)
                    .ThenByDescending(row => row.Hour)
                : rows.OrderBy(row => row.Route, RouteCodeComparer.Instance)
                    .ThenBy(row => row.Date)
                    .ThenBy(row => row.Hour),
            "totalcount" => sortDescending
                ? rows.OrderByDescending(row => row.TotalCount)
                    .ThenByDescending(row => row.Date)
                    .ThenByDescending(row => row.Hour)
                : rows.OrderBy(row => row.TotalCount)
                    .ThenBy(row => row.Date)
                    .ThenBy(row => row.Hour),
            "operator" => sortDescending
                ? rows.OrderByDescending(row => row.Operator, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(row => row.Date)
                    .ThenByDescending(row => row.Hour)
                : rows.OrderBy(row => row.Operator, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(row => row.Date)
                    .ThenBy(row => row.Hour),
            _ => sortDescending
                ? rows.OrderByDescending(row => row.Date)
                    .ThenByDescending(row => row.Hour)
                    .ThenBy(row => row.Route, RouteCodeComparer.Instance)
                : rows.OrderBy(row => row.Date)
                    .ThenBy(row => row.Hour)
                    .ThenBy(row => row.Route, RouteCodeComparer.Instance)
        };
    }

    private static string NormalizeSortBy(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return "date";
        }

        return sortBy.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToLowerInvariant();
    }
}
