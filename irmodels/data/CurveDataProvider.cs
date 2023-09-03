using System;using CsvHelper;using System.Globalization;using Deedle;namespace irmodels.data{           public record struct CurveData(DateTime date, string curve_label, string curve_type, double curve_value);    public class CurveDataProvider : IDataProvider<CurveData, string>    {        public CurveDataProvider(string folderpath, string[] riskfactors) : base(folderpath, riskfactors)        { }        public override CurveData[] this[string key]         {            get{ return (from x in _data where x.curve_label == key select x).ToArray(); }        }        public override void Close()        {            throw new NotImplementedException();        }

        public override Series<DateTime, double> getTargetSeries(string ticker)
        {
            var a = from p in _data where p.curve_label == ticker select p;
            var b = Frame.FromRecords(a).IndexRows<DateTime>("date");
            var targetts= b.GetColumn<double>("curve_value");
            var dates = CleanRiskFrame.RowKeys;
            return targetts[dates];
        }

        protected override Frame<DateTime, string> toframe(CURVER_TPYE curve_type)        {            return Frame.FromRecords(_data.Where(p => p.curve_type==curve_type.ToString().ToLower()))                    .PivotTable<int,string,DateTime,string,double>("date","curve_label",r=> r.GetColumn<double>("curve_value").FirstValue())                    .DropSparseRows();        }    }}