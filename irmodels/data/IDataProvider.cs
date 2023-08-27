using System;
namespace irmodels.data
{    
    
	abstract public class IDataProvider<TConfig,TData>
	{
		public IDataProvider(TConfig c)
		{
            if (c is null)
            {
                throw new ArgumentNullException(nameof(c));
            }
            this.Open(c);
		}
		protected Dictionary<string, TData[]> m_data = new Dictionary<string, TData[]>();
		abstract public void Open(TConfig c);
		abstract public void Close();
		public TData[] Get(string ticker) => m_data[ticker];		
	}
}

