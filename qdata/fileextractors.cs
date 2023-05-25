namespace qdata.csvextractor;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using CsvHelper.Configuration.Attributes;

public class VolSurfaceCsv				
{
    [Index(0)]
    public double Tenor { get; set; }

    [Index(1)]
    public double Expiry { get; set; }

    [Index(2)]
    public double Fwd { get; set; }

    [Index(3)]
    public double Strike { get; set; }

    [Index(4)]
    public double Vol { get; set; }
}
static public class VolSurfaceCsvHelper
{
    static public VolSurfaceCsv[] ExtractSurface(string csvfilepath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLower(),
        };

        var volpillars = new List<VolSurfaceCsv>();

        using (var reader = new StreamReader(csvfilepath))
        using (var csv = new CsvReader(reader, config))
        {
            volpillars.AddRange(csv.GetRecords<VolSurfaceCsv>());
        }
        return volpillars.ToArray();
    }
}

