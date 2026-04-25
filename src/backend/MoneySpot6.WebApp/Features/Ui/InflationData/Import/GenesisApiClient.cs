using System.Collections.Immutable;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.Extensions.Options;

namespace MoneySpot6.WebApp.Features.Ui.InflationData.Import;

public class GenesisApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<InflationImportOptions> _options;

    public GenesisApiClient(HttpClient httpClient, IOptions<InflationImportOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<ImmutableArray<VpiDataPoint>> GetVpiData(CancellationToken cancellationToken = default)
    {
        var formData = new FormUrlEncodedContent(
        [
            new("name", "61111-0002"),
            new("area", "all"),
            new("format", "ffcsv"),
            new("language", "de"),
            new("startyear", "2000"),
            new("endyear", DateTimeOffset.UtcNow.Year.ToString()),
        ]);

        if (string.IsNullOrEmpty(_options.Value.GenesisApiToken))
            throw new Exception("No genesis api token configued");

        _httpClient.DefaultRequestHeaders.Add("username", _options.Value.GenesisApiToken);

        var response = await _httpClient.PostAsync("https://www-genesis.destatis.de/genesisWS/rest/2020/data/tablefile", formData, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var repsponseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var zip = new ZipArchive(repsponseStream, ZipArchiveMode.Read);

        var entry = zip.Entries.Single();
        using var csv = entry.Open();
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.NumberGroupSeparator = ".";
        using var reader = new CsvHelper.CsvReader(new StreamReader(csv), new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Encoding = Encoding.UTF8,
            Delimiter = ";",
            HasHeaderRecord = true,
        });

        if (!reader.Read() || !reader.ReadHeader())
            throw new Exception("CSV header could not be read");

        var r = ImmutableArray.CreateBuilder<VpiDataPoint>();
        await foreach (var csvEntry in reader.GetRecordsAsync<CsvEntry>())
        {
            if (csvEntry.Label != "Verbraucherpreisindex")
                continue;
            if (csvEntry.Value == "...")
                continue;

            r.Add(new VpiDataPoint
            {
                Year = csvEntry.Year,
                Month = int.Parse(csvEntry.Month.Substring("MONAT".Length)),
                Value = decimal.Parse(csvEntry.Value, culture)
            });
        }

        return r.ToImmutable();
    }

    public class CsvEntry
    {
        [Name("time")]
        public required int Year { get; init; }
        [Name("1_variable_attribute_code")]
        public required string Month { get; init; }
        [Name("value")]
        public required string Value { get; init; }
        [Name("value_variable_label")]
        public required string Label { get; init; }
    }

}

public class VpiDataPoint
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required decimal Value { get; init; }
}