using System;
using CommandLine;
using CommandLine.Text;
using System.Windows.Input;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using qirvol;
using qirvol.volatility;

namespace qirvol.comands
{
    public interface ICommand
    {
        void Execute();
    }

   

    [Verb("calibrate", HelpText = "Calibrate a vol surface given a file")]
    public class CVolSurfaceData : ICommand
    {
        [Option('i', "input", Required = true, HelpText = "Path to the file including vol surface data")]
        public string? Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Path to the output folder where to serialize the results")]
        public string? Output { get; set; }

        [Option('T', "maturity", Required = true, HelpText = "maturity in years where to compute the smile")]
        public float Expiry { get; set; }

        [Option('t', "tenor", Required = true, HelpText = "Tenor in months where to compute the smile")]
        public int Tenor { get; set; }

        [Option('b', "beta", Required = true, HelpText = "Beta coefficient")]
        public float Beta { get; set; }

        [Option('l', "low_moneyness", Required = true, HelpText = "i.e. 20% is express are 0.8")]
        public float Low_moneyness { get; set; }

        [Option('h', "high_moneyness", Required = true, HelpText = "i.e. 20% is express are 1.2")]
        public float High_moneyness { get; set; }


        [Option('f', "forward", Required = true, HelpText = "forward spot value")]
        public float Fwd { get; set; }

        [Option('r', "resolution", Required = true, HelpText = "Number of strikes in the provided moneyness range")]
        public int Resolution { get; set; }



        public void Execute()
        {

            //Reading pillars from file ..
            var surface = VolSurface.from_csv(Input);
            var interpolator = new SABRInterpolator.SurfaceInterpolator(surface, Beta );
            var logK_f = Enumerable.Range(0, Resolution)
                            .Select(n => Low_moneyness + (High_moneyness - Low_moneyness) * n / (Resolution - 1))
                            .Select(x => Math.Log(x));
            var smile = interpolator.get_smile(Expiry, Tenor, logK_f.ToArray(), Fwd);

            smile.to_csv(Output);
            interpolator.SABRCube.to_csv(Output.Replace(".csv", "sabr.csv"));

        }
    }
}

