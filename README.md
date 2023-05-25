# SABR calibration implementation (log normal volatility cubes)
Implementes in F# the calibration of volatility surfaces using Stochastic Alfa, Beta, Gamma and Rho (SABR).
The code is based on the article "Managing the Smile" by Hagan in 2002 ( https://www.researchgate.net/publication/235622441_Managing_Smile_Risk )

The following projects/files are good entry points to locate the most relevant logic:
## Project 
- **qrivol** 
(https://github.com/frannuca/qsabr/blob/main/volatility/sabr.fs)
SABR approximation volatility formulas for the lognormal case including hagan lognormal extension for greate accuracy.
The smile calibration by convex optimization is performed using Broyden–Fletcher–Goldfarb–Shanno Bounded (BFGS-B) algorithm.(https://github.com/frannuca/qsabr/blob/a18790dbed610a7976e5c9beed0d2d9665ade09f/volatility/sabr.fs#LL109C3-L109C3)

- **volcube** 
(https://github.com/frannuca/qsabr/blob/main/volatility/volcube.fs) 
Volatility surface data structure implemented as a builder class (VolSurfaceBuilder) to help for the construction and its companion class VolSurface.

- **qirvol** (https://github.com/frannuca/qsabr/blob/main/qirvol/Program.cs) 
Simple C# programs which demonstrate how to use the F# library from C#.
To run a calibration and generate of new surface run the following command:
 
  >> qirvol --input= <path to volsurface.csv> --output=<path to the ouput file> --resolution=100
  
 This command will read market data included inthe volatility.csv  (https://github.com/frannuca/qsabr/blob/main/data/volsurface.csv), calibrate the surface 
  and generate a new surface with 100 strikes. The output result will be dumped into the output csv file specificed with th option --output

- **qrirvol_test** project contains unit tests which can be visitied to demonstrate the usage of the library from F#. 
The construction of a volatility surface and its calibration is demonstrated in  https://github.com/frannuca/qsabr/blob/main/qrirvol_tests/sabr_surface_tests.fs , where a complete surface (with various maturities and tenors) is built, calibrated and check for accuracy against the benchmark provided in https://github.com/frannuca/qsabr/blob/main/data/market_data.xlsx .




