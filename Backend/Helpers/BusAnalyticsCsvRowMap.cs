using Backend.Models;
using CsvHelper.Configuration;

namespace Backend.Helpers;

public sealed class BusAnalyticsCsvRowMap : ClassMap<BusAnalyticsCsvRow>
{
    public BusAnalyticsCsvRowMap()
    {
        Map(row => row.Date).Name("Date");
        Map(row => row.Hour).Name("Hour");
        Map(row => row.Route).Name("Route");
        Map(row => row.TotalCount).Name("Total Count");
        Map(row => row.BySmartCard).Name("By SmartCard");
        Map(row => row.ByQr).Name("By QR");
        Map(row => row.NumberOfBusses).Name("Number Of Busses");
        Map(row => row.Operator).Name("Operator");
    }
}
