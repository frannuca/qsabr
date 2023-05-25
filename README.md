# SABR calibration implementation (log normal volatility cubes)
Calibration of volatility surfaces using Stochastic Alfa, Beta, Gamma and Rho (SABR).
The code is based on the article "Managing the Smile" by Hagan in 2002 ( https://www.researchgate.net/publication/235622441_Managing_Smile_Risk )

The following projects/files are good entry points to find the most relevant logic:

## Project **qirvol** 
(https://github.com/frannuca/qsabr/blob/main/volatility/sabr.fs)
Simple console application which accepts aa input a csv file with volatility surface data (as per the format depicted in the file https://github.com/frannuca/qsabr/blob/main/qrirvol_tests/data/volsurface.csv). 
The program generates two files, namely:
- one with a new interpolated vol surface and
- a second one with the SABR coefficents
both in csv format.

Command line usage:
>>./qirvol --input=<path to volsurface.csv> --output=C:/temp --resolution=1000

## Project **volcube** 
This project includes data structure definitions to access volatilty surfaces (https://github.com/frannuca/qsabr/blob/main/volatility/volcube.fs) and sabr coefficients cubes (https://github.com/frannuca/qsabr/blob/main/volatility/sabrcube.fs). 
 The calibration using (lognormal approximation) is part of the module SABR, more specifically the function https://github.com/frannuca/qsabr/blob/main/volatility/sabr.fs#L51, which applies BFGS-B algorithm (Broyden–Fletcher–Goldfarb–Shanno Bounded) to optimize rho and nu coefficent for a given beta and resolved alpha to match at the moment volatility for each smile. This approach shows to be stable and fast convergent.

 Lastly the module SABRInterpolator (https://github.com/frannuca/qsabr/blob/main/volatility/sabrinterpolator.fs#L13) includes various functions to re-sample the original volutility surface to higher strike resolutions.
 
 
## **qrirvol_test** 
Contains unit tests which can be visitied to demonstrate the usage of the library from F#. 
The construction of a volatility surface and its calibration is demonstrated in  https://github.com/frannuca/qsabr/blob/main/qrirvol_tests/sabr_surface_tests.fs , where a complete surface (with various maturities and tenors) is built, calibrated and check for accuracy against a chosen benchmark.




