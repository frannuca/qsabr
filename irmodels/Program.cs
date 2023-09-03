// See https://aka.ms/new-console-template for more information
using System.Formats.Asn1;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using irmodels.data;
using irmodels.models;
using Deedle;
using irmodels.fitters;

internal class Program
{
    private static void Main(string[] args)
    {
        var T = 20;
        var vs = new Vasicek(timehorizon: T, N: (uint)T * 12, nsims: 1000);

        var boundaries = new RegimeDefinition[]
        {
            new RegimeDefinition(lower_boundary:15,higher_boundary:20,"VIX"),
            new RegimeDefinition(lower_boundary:20,higher_boundary:2000,"VIX")
        };
        var mds = new CurveDataProvider(folderpath: "/Users/fran/Downloads/md",new string[] {"VIX","ESTR"});

        var optimmizer = new RegressionOptimizer(mds, boundaries);
        var cons = new Dictionary<string, (double, double)>();
        cons["VIX"] = (-100, 100);
        cons["ESTR"] = (-100, 100);
        
        var res = optimmizer.Fit("UBS",cons);
        Console.WriteLine(res);
        var cob = new DateTime(2023, 1, 1);
        var estr = mds["ESTR"].Where(p => p.date>= cob).ToArray();
        var ret_estr = Enumerable.Range(1, estr.Length - 1)             
            .Select(i => new Pillar { t = (float)i/360.0, value = estr[i].curve_value/100.0 }).ToArray();


        //var paramvasicek = vs.calibrate(ret_estr);
        //var coeff = vs.calibrate()
        //vs.Run(paramvasicek);

    }
}