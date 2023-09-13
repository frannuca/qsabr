using System;
using System.Collections.Generic;
using Deedle;
using irmodels.data;
using Accord.Math.Optimization;
using System.Reflection;
using System.Text.RegularExpressions;
using Accord.Math;

namespace irmodels.fitters
{
    public record struct RegimeDefinition(double LowerBoundary,double HigherBoundary,string Ticker);
	public class RegressionOptimizer
    {
        readonly DataProvider<CurveData, string> _data;
        readonly CurveProjection _calculationEngine;
        readonly string[] _optimizationVariables;
        readonly string[] _independentOptimizationVariables;
        readonly string[] _dependentOptimizationVariables;
        readonly RegimeDefinition[] _boundaries;
        readonly string[] _riskfactors;
        readonly Dictionary<string, int> _variableToIndex;

        public RegressionOptimizer(DataProvider<CurveData, string> dataProvider, RegimeDefinition[] boundaries)
        {
            
            //retrieve the tickers available in the data set
            HashSet<string> optimizationVariables = new HashSet<string>();

            //store the risk factors to be used in the linear regression
            _riskfactors = dataProvider.RiskFactors;



            //the boundaries are mandatory and include information about the tickers that are conforming the independent variables
            //to be optimized.            
            _boundaries = boundaries;
            Func<RegimeDefinition,Func<double,double>> fcondIntercept = (RegimeDefinition b) => {
                return x => (x > b.LowerBoundary && x <= b.HigherBoundary) ? 1.0 : 0.0;
            };

            //Condition
            Func<RegimeDefinition, Func<double, double>> fcondFactor = (RegimeDefinition b) => {
                return x => (x > b.LowerBoundary && x <= b.HigherBoundary) ? x : 0.0;
            };

            _variableToIndex = new Dictionary<string, int>();
            for (int i = 0; i < _boundaries.Length; ++i)
            {
                var b = boundaries[i];
                
                string interceptVar = $"I_{i}";                
                dataProvider.AddCondColumn(interceptVar, fcondIntercept(boundaries[i]), boundaries[i].Ticker);
                    optimizationVariables.Add(interceptVar);
                

               string regFactor = $"{b.Ticker}_{i}";
                dataProvider.AddCondColumn(regFactor, fcondFactor(boundaries[i]), boundaries[i].Ticker);                
            }
           
            _data = dataProvider;
            var oriskfactors = _data.OriginalRiskFactors;
            _optimizationVariables = _data.get_clean_factors();
            _calculationEngine = new CurveProjection(_data.CleanRiskFrame);

            Regex re = new Regex("I_[1-9]");
            _independentOptimizationVariables = (from v in _optimizationVariables where !re.Match(v).Success select v).ToArray();
           
            _dependentOptimizationVariables = (from v in _optimizationVariables where re.Match(v).Success select v).ToArray();
        }

        Series<string, double> ToSolSeries(double[] x)
        {
            var xs = new Dictionary<string, double>();
            for(int i = 0; i < x.Length; ++i)
            {
                xs[_independentOptimizationVariables[i]] = x[i];
            }


            for(int i=1;i< _boundaries.Length; ++i)
            {
                var i0 = $"I_{i-1}";
                var i1 = $"I_{i}";
                var c0 = $"{_boundaries[i].Ticker}_{i - 1}";
                var c1 = $"{_boundaries[i].Ticker}_{i}";
                xs[i1] = xs[i0] + (xs[c0] - xs[c1]) * _boundaries[i].HigherBoundary;
            }

            var ss= new Series<string, double>(xs);
            //Console.WriteLine(String.Join(",", ss.Values));
            return ss;
        }
        public Series<string, double> Fit(string targetLabel,Dictionary<string,(double,double)> limits)
        {
            var target = _data.GetTargetSeries(targetLabel);

            int nvar = _independentOptimizationVariables.Length;
            // In code, this means we would like to minimize:
            var function = (double[] x) => _calculationEngine.compute_error(ToSolSeries(x),target:target);
            
            NelderMead solver =
                new NelderMead(numberOfVariables: nvar)
                {
                    Function = function
                };
          
            foreach (var limit in limits)
            {
                //Variables in the optimmization which match a limit
                var optvars = _independentOptimizationVariables
                    .Where(p => p.Contains(limit.Key));

                foreach (var pvar in optvars)
                {
                    var idx = _independentOptimizationVariables.IndexOf(pvar);
                    solver.UpperBounds[idx] = limit.Value.Item2;
                    solver.LowerBounds[idx] = limit.Value.Item1;
                }
            }
            // Now, we can minimize it with:
            bool success = solver.Minimize();

            // And get the solution vector using
            double[] solution = solver.Solution; // should be (-1, 1)

            // The minimum at this location would be:
            double minimum = solver.Value; // should be 0

            return ToSolSeries(solution);
        }
    }
}

