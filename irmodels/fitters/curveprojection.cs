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
		readonly protected Frame<DateTime, string> _factors;
        readonly protected Matrix<double> _matrix_factors;		
        readonly string[] _factor_order;

		public CurveProjection(Frame<DateTime,string> factors)
		{
			_factors = factors;			
            _factor_order = factors.ColumnKeys.ToArray();
			_matrix_factors = Matrix.ofFrame(_factors);
        }

		public Vector<double> series2vector(Series<string, double> x)
		{
			var a = x[_factor_order].Values.ToArray();
            return Vector<double>.Build.DenseOfArray(a);
		}

		/// <summary>
		/// computes the time series regression for the given coefficients.
		/// </summary>
		/// <param name="x">coefficients as a series that need to match the
		/// the risk factors in the internal _factors frame.</param>
		/// <returns></returns>
		public Series<DateTime,double> compute(Series<string,double> x)
		{
			var ts = _matrix_factors * series2vector(x);
			return new Series<DateTime,double>(_factors.RowKeys, ts.ToArray());
        }

		public double compute_error(Series<string, double> x, Series<DateTime, double> target)
		{
			var err = (compute(x) - target).Select(x => x.Value * x.Value).Sum();
			//Console.WriteLine(err);
			return err;
		}
        
	}
}

