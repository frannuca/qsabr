using System;
using Deedle;
using Deedle.Math;
using MathNet.Numerics.LinearAlgebra;
namespace irmodels.fitters
{
	/// <summary>
	/// Given a Frame with input risk factors this class allows the computation
	/// of a linearly regressed curve given a specific coefficient Series.
	/// The purpose of this class is to be used as part of a calibration super class.
	/// </summary>
	public class CurveProjection
	{
		readonly protected Frame<DateTime, string> Factors;
        readonly protected Matrix<double> MatrixFactors;		
        readonly string[] _factorOrder;

		public CurveProjection(Frame<DateTime,string> factors)
		{
			Factors = factors;			
            _factorOrder = factors.ColumnKeys.ToArray();
			MatrixFactors = Matrix.ofFrame(Factors);
        }

		public Vector<double> Series2Vector(Series<string, double> x)
		{
			var a = x[_factorOrder].Values.ToArray();
            return Vector<double>.Build.DenseOfArray(a);
		}

		/// <summary>
		/// computes the time series regression for the given coefficients.
		/// </summary>
		/// <param name="x">coefficients as a series that need to match the
		/// the risk factors in the internal _factors frame.</param>
		/// <returns></returns>
		public Series<DateTime,double> Compute(Series<string,double> x)
		{
			var ts = MatrixFactors * Series2Vector(x);
			return new Series<DateTime,double>(Factors.RowKeys, ts.ToArray());
        }

		public double compute_error(Series<string, double> x, Series<DateTime, double> target)
		{
			var err = (Compute(x) - target).Select(x => x.Value * x.Value).Sum();
			//Console.WriteLine(err);
			return err;
		}
        
	}
}

