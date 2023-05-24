// See https://aka.ms/new-console-template for more information
using static System.Net.Mime.MediaTypeNames;
using System;
using qirvol.volatility;

// Main Method
static class Program {
    static public void Main(String[] args)
    {
        // Demonstration program in C# to calibrate a swaption volitility surface (1 smile) using C#

        //----------------------
        //Pameters of the model:        
        //----------------------
        var nu = 0.05;          //vol of vol        
        var rho = 0.8;          //forward<->vol correlation
        var f = 0.028436364;    //spot forward rate
        var beta = 0.5;         //beta from SABR assuming CIR process
        var texp = 5.0;         //expiration time in 5 years
        var tenor = 3;          // tenor of the swaption in months
        var nu0 = 0.05;         // initial vol of vol value for the underyting optimzation process.
        var rho0 = 0.1;         // initial rho value for the underlying optimization process.



        //-----------
        //Smile data
        //----------- 
        var sigmaB = new double[] { 0.4040, 0.3541, 0.3218, 0.3107, 0.3048, 0.2975, 0.2923, 0.2873, 0.2870 };
        var atmvol = 0.3048;
        var strikes_in_bps = new double[] { -150, -100, -50, -25, 0, 25, 50, 100, 150 };


        //--------------------------------------------------
        // Constructing the volatility surface for the model
        //--------------------------------------------------
        var pillars = Enumerable.Range(0, sigmaB.Length)
                    .Select(n => new VolPillar(tenor: tenor, strike: strikes_in_bps[n]*1e-4 + f, maturity: texp, volatility: sigmaB[n],forwardrate:f));

        var surface = new VolSurfaceBuilder().withPillars(pillars).Build();

        //Check on resolving atm vol for the optimum beta, rho, nu paramter and checking it matches the given atm one in the data.
        var sol = SABR.Solve_alpha_for_ATM(atmvol, beta, nu, texp, rho, f);
        var atm_computed = SABR.Sigma_SABR_ATM(sol, beta, nu, texp, rho, f);
        if (Math.Abs(atm_computed - atmvol) > 1e-4) throw new Exception("Wrong atm resolution.");

        //Calling the calibration engine on the just built vol surface object
        // and plotting the optimium parmaters (1 smile in this case)
        var res = SABR.sigma_calibrate(surface, nu0, rho0, beta);

        System.Console.WriteLine($"alpha={res[texp][tenor].alpha}");
        System.Console.WriteLine($"beta={res[texp][tenor].beta}");
        System.Console.WriteLine($"nu={res[texp][tenor].nu}");
        System.Console.WriteLine($"rho={res[texp][tenor].rho}");
        System.Console.WriteLine($"f={res[texp][tenor].f}");

       

        Console.WriteLine("Success!!");
        
    }
}