using System;
using CsvHelper;
using System.Globalization;
using Deedle;
namespace irmodels.data
{

    abstract public class IDataProvider<TData, TKey> where TData : struct
    {
        //The TData object requires to include a property indicating if the quote
        //is market or target value.
        public enum CURVER_TPYE { MARKET = 0, TARGET = 1 };
        readonly protected TData[] _data;        
        Frame<DateTime, string> _frame_data;
        readonly HashSet<string> _original_riskfactors;

        public IDataProvider(string folderpath, string[] riskfactors)
        {
            _original_riskfactors = new HashSet<string>(riskfactors);
            if (folderpath is null)
            {
                throw new ArgumentNullException(nameof(folderpath));
            }
            _data = this.Open(folderpath);
            _frame_data = this.toframe(CURVER_TPYE.MARKET).Columns[riskfactors];
            


        }

        public string[] get_clean_factors()
        {
            var orisk = OriginalRiskFactors;
            var allregrisk = new HashSet<string>(from r in RiskFactors where r.Contains("_") && !r.Contains("I_") select r.Split('_')[0]);
            var risk_2_remove = orisk.Intersect(allregrisk).ToList();
            return RiskFactors.ToList().Except(risk_2_remove).ToArray();
        }

        public HashSet<string> OriginalRiskFactors => _original_riskfactors;
        /// <summary>
        /// converts the internal record (row) struct into a frame.
        /// </summary>
        /// <param name="curve_type">curve types as defined</param>
        /// <returns></returns>
        protected abstract Frame<DateTime, string> toframe(CURVER_TPYE curve_type);

        public Frame<DateTime, string> RiskFrame
        {
            get { return _frame_data; }
        }

        public Frame<DateTime, string> CleanRiskFrame
        {
            get { return _frame_data.Columns[get_clean_factors()]; }
        }
        public Frame<DateTime, string> AddCondColumn(string newcolname, Func<double, double> fcond, string refcolumn)
        {
            
            if ( _frame_data.Columns.ContainsKey(newcolname))
            {
                throw new ArgumentException($"Provided new column already exists:{newcolname}");
            }

            var newcol = _frame_data.GetColumn<double>(refcolumn).Select(r => fcond(r.Value));
                        
            _frame_data.AddColumn(newcolname, newcol);
            
            

            return _frame_data;
        }
       
        public string[] RiskFactors
        {
            get { return _frame_data.ColumnKeys.ToArray(); }
        }

        public Frame<DateTime, string> RiskFactorsFrame => _frame_data;

        abstract public Series<DateTime, double> getTargetSeries(string ticker);
        
        protected TData[] Open(string path2folder)
        {
            var data = new List<TData>();
            foreach (string file in System.IO.Directory.GetFiles(path2folder, "*.csv", SearchOption.AllDirectories))
            {
                using (var reader = new StreamReader(file))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<TData>();
                    if (records != null)
                    {
                        data.AddRange(records.ToArray());
                    }
                }
            }

            return data.ToArray();
        }

       
        abstract public void Close();
        abstract public TData[] this[TKey key]
        {
             get;
        }
    }
}

