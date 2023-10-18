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
                    (IDictionary<double, IDictionary<double, VolPillar[]>>)data.GetRecords<CSV_VolSurface>()
                    .GroupBy(x => x.Expiry)
                    .ToDictionary(x => x.Key,
                                    x => (IDictionary<double, VolPillar[]>)x.GroupBy(y => (y.Tenor))
                                    .ToDictionary(a => a.Key,
                                                  b => b.Select(v => new VolPillar(tenor: (v.Tenor), strike: v.Strike, maturity: v.Expiry, volatility: v.Vol, forwardrate: v.Fwd)).ToArray()));

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

        [ExcelFunction(Name = "SABR_Coefficients")]
        public static object[,] SABR_Coefficients(string handle)
        {
            try
            {
                if (ExcelDnaUtil.IsInFunctionWizard()) return new object[0, 0];
                var interp = _volsufaces[handle];

                var frame = interp.SABRCube.getCoeffs();
                var coeffs = frame.ToArray2D<object>();
                var result = new object[coeffs.GetLength(0) + 1, coeffs.GetLength(1)];
                for(int i=0; i<coeffs.GetLength(0);++i)
                    for(int j=0;j<coeffs.GetLength(1);++j)
                        result[i + 1, j] = coeffs[i,j];

                for (int i = 0; i < frame.Columns.Keys.Count(); ++i) { result[0, i] = frame.Columns.Keys.ToList()[i].ToString(); }


                return result;
            }
            catch(Exception ex)
            {
                return new object[1, 1] { { ex.Message } };
            }
            
        }

    }
}