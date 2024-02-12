using MathNet.Numerics.LinearAlgebra.Complex;

namespace Math.topology.distance;

//write a function that computes mahalnobis distance
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using System;
using Accord.Statistics;

public static class SatisticsGaussianMeasures
{
    public static Vector<double> Mean(this Matrix<double> X)
    {
        return X.ColumnSums() / X.RowCount;
    }
    
    public static Matrix<double> CovarianceMatrix(this Matrix<double> X)
    {
        var mean = Mean(X);
        var Xm = X.ToRowArrays()
                                       .Select(r => Vector<double>.Build.DenseOfArray(r) - mean);

        var Xf = Matrix<double>.Build.DenseOfColumnVectors(Xm);
        var C = Xf.Transpose() * Xf/ Xf.RowCount;
        return C;
    }

    public static Matrix<double> Correlation(this Matrix<double> X)
    {
        return Matrix<double>.Build.DenseOfColumnArrays(Measures.Correlation(X.ToColumnArrays()));
    }
    
   
}
public class MahalanobisDistance
{
    private Matrix<double> C, Cinv, rho;
    private Vector<double> mu;
    public MahalanobisDistance(Matrix<double> Xs)
    {
        this.C = Xs.CovarianceMatrix();
        this.Cinv = C.Inverse();
        this.rho = Xs.Correlation();
        this.mu = Xs.Mean();
    }
    public double Calculate(Vector<double> x, Vector<double> y)
    {
        var d = x - y;
        var dC = d * Cinv * d;
        return Math.Sqrt(dC);
    }
    
    public IEnumerable<Vector<double>> RemoveAnomalies(IEnumerable<Vector<double>> Xsamples,double quantile)
    {
       
        var d = Xsamples.Select(x => Calculate(x, mu)).ToArray();
        var chi2 = new Accord.Statistics.Distributions.Univariate.ChiSquareDistribution(d.Length);
        var filtered = d
                .Select((v,idx) => new {distance=v, prob= chi2.ProbabilityDensityFunction(v),index=idx})
                .Where(s=> s.prob<=quantile)
                .Select((v,_)=>Xsamples.ElementAt(v.index));

        return filtered.ToArray();
    }
}