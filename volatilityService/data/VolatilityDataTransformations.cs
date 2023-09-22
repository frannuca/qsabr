using VolatilityService.Generated;
using qirvol.volatility;

namespace volatilityService.data
{
    public static class VolatilityDataTransformations
    {
        public static VolSurface ToVolSurface(this PVolSurface cube)
        {
            var dt = new Dictionary<double, IDictionary<double, VolPillar[]>>();
            foreach(var x in cube.Surface)
            {
                var expiry = x.ExpiryYears;

                if (!dt.ContainsKey(expiry))
                {
                    IDictionary<double, VolPillar[]> aux = new Dictionary<double, VolPillar[]>();
                    dt[expiry] = aux ;
                }

                var tenor = Convert.ToInt32(x.TenorYears * 12);
                var pillar = new VolPillar(tenor: tenor, strike: x.Strike, maturity: x.ExpiryYears, volatility: x.Value, forwardrate: x.Forward);
                dt[expiry][tenor] = new [] { pillar };
            }

            return new VolSurface(dt);
        }

        public static SABRCube toSABRCube(this PSABRCube cube)
        {
            var dt = new Dictionary<double, IDictionary<double, SABRSolution[]>>();
            foreach (var x in cube.Cube)
            {
                var expiry = x.ExpiryYears;

                if (!dt.ContainsKey(expiry))
                {
                    dt[expiry] = new Dictionary<double, SABRSolution[]>();
                }

                var tenor = Convert.ToInt32(x.TenorYears * 12);
                var pillar = new SABRSolution(x.ExpiryYears,tenor,x.Alpha,x.Beta,x.Nu,x.Rho,f:x.Forward);
                dt[expiry][tenor] = new SABRSolution[] { pillar };
            }

            return new SABRCube(dt);
        }
        
        public static PSABRCube toPSBARCube(this SABRCube cube)
        {
            var psabr = new PSABRCube();
            
            foreach (var x in cube.Cube)
            {
                foreach (var y in x.Value)
                {
                    SABRSolution sol = y.Value.First();

                    psabr.Cube.Add(new PSABRPillar()
                    {
                        ExpiryYears = (float)x.Key,
                        TenorYears = (float)y.Key,
                        Alpha = (float)sol.alpha,
                        Beta = (float)sol.beta,
                        Forward = (float)sol.f,
                        Nu = (float)sol.nu,
                        Rho = (float)sol.rho
                    });
                }
                ;
            }
            return psabr;
        }
    }

    
}


