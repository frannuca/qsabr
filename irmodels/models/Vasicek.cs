using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Distributions;

namespace irmodels.models
{
	public class VasicekParams
	{
        public VasicekParams(double theta,double kappa,double sigma,double ro)
        {
            this.Theta = theta;
            this.Kappa = kappa;
            this.Sigma = sigma;
            this.Ro = ro;
        }
        readonly public double Theta;
        readonly public double Kappa;
        readonly public double Sigma;
        readonly public double Ro;        
	}

	public struct Pillar { public double T; public double Value; }
	
	public class Vasicek:  IrModelBase<VasicekParams, Pillar[]>
	{

		public Vasicek(double timehorizon,uint n,uint nsims) : base(timehorizon,n,nsims) { }

        override public VasicekParams Calibrate(Pillar[] x)
        {
            int n = x.Length;
            var dt = x[1].T - x[0].T;
            var rVTotal = Vector<double>.Build.DenseOfEnumerable(x.Select(p => p.Value));
            var rV0 = rVTotal.SubVector(0, rVTotal.Count - 1);
            var rV1 = rVTotal.SubVector(1, rVTotal.Count - 1);
            var sumRt0 = rV0.Sum();
            var sumRt1 = rV1.Sum();
            var rt0Xrt1 = rV0 * rV1;
            var ri2 = (rV0 * rV0);

            var a = (rt0Xrt1 - 1.0 / (float)n  * sumRt0 * sumRt1) / (ri2 - 1.0 / (float)n  * sumRt0 * sumRt0);
            var b = 1.0 / (float)n  * (sumRt1 - a * sumRt0);

            var xkappa = (1 - a) / dt;
            var xtheta = b / (1 - a);
            var xro = rVTotal[0];
            var xsigma = MathNet.Numerics.Statistics.Statistics.StandardDeviation(rV1 - a * rV0 - b) / Math.Sqrt(dt);
            return new VasicekParams(kappa: xkappa, theta: xtheta, sigma: xsigma, ro: xro);       
        }

        override public Matrix<double> Run(VasicekParams x)
        {
            var norm = new Normal();
            int n = (int)this.N;
            
            double dt = this.Thorizon / (this.N-1);
            double sqrtDt = Math.Sqrt(dt);
            
            var path = Matrix<double>.Build.Dense((int)this.NSim,n);
            using (var file = new StreamWriter("./file.csv"))
                for (int i = 0; i < this.NSim; ++i)
                {
                    var rv = Vector<double>.Build.Dense(n);
                    rv[0] = x.Ro;
                    for (int j = 1; j < n; ++j)
                    {            
                        rv[j] = rv[j - 1] + x.Kappa * (x.Theta - rv[j - 1]) * dt + x.Sigma *sqrtDt* norm.Sample();
                        file.Write(rv[j]);
                        file.Write(",");
                    }
                    file.WriteLine();
                    path.SetSubMatrix(i, 0, rv.ToRowMatrix());
                }


            return path;
        }
    }
}

