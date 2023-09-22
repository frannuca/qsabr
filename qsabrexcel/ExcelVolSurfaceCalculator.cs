using ExcelDna.Integration;
using MathNet.Numerics.Statistics;
using qirvol.volatility;
using System.Text;

namespace qsabrexcel
{
    using CsvHelper;
    using data;
    using System.Collections.Generic;
    using System.Globalization;

    public static class ExcelVolSurfaceCalculator
    {
        private static Dictionary<string, SABRInterpolator.SurfaceInterpolator> _volsufaces= new Dictionary<string, SABRInterpolator.SurfaceInterpolator>();
        
        private static string create_vol_surface_id() => "volsurface_"+ System.Guid.NewGuid().ToString();

        [ExcelFunction(Name = "VolSurfaceGenerator", IsVolatile = false)]
        public static string VolSurfaceGenerator(object[,] values,double beta)
        {
            try
            {
                if (ExcelDnaUtil.IsInFunctionWizard()) return "!!! In FunctionWizard";

                StringBuilder csvstr = new StringBuilder();
                int rows = values.GetLength(0);
                int cols = values.GetLength(1);
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        csvstr.Append(values[i, j].ToString());
                        if (j < cols - 1) csvstr.Append(",");
                    }
                    csvstr.AppendLine();
                }

                var reader = new StringReader(csvstr.ToString());
                var data = new CsvReader(reader, CultureInfo.InvariantCulture);
                IDictionary<double, IDictionary<double, VolPillar[]>> records =
                    data.GetRecords<CSV_VolSurface>()
                    .GroupBy(x => x.Expiry)
                    .ToDictionary(x => x.Key,
                                    x => (IDictionary<double, VolPillar[]>)x.GroupBy(y => (int)(y.Tenor * 12))
                                    .ToDictionary(a => a.Key,
                                                  b => b.Select(v => new VolPillar(tenor: (int)(v.Tenor * 12), strike: v.Strike, maturity: v.Expiry, volatility: v.Vol, forwardrate: v.Fwd)).ToArray()));

                var surface = new VolSurface(records);
                var key = create_vol_surface_id();
                var interp = new SABRInterpolator.SurfaceInterpolator(surface,beta);

                _volsufaces.Add(key, interp);
                return key;
            }
            catch(Exception e)
            {
                return "ERROR" + e.Message;
            }
        }

        [ExcelFunction(Name = "VolSurfaceInterpolator")]
        public static double VolSurfaceInterpolator(string handle,double T, int tenor, double K,double fwd)
        {
            if (ExcelDnaUtil.IsInFunctionWizard()) return 0.0;
            var interp = _volsufaces[handle];

            
            
            return interp.interpolate(T,tenor,K,fwd);
        }

        [ExcelFunction(Name = "SABR_Delta")]
        public static double SABR_Delta(string handle, double T, int tenor, double K, double fwd,double zerorate,bool isCall)
        {
            if (ExcelDnaUtil.IsInFunctionWizard()) return 0.0;
            var interp = _volsufaces[handle];



            return interp.Delta(T, tenor, K, fwd, zerorate, isCall);
        }

    }
}