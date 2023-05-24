using System;
using CommandLine;
using CommandLine.Text;
using System.Windows.Input;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using CsvHelper.Configuration.Attributes;
using qirvol.volatility;


namespace qirvol.comands
{
    public interface ICommand
    {
        void Execute();
    }

    public class VolSurfaceCsv//				
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

    [Verb("calibrate", HelpText = "Calibrate a vol surface given a file")]
    public class CVolSurfaceData : ICommand
    {
        [Option('i', "input", Required = true, HelpText = "Path to the file including vol surface data")]
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Path to the output including vol surface results")]
        public string Output { get; set; }

        [Option('r', "resolution", Required = true, HelpText = "number of strikes to compute")]
        public int NStrikes { get; set; }

        public void Execute()
        {
            if (!System.IO.File.Exists(Input)) throw new Exception("Invalid path to market data");

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
            };

            var volsurfacebld = new VolSurfaceBuilder();
            var strikes_deltas = new HashSet<double>();

            using (var reader = new StreamReader(Input))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<VolSurfaceCsv>();
                foreach(var point in records)
                {
                    volsurfacebld.withPillar(new VolPillar((int)(point.Tenor * 12), point.Strike*1e-4+point.Fwd, point.Expiry,point.Vol, point.Fwd));
                    strikes_deltas.Add(point.Strike);
                }
            }


            var minK = strikes_deltas.Min();
            var maxK = strikes_deltas.Max();
            var highresStrikes = (double f) => Enumerable.Range(0, NStrikes)
                                                .Select(n => (minK + n * (maxK - minK) / (NStrikes - 1)))
                                                .Select(d=> f+d*1e-4).ToArray();

            var volsurface = volsurfacebld.Build();
            
            //Calling the calibration engine on the just built vol surface object
            // and plotting the optimium parmaters (1 smile in this case)
            var nu0 = 10.0;
            var rho0 = 0.2;
            var beta = 0.5;
            var res = SABR.sigma_calibrate(volsurface,nu0,rho0,beta);

            using (var outfile = new StreamWriter(Output))
            {
                outfile.WriteLine("Expiry,Tenor,Fwd,Strike,alpha,beta,nu,rho,Vol");
                foreach (var mat_frame in res)
                {
                    var texp = mat_frame.Key;
                    foreach (var kv in mat_frame.Value)
                    {
                        var tenor = kv.Key;
                        var param = kv.Value;

                        //var Ks = strikes_deltas.Select(d => d * 1e-4 + param.f).ToArray();
                        var strikes = highresStrikes(param.f);
                        var smile = strikes.Select(k => SABR.Sigma_SABR(param.alpha, param.beta, param.nu, param.texp, param.tenor, k, param.f));
                        
                        for (int i = 0; i < strikes.Length; ++i)
                        {
                            var v = SABR.Sigma_SABR(param.alpha, param.beta, param.nu, param.texp, param.rho, strikes[i], param.f);
                            outfile.WriteLine($"{texp},{tenor},{param.f},{strikes[i]},{param.alpha},{param.beta},{param.nu},{param.rho},{v}");
                        }


                    }
                }
            }


        }
    }
}

