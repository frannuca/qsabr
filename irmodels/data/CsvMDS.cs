using System;
using CsvHelper;
using System.Globalization;

namespace irmodels.data
{
    public readonly record struct CsvMDSData(DateTime date, double quote);
    public class CsvMDS: IDataProvider<string, CsvMDSData>
	{        

        public CsvMDS(string path2folder):base(path2folder){ }

        override public void Close()
        {
            throw new NotImplementedException();
        }

        

        override public void Open(string path2file)
        {
            
            foreach (string file in System.IO.Directory.GetFiles(path2file, "*.csv", SearchOption.AllDirectories))
            {
                using (var reader = new StreamReader(file))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<CsvMDSData>();
                    if (records != null)
                    {
                        this.m_data.Add(System.IO.Path.GetFileNameWithoutExtension(file), records.ToArray());
                    }
                }
            }
        }
    }
}

