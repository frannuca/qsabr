using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Distributions;

namespace irmodels.models
{
	public class VasicekParams
	{
        public VasicekParams(double theta,double kappa,double sigma,double ro)
        {
            this.theta = theta;
            this.kappa = kappa;
            this.sigma = sigma;
            this.ro = ro;
        }
        readonly public double theta;
        readonly public double kappa;
        readonly public double sigma;
        readonly public double ro;        
	}

	public struct Pillar { public double t; public double value; }
	
	public class Vasicek:  IRModelBase<VasicekParams, Pillar[]>
	{

		public Vasicek(double timehorizon,uint N,uint nsims) : base(timehorizon,N,nsims) { }

        override public VasicekParams calibrate(Pillar[] x)
        {
            int N = x.Length;
            var dt = x[1].t - x[0].t;
            var r_v_total = Vector<double>.Build.DenseOfEnumerable(x.Select(p => p.value));
            var r_v_0 = r_v_total.SubVector(0, r_v_total.Count - 1);
            var r_v_1 = r_v_total.SubVector(1, r_v_total.Count - 1);
            var sum_rt_0 = r_v_0.Sum();
            var sum_rt_1 = r_v_1.Sum();
            var rt_0xrt_1 = r_v_0 * r_v_1;
            var ri2 = (r_v_0 * r_v_0);

            var a = (rt_0xrt_1 - 1.0 / (float)N  * sum_rt_0 * sum_rt_1) / (ri2 - 1.0 / (float)N  * sum_rt_0 * sum_rt_0);
            var b = 1.0 / (float)N  * (sum_rt_1 - a * sum_rt_0);

            var xkappa = (1 - a) / dt;
            var xtheta = b / (1 - a);
            var xro = r_v_total[0];
            var xsigma = MathNet.Numerics.Statistics.Statistics.StandardDeviation(r_v_1 - a * r_v_0 - b) / Math.Sqrt(dt);
            return new VasicekParams(kappa: xkappa, theta: xtheta, sigma: xsigma, ro: xro);       
        }

        override public Matrix<double> Run(VasicekParams x)
        {
            var norm = new Normal();
            int N = (int)this.N;
            
            double dt = this.thorizon / (this.N-1);
            double sqrt_dt = Math.Sqrt(dt);
            
            var path = Matrix<double>.Build.Dense((int)this.NSim,N);
            using (var file = new StreamWriter("./file.csv"))
                for (int i = 0; i < this.NSim; ++i)
                {
                    var rv = Vector<double>.Build.Dense(N);
                    rv[0] = x.ro;
                    for (int j = 1; j < N; ++j)
                    {            
                        rv[j] = rv[j - 1] + x.kappa * (x.theta - rv[j - 1]) * dt + x.sigma *sqrt_dt* norm.Sample();
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

