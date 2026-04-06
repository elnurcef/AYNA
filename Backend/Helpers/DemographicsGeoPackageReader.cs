using System.Buffers.Binary;
using System.Globalization;
using Backend.Models;
using Microsoft.Data.Sqlite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Backend.Helpers;

public static class DemographicsGeoPackageReader
{
    public static IReadOnlyList<DemographicsGeoPackageFeature> ReadFeatures(string geoPackagePath)
    {
        using var connection = new SqliteConnection($"Data Source={geoPackagePath};Mode=ReadOnly");
        connection.Open();

        var layerName = ExecuteScalar<string>(
            connection,
            "SELECT table_name FROM gpkg_contents WHERE data_type = 'features' LIMIT 1");

        var geometryColumnName = ExecuteScalar<string>(
            connection,
            $"SELECT column_name FROM gpkg_geometry_columns WHERE table_name = {QuoteLiteral(layerName)} LIMIT 1");

        var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {QuoteIdentifier(geometryColumnName)},
                   MICRO,
                   MESO,
                   MACRO,
                   population,
                   tot_jobs
            FROM {QuoteIdentifier(layerName)}
            """;

        using var reader = command.ExecuteReader();

        var wkbReader = new WKBReader();
        var features = new List<DemographicsGeoPackageFeature>();

        while (reader.Read())
        {
            var geometryBlob = (byte[])reader[0];
            var geometry = ReadGeometry(geometryBlob, wkbReader);

            features.Add(new DemographicsGeoPackageFeature(
                Micro: FormatMicroName(reader[1]),
                Meso: reader.GetString(2).Trim(),
                Macro: reader.GetString(3).Trim(),
                Population: reader.GetInt32(4),
                Jobs: reader.GetInt32(5),
                Geometry: geometry));
        }

        return features;
    }

    private static Geometry ReadGeometry(byte[] geoPackageGeometry, WKBReader wkbReader)
    {
        if (geoPackageGeometry.Length < 8 || geoPackageGeometry[0] != 0x47 || geoPackageGeometry[1] != 0x50)
        {
            throw new InvalidDataException("The geometry blob is not a valid GeoPackage binary geometry.");
        }

        var flags = geoPackageGeometry[3];
        var littleEndian = (flags & 0b0000_0001) == 0b0000_0001;
        var envelopeIndicator = (flags >> 1) & 0b0000_0111;
        var envelopeLength = GetEnvelopeLength(envelopeIndicator);
        var headerLength = 8 + envelopeLength;

        if (geoPackageGeometry.Length <= headerLength)
        {
            throw new InvalidDataException("The geometry blob does not contain a valid WKB payload.");
        }

        var srid = littleEndian
            ? BinaryPrimitives.ReadInt32LittleEndian(geoPackageGeometry.AsSpan(4, 4))
            : BinaryPrimitives.ReadInt32BigEndian(geoPackageGeometry.AsSpan(4, 4));

        var wkb = geoPackageGeometry.AsSpan(headerLength).ToArray();
        var geometry = wkbReader.Read(wkb);
        geometry.SRID = srid;

        return geometry;
    }

    private static int GetEnvelopeLength(int envelopeIndicator)
    {
        return envelopeIndicator switch
        {
            0 => 0,
            1 => 32,
            2 => 48,
            3 => 48,
            4 => 64,
            _ => throw new InvalidDataException($"Unsupported GeoPackage envelope indicator: {envelopeIndicator}.")
        };
    }

    private static T ExecuteScalar<T>(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = command.ExecuteScalar();

        if (result is null or DBNull)
        {
            throw new InvalidDataException($"Expected a scalar result for SQL: {sql}");
        }

        return (T)result;
    }

    private static string FormatMicroName(object value)
    {
        return value switch
        {
            double doubleValue when Math.Abs(doubleValue % 1) < double.Epsilon =>
                doubleValue.ToString("0", CultureInfo.InvariantCulture),
            double doubleValue =>
                doubleValue.ToString("0.################", CultureInfo.InvariantCulture),
            float floatValue when Math.Abs(floatValue % 1) < float.Epsilon =>
                floatValue.ToString("0", CultureInfo.InvariantCulture),
            float floatValue =>
                floatValue.ToString("0.################", CultureInfo.InvariantCulture),
            decimal decimalValue when decimal.Truncate(decimalValue) == decimalValue =>
                decimalValue.ToString("0", CultureInfo.InvariantCulture),
            decimal decimalValue =>
                decimalValue.ToString("0.################", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
        };
    }

    private static string QuoteIdentifier(string value)
    {
        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string QuoteLiteral(string value)
    {
        return $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
    }
}
