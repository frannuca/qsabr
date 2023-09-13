using System;
using CsvHelper;
using System.Globalization;
using Deedle;
namespace irmodels.data
{

    abstract public class DataProvider<TData, TKey> where TData : struct
    {
        //The TData object requires to include a property indicating if the quote
        //is market or target value.
        public enum CurverTpye { Market = 0, Target = 1 };
        readonly protected TData[] Data;        
        Frame<DateTime, string> _frameData;
        readonly HashSet<string> _originalRiskfactors;

        public DataProvider(string folderpath, string[] riskfactors)
        {
            _originalRiskfactors = new HashSet<string>(riskfactors);
            if (folderpath is null)
            {
                throw new ArgumentNullException(nameof(folderpath));
            }
            Data = this.Open(folderpath);
            _frameData = this.Toframe(CurverTpye.Market).Columns[riskfactors];
            


        }

        public string[] get_clean_factors()
        {
            var orisk = OriginalRiskFactors;
            var allregrisk = new HashSet<string>(from r in RiskFactors where r.Contains("_") && !r.Contains("I_") select r.Split('_')[0]);
            var risk2Remove = orisk.Intersect(allregrisk).ToList();
            return RiskFactors.ToList().Except(risk2Remove).ToArray();
        }

        public HashSet<string> OriginalRiskFactors => _originalRiskfactors;
        /// <summary>
        /// converts the internal record (row) struct into a frame.
        /// </summary>
        /// <param name="curveType">curve types as defined</param>
        /// <returns></returns>
        protected abstract Frame<DateTime, string> Toframe(CurverTpye curveType);

        public Frame<DateTime, string> RiskFrame
        {
            get { return _frameData; }
        }

        public Frame<DateTime, string> CleanRiskFrame
        {
            get { return _frameData.Columns[get_clean_factors()]; }
        }
        public Frame<DateTime, string> AddCondColumn(string newcolname, Func<double, double> fcond, string refcolumn)
        {
            
            if ( _frameData.Columns.ContainsKey(newcolname))
            {
                throw new ArgumentException($"Provided new column already exists:{newcolname}");
            }

            var newcol = _frameData.GetColumn<double>(refcolumn).Select(r => fcond(r.Value));
                        
            _frameData.AddColumn(newcolname, newcol);
            
            

            return _frameData;
        }
       
        public string[] RiskFactors
        {
            get { return _frameData.ColumnKeys.ToArray(); }
        }

        public Frame<DateTime, string> RiskFactorsFrame => _frameData;

        abstract public Series<DateTime, double> GetTargetSeries(string ticker);
        
        protected TData[] Open(string path2Folder)
        {
            var data = new List<TData>();
            foreach (string file in System.IO.Directory.GetFiles(path2Folder, "*.csv", SearchOption.AllDirectories))
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

