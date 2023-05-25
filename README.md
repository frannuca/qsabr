# SABR calibration implementation (log normal volatility cubes)
Calibration of volatility surfaces using Stochastic Alfa, Beta, Gamma and Rho (SABR).
The code is based on the article "Managing the Smile" by Hagan in 2002 ( https://www.researchgate.net/publication/235622441_Managing_Smile_Risk )

The following projects/files are good entry points to locate the most relevant logic:

## **qirvol** 
(https://github.com/frannuca/qsabr/blob/main/volatility/sabr.fs)
Simple console application which accepts an input csv with volatility surface data as per the format of the file https://github.com/frannuca/qsabr/blob/main/qrirvol_tests/data/volsurface.csv. The program generates two files, 
- one with a new interpolated vol surface and
- the SABR coefficents
both in csv format.

Command line example:
>>./qirvol --input=<path to volsurface.csv> --output=C:/temp --resolution=1000

## **volcube** 
(https://github.com/frannuca/qsabr/blob/main/volatility/volcube.fs) 
Volatility surface data structure implemented as a builder class (VolSurfaceBuilder) to help for the construction and its companion class VolSurface.

## **qirvol** (https://github.com/frannuca/qsabr/blob/main/qirvol/Program.cs) 
Simple C# programs which demonstrate how to use the F# library from C#.
To run a calibration and generate of new surface run the following command:
 
  >> qirvol --input= <path to volsurface.csv> --output=<path to the ouput file> --resolution=100
  
 This command will read market data included inthe volatility.csv  (https://github.com/frannuca/qsabr/blob/main/data/volsurface.csv), calibrate the surface 
  and generate a new surface with 100 strikes. The output result will be dumped into the output csv file specificed with th option --output

## **qrirvol_test** project contains unit tests which can be visitied to demonstrate the usage of the library from F#. 
The construction of a volatility surface and its calibration is demonstrated in  https://github.com/frannuca/qsabr/blob/main/qrirvol_tests/sabr_surface_tests.fs , where a complete surface (with various maturities and tenors) is built, calibrated and check for accuracy against the benchmark provided in https://github.com/frannuca/qsabr/blob/main/data/market_data.xlsx .




