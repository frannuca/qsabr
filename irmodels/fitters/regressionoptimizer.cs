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
    public record struct RegimeDefinition(double lower_boundary,double higher_boundary,string ticker);
	public class RegressionOptimizer
    {
        readonly IDataProvider<CurveData, string> _data;
        readonly CurveProjection _calculation_engine;
        readonly string[] _optimization_variables;
        readonly string[] _independent_optimization_variables;
        readonly string[] _dependent_optimization_variables;
        readonly RegimeDefinition[] _boundaries;
        readonly string[] _riskfactors;
        readonly Dictionary<string, int> variable_to_index;

        public RegressionOptimizer(IDataProvider<CurveData, string> dataProvider, RegimeDefinition[] boundaries)
        {
            
            //retrieve the tickers available in the data set
            HashSet<string> optimization_variables = new HashSet<string>();

            //store the risk factors to be used in the linear regression
            _riskfactors = dataProvider.RiskFactors;



            //the boundaries are mandatory and include information about the tickers that are conforming the independent variables
            //to be optimized.            
            _boundaries = boundaries;
            Func<RegimeDefinition,Func<double,double>> fcond_intercept = (RegimeDefinition b) => {
                return x => (x > b.lower_boundary && x <= b.higher_boundary) ? 1.0 : 0.0;
            };

            //Condition
            Func<RegimeDefinition, Func<double, double>> fcond_factor = (RegimeDefinition b) => {
                return x => (x > b.lower_boundary && x <= b.higher_boundary) ? x : 0.0;
            };

            variable_to_index = new Dictionary<string, int>();
            for (int i = 0; i < _boundaries.Length; ++i)
            {
                var b = boundaries[i];
                
                string intercept_var = $"I_{i}";                
                dataProvider.AddCondColumn(intercept_var, fcond_intercept(boundaries[i]), boundaries[i].ticker);
                    optimization_variables.Add(intercept_var);
                

               string reg_factor = $"{b.ticker}_{i}";
                dataProvider.AddCondColumn(reg_factor, fcond_factor(boundaries[i]), boundaries[i].ticker);                
            }
           
            _data = dataProvider;
            var oriskfactors = _data.OriginalRiskFactors;
            _optimization_variables = _data.get_clean_factors();
            _calculation_engine = new CurveProjection(_data.CleanRiskFrame);

            Regex re = new Regex("I_[1-9]");
            _independent_optimization_variables = (from v in _optimization_variables where !re.Match(v).Success select v).ToArray();
           
            _dependent_optimization_variables = (from v in _optimization_variables where re.Match(v).Success select v).ToArray();
        }

        Series<string, double> toSolSeries(double[] x)
        {
            var xs = new Dictionary<string, double>();
            for(int i = 0; i < x.Length; ++i)
            {
                xs[_independent_optimization_variables[i]] = x[i];
            }


            for(int i=1;i< _boundaries.Length; ++i)
            {
                var i0 = $"I_{i-1}";
                var i1 = $"I_{i}";
                var c0 = $"{_boundaries[i].ticker}_{i - 1}";
                var c1 = $"{_boundaries[i].ticker}_{i}";
                xs[i1] = xs[i0] + (xs[c0] - xs[c1]) * _boundaries[i].higher_boundary;
            }

            var ss= new Series<string, double>(xs);
            //Console.WriteLine(String.Join(",", ss.Values));
            return ss;
        }
        public Series<string, double> Fit(string target_label,Dictionary<string,(double,double)> limits)
        {
            var target = _data.getTargetSeries(target_label);

            int Nvar = _independent_optimization_variables.Length;
            // In code, this means we would like to minimize:
            var function = (double[] x) => _calculation_engine.compute_error(toSolSeries(x),target:target);
            
            NelderMead solver =
                new NelderMead(numberOfVariables: Nvar)
                {
                    Function = function
                };
          
            foreach (var limit in limits)
            {
                //Variables in the optimmization which match a limit
                var optvars = _independent_optimization_variables
                    .Where(p => p.Contains(limit.Key));

                foreach (var pvar in optvars)
                {
                    var idx = _independent_optimization_variables.IndexOf(pvar);
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

            return toSolSeries(solution);
        }
    }
}

