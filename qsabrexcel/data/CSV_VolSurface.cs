using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qsabrexcel.data
{
    internal class CSV_VolSurface
    {
        public double Tenor { get; set; }
        public double Expiry { get; set; }
        public double Fwd { get; set; }
        public double Strike { get; set; }
        public double Vol { get; set; }
    }
}
