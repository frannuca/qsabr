using System;
using MathNet.Numerics.LinearAlgebra;
namespace irmodels.models{

	public abstract class IRModelBase<A,B>
	{
		public IRModelBase(double th,uint Ntime, uint nsims)
		{
            thorizon = th;
            N = Ntime;
            NSim = nsims;
        }
		protected readonly double thorizon;
        protected readonly uint N;
        protected readonly uint NSim;

        public abstract Matrix<double> Run(A x);
        public abstract A calibrate(B x);
    }
}

