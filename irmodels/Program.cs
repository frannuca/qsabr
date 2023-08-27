// See https://aka.ms/new-console-template for more information
using System.Formats.Asn1;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using irmodels.data;
using irmodels.models;

internal class Program
{
    private static void Main(string[] args)
    {
        var T = 20;
        var vs = new Vasicek(timehorizon: T, N: (uint)T * 12, nsims: 1000);

        var mds = new CsvMDS(path2folder: "/Users/fran/Downloads/md");
        var cob = new DateTime(2023, 1, 1);
        var estr = mds.Get("ESTR").Where(p => p.date>= cob).ToArray();
        var ret_estr = Enumerable.Range(1, estr.Length - 1)             
            .Select(i => new Pillar { t = (float)i/360.0, value = estr[i].quote/100.0 }).ToArray();
        var paramvasicek = vs.calibrate(ret_estr);
        //var coeff = vs.calibrate()
        vs.Run(paramvasicek);
    }
}