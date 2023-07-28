using System;
using VolatilityService.Generated;
using System.Data;
using System.Linq;

namespace volatilityService.data
{
    static public class voldatatransformations
    {
        private static (string, System.Type)[] sabrcolumns = new (string, System.Type)[] {
            ("Tenor",typeof(float)), ("Expiry",typeof(float)), ("alpha",typeof(float)), ("beta",typeof(float)),
            ("nu",typeof(float)),  ("rho",typeof(float)) };

        private static (string, System.Type)[] volcolums = new (string, System.Type)[] {
            ("Tenor",typeof(float)), ("Expiry",typeof(float)), ("Fwd",typeof(float)), ("Strike",typeof(float)),
            ("Vol",typeof(float))};

        
        static public DataTable SABRcubetoTable(this SABRCube cube)
        {
            DataTable dt = new DataTable();
            foreach (var c in sabrcolumns)
            {
                dt.Columns.Add(c.Item1, c.Item2);
            }

            var generaterrow = (SABRPillar x) =>
            {
                var row = dt.NewRow();
                row["Tenor"] = x.TenorYears;
                row["Expiry"] = x.ExpiryYears;
                row["alpha"] = x.Alpha;
                row["beta"] = x.Beta;
                row["nu"] = x.Nu;
                row["rho"] = x.Rho;

                return row;
            };
            foreach (var row in from x in cube.Cube select generaterrow(x)) dt.Rows.Add(row);

            return dt;
        }

        static public DataTable VolSurfacetoTable(this VolSurface volsurface)
        {
            DataTable dt = new DataTable();
            foreach (var c in volcolums)
            {
                dt.Columns.Add(c.Item1, c.Item2);
            }

            var generaterrow = (SurfacePillar x) =>
            {
                var row = dt.NewRow();
                row["Tenor"] = x.TenorYears;
                row["Expiry"] = x.ExpiryYears;
                row["Fwd"] = x.Forward;
                row["Strike"] = x.Strike;
                row["Vol"] = x.Value;
                

                return row;
            };
            foreach (var row in from x in volsurface.Surface select generaterrow(x)) dt.Rows.Add(row);

            return dt;
        }
    }

    
}


