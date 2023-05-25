using System;
using CommandLine;
using CommandLine.Text;
using System.Windows.Input;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using CsvHelper.Configuration.Attributes;
using qdata.csvextractor;
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
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Path to the output folder where to serialize the results")]
        public string Output { get; set; }

        [Option('r', "resolution", Required = true, HelpText = "number of strikes to compute")]
        public int NStrikes { get; set; }

        public void Execute()
        {

            //Reading pillars from file ..
            var volpillars = SABRInterpolator.get_surface_from_csv(Input);

            // Generation of the surface data ...
            var surface = new VolSurfaceBuilder().withPillars(volpillars).Build();


            // Resampling the surface with 1000  strike samples
            var (resampled_surface, sabrcube) = SABRInterpolator.get_cube_coeff_and_resampled_volsurface(surface, 0.5, -150.0, 150.0, 1000);


            resampled_surface.to_csv(System.IO.Path.Join(this.Output ,"resampled_300.csv"));
            sabrcube.to_csv(System.IO.Path.Join(this.Output,"sabrcube.csv"));

        }
    }
}

