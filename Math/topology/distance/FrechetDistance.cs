namespace Math.topology.distance;
using System;

public class FrechetDistance
{
    private double EuclideanDistance(Tuple<double, double> point1, Tuple<double, double> point2)
    {
        return Math.Sqrt(Math.Pow(point1.Item1 - point2.Item1, 2) + Math.Pow(point1.Item2 - point2.Item2, 2));
    }

    //document this function
    private double CalculateRecursive(List<Tuple<double, double>> curve1, List<Tuple<double, double>> curve2, int i, int j, double[,] memo)
    {
        if (memo[i, j] > -1)
        {
            return memo[i, j];
        }

        if (i == 0 && j == 0)
        {
            memo[i, j] = EuclideanDistance(curve1[0], curve2[0]);
        }
        else if (i > 0 && j == 0)
        {
            memo[i, j] = Math.Max(CalculateRecursive(curve1, curve2, i - 1, 0, memo), EuclideanDistance(curve1[i], curve2[0]));
        }
        else if (i == 0 && j > 0)
        {
            memo[i, j] = Math.Max(CalculateRecursive(curve1, curve2, 0, j - 1, memo), EuclideanDistance(curve1[0], curve2[j]));
        }
        else if (i > 0 && j > 0)
        {
            double temp = Math.Min(Math.Min(CalculateRecursive(curve1, curve2, i - 1, j, memo), CalculateRecursive(curve1, curve2, i - 1, j - 1, memo)), CalculateRecursive(curve1, curve2, i, j - 1, memo));
            memo[i, j] = Math.Max(temp, EuclideanDistance(curve1[i], curve2[j]));
        }
        else
        {
            memo[i, j] = double.MaxValue;
        }

        return memo[i, j];
    }

    public double Calculate(List<Tuple<double, double>> curve1, List<Tuple<double, double>> curve2)
    {
        double[,] memo = new double[curve1.Count, curve2.Count];
        for (int i = 0; i < curve1.Count; i++)
        {
            for (int j = 0; j < curve2.Count; j++)
            {
                memo[i, j] = -1;
            }
        }

        return CalculateRecursive(curve1, curve2, curve1.Count - 1, curve2.Count - 1, memo);
    }
}