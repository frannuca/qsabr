# SABR calibration implementation (log normal volatility cubes)
Calibration of volatility surfaces using Stochastic Alfa, Beta, Gamma and Rho (SABR).
The code is based on the article "Managing the Smile" by Hagan in 2002 ( https://www.researchgate.net/publication/235622441_Managing_Smile_Risk )

The following projects/files are good entry points to find the most relevant logic:

## **qirvol** 
https://github.com/frannuca/qsabr/blob/main/qirvol/Program.cs
Simple console application which accepts aa input a csv file with volatility surface data (as per the format depicted in the file https://github.com/frannuca/qsabr/blob/main/qrirvol_tests/data/volsurface.csv). 
The program generates two files, namely:
- one with a new interpolated vol surface and
- a second one with the SABR coefficents
both in csv format.

Command line usage:
>>./qirvol --input=<path to volsurface.csv> --output=C:/temp --resolution=1000

## **volatility** 
This project includes data structure definitions to access volatilty surfaces and calibration algorithm for SABR coefficients, namely:
 - (https://github.com/frannuca/qsabr/blob/main/volatility/basecube.fs) base cube data class to be inherited for specific serialization into csv. Mainly vol and sabr coefficients share a similar cube structure which is shared across using this base class.
 - (https://github.com/frannuca/qsabr/blob/main/volatility/volcube.fs) includes vol surface data structure for easy management of the smiles
 - (https://github.com/frannuca/qsabr/blob/main/volatility/sabrcube.fs) is the data class to manage SABR coefficient cubes.
 - The calibration using (lognormal approximation) is part of the module SABR, more specifically the function https://github.com/frannuca/qsabr/blob/main/volatility/sabr.fs#L51, which applies BFGS-B algorithm (Broyden–Fletcher–Goldfarb–Shanno Bounded) to optimize rho and nu coefficent for a given beta and resolved alpha to match at the moment volatility for each smile. This approach shows to be stable and fast convergent.

 Lastly the module SABRInterpolator (https://github.com/frannuca/qsabr/blob/main/volatility/sabrinterpolator.fs#L13) includes various functions to re-sample the original volutility surface to higher strike resolutions.
 
 
## **qrirvol_test** 
Contains unit tests which can be visitied to demonstrate the usage of the library from F#. 
The construction of a volatility surface and its calibration is demonstrated in  https://github.com/frannuca/qsabr/blob/main/qrirvol_tests/sabr_surface_tests.fs , where a complete surface (with various maturities and tenors) is built, calibrated and check for accuracy against a chosen benchmark.
Seriealization of re-sampled surface into csv is demonstrated in https://github.com/frannuca/qsabr/blob/main/qrirvol_tests/sabr_resampling_tests.fs.
 
 ## **qdata** and **qtime**
 Are work in progress intended more as placeholder for a future calendar library and data transformations.
 
 




