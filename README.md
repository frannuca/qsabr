# SABR calibration implementation (Interpolated volatility cubes)
Calibration of volatility surfaces using Stochastic Alfa, Beta, Rho (SABR).
The code is based on the article "Managing the Smile" by Hagan in 2002 ( https://www.researchgate.net/publication/235622441_Managing_Smile_Risk )

The following projects/files are good entry points to find the most relevant logic:

## **qirvol** 
https://github.com/frannuca/qsabr/blob/main/qirvol/Program.cs
Simple console application which accepts an input a csv file with volatility surface data (as per the format depicted in the file https://github.com/frannuca/qsabr/blob/main/qrirvol_tests/data/volsurface.csv). 
The program generates two files, namely:
- one with a new interpolated vol surface and
- a second one with the SABR coefficents
both in csv format.

Command line usage:
- The following command will generate a resampled volatility smile file for a tenor of 24 month, expiry 10 years, using a SABR parameters beta=1 (lognormal). The moneyness of the 
smile (F/K) will go from F/K=0.5 up to F/K=2.0.  Moneyness is referred to the provided forward 200 (int basis points) and 10 point will be equally spaced in the smile x-axis generation. 
>>  ./qirvol --input=/Users/fran/code/qsabr/qrirvol_tests/data/input_volsurface.csv --output=./smile2.csv --maturity=10 --tenor=24 --beta=1.0 -l 0.5 -h 2.0 -f 200e-4 -r 10

Use this commnad to obtain help on how to run the application:

>>./qirvol --help

## **volatility** 
This project includes data structure definitions to access volatilty surfaces and calibration algorithm for SABR coefficients, namely:

 - (https://github.com/frannuca/qsabr/blob/main/volatility/basecube.fs) base cube data class to be inherited for specific serialization into csv. Mainly vol and sabr coefficients share a similar cube structure which is shared across using this base class.

 - (https://github.com/frannuca/qsabr/blob/main/volatility/volcube.fs) includes vol surface data structure for easy management of the smiles.

 - (https://github.com/frannuca/qsabr/blob/main/volatility/sabrcube.fs) is the data class to manage SABR coefficient cubes.

 - The calibration is part of the module SABR module, more specifically the file https://github.com/frannuca/qsabr/blob/main/volatility/sabr.fs, which includes the calibration routines to compute SABR volatility. The underlying optimization algorithm is  BFGS-B algorithm (Broyden–Fletcher–Goldfarb–Shanno Bounded), which is used to optimize rho and nu coefficents for beta<=0.5 and alpha,rho and nu otherwise.  This algorithm shows to be stable and fast convergent.

 - The module SABRInterpolator (https://github.com/frannuca/qsabr/blob/main/volatility/sabrinterpolator.fs includes the SABRInterpolator.SurfaceInterpolator type, which corresponds to a class integrating the interpolation capabilities on maturity, tenor and strike (moneyness). Interpolation on maturities is performed using total variance  while for tenors only a linear interpolation is available at the moment. Calendar spread arbritage is  hence avoided on maturity interpolation but more work might be required on the algorithm to interpolate tenors. (For total variance interpolation see equation (21) in https://www.iasonltd.com/doc/old_rps/2007/2013_The_implied_volatility_surfaces.pdf)
 
##  **Greeks**
A first version of greeks for delta, gamma (non-diagonal) and vega is included. The expressions used for the given greeks are taken from https://www.next-finance.net/IMG/pdf/pdf_SABR.pdf .

The computation of sensitivities with respect to SABR parameters such as $\rho$ requires to either interpolate on the SABR parameters directely (which is not implemented in this version) or resample the volatitlity surface for a given maturity and tenor, compute the SABR coefficients and then derive with respect to $\rho$ on the just interpolated point in the vol surface. This is currently work in progress.

## **qrirvol_test** 
Contains unit tests which can be visitied to demonstrate the usage of the library from F#. 
 ## **qdata** and **qtime**
 Are work in progress intended more as placeholder for a future calendar library and data transformations.
 
 




