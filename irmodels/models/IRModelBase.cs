using System;
using MathNet.Numerics.LinearAlgebra;
namespace irmodels.models{

	public abstract class IrModelBase<TA,TB>
	{
		public IrModelBase(double th,uint ntime, uint nsims)
		{
            Thorizon = th;
            N = ntime;
            NSim = nsims;
        }
		protected readonly double Thorizon;
        protected readonly uint N;
        protected readonly uint NSim;

        public abstract Matrix<double> Run(TA x);
        public abstract TA Calibrate(TB x);
    }
}

