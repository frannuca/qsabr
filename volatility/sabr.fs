﻿namespace qirvol.volatility

open System
open System.Collections
open MathNet.Numerics
open qirvol
open qirvol.qtime
open MathNet.Numerics.Optimization
open Accord.Math.Differentiation
open MathNet.Numerics.LinearAlgebra
open MathNet.Numerics.LinearAlgebra.Double;


///Module for the SABR calibration.
module SABR=
    
    //Each calibrated smile is charaterized by its alpha, beta, nu, rho and current forward 'f'.
    type SABRSolu={texp:float<year>;tenor:int<month>;alpha:float;beta:float;nu:float;rho:float;f:float}

    ///Given  SABR parameters returns the atm SABR expression coefficients of the underlying cubic
    ///polynome. The resulting coefficients are returnd in an array with:
    /// [0] -> a^1
    /// [1] -> a^2
    /// [2] -> a^3
    let get_atm_coeffs(b:float,nu:float,texp:float<year>,rho:float,f:float)=
        let fb = f**(1.0-b)
        let t = float(texp)
        [(1.0+(2.0-3.0*rho**2.0)*nu**2.0 * t/24.0)/fb;
        rho*b*nu/(4.0*fb**2.0);
        (1.0-b)**2.0 * t/(24.0 *fb**3.0)
        ]

    
    ///Computes the ATM volatility for a given set of SABR parameters as per   
    /// https://www.researchgate.net/publication/235622441_Managing_Smile_Risk/link/59e4d89f458515250246e626/download
    /// equation 2.18
    let Sigma_SABR_ATM(a:float,b:float,nu:float,texp:float<year>,rho:float,f:float)=
        if f <= 0.0 || a <= 0.0 then 0.0
        else
            let p= get_atm_coeffs(b,nu,texp,rho,f)
            a * p.[0]+ a**2.0*p.[1]+a**3.0*p.[2]

    let private hagan_expansion_log(rho,z)=
        let a = (1.0 - 2.0*rho*z + z**2.0)**0.5 + z - rho
        let b = 1.0 - rho
        Math.Log(a / b)

    /// General SABR approxmiation up to O(2) as per
    /// https://www.researchgate.net/publication/235622441_Managing_Smile_Risk/link/59e4d89f458515250246e626/download
    /// equation 2.17a
    let Sigma_SABR(alpha:float,beta:float,nu:float,texp:float<year>,rho:float,k:float,f:float)=
        if k<=0.0 || f <= 0.0 || alpha<=0.0 then
            0.0
        else if k=f then
            Sigma_SABR_ATM(alpha,beta,nu,texp,rho,f)
        else
            let t = float texp                                    
                        
            let eps = 1e-07
            let logfk = Math.Log(f / k)
            let fkbeta = (f*k)**(1.0 - beta)
            let a = (1.0 - beta)**2.0 * alpha**2.0 / (24.0 * fkbeta)
            let b = 0.25 * rho * beta * nu * alpha / fkbeta**0.5
            let c = (2.0 - 3.0*rho**2.0) * nu**2.0 / 24.0
            let d = fkbeta**0.5
            let v = (1.0 - beta)**2.0 * logfk**2.0 / 24.0
            let w = (1.0 - beta)**4.0 * logfk**4.0 / 1920.0
            let z = nu * fkbeta**0.5 * logfk / alpha

            
            if Math.Abs(z) > eps then
                alpha * z * (1.0 + (a + b + c) * t) / (d * (1.0 + v + w) * hagan_expansion_log(rho, z))                        
            else
                alpha * (1.0 + (a + b + c) * t) / (d * (1.0 + v + w))
            
            



    let Solve_alpha_for_ATM(sigmaATM:float,b:float,nu:float,texp:float<year>,rho:float,f:float)=
        if sigmaATM <= 0.0 || f <= 0.0   then 0.0
        else
            let p= get_atm_coeffs(b,nu,texp,rho,f)
            let struct (s1,s2,s3) = FindRoots.Cubic(-sigmaATM,p.[0],p.[1],p.[2])
            [s1;s2;s3] |> Seq.filter(fun x-> Math.Abs(x.Imaginary) < 1e-9) |> Seq.map(fun x -> x.Real)|> Seq.max


    ///Fitness function to per passed into the convex optimization algorithm to fit each smile.
    /// This method assumes that the alpha parameters is resolved for each (nu,rho), reducing
    /// therefore the optimization process to a 2-d problem.
    /// Returns the error in between the target smile and the SABR approximation.
    let private compute_fitness(smile:VolPillar array,beta:float,f:float) (rho:float,nu:float)=
       
        let atm_vol = (smile |> Array.minBy(fun p -> Math.Abs(p.strike-f))).volatility
        let texp = smile.[0].maturity

        let alpha = Solve_alpha_for_ATM(atm_vol,beta,nu,texp,rho,f)

        let error=smile |> Array.map(fun p ->
                                    let sigma_b = p.volatility
                                    (Sigma_SABR(alpha,beta,nu,texp,rho,p.strike,f)-sigma_b)**2.0
                        )|> Array.sum       
        System.Console.WriteLine(error)
        error

    
    /// Finds the optimum SABR parameters for a given target smile curve.
    /// Beta  is required as input, assuming the model will be normal, lognormal or CIR as prior assumption.
    let private calibrate_smile(smile:VolPillar array,nu0:float,rho0:float,beta:float)=
        let strikes = smile |> Array.map(fun p -> p.strike)
        let texp=smile.[0].maturity
        let tenor=smile.[0].tenor
        let f=smile.[0].forwardrate
        let fitness = compute_fitness(smile,beta,f)

        let optfunc = System.Func<Vector<float>, float>(fun (x:Vector<float>) -> fitness(x.[0],x.[1]))
        let xoptfunc = System.Func<float array, float>(fun (x:float array) -> fitness(x.[0],x.[1]))
        let gg =   new FiniteDifferences(2, xoptfunc,1,1e-5)
        let optgrad = fun (x:Vector<float>) -> gg.Gradient(x.ToArray()) |> Vector.Build.DenseOfArray
        
        let obj = ObjectiveFunction.Gradient(optfunc,System.Func<Vector<float>,Vector<float>> (optgrad))
        let solver = new BfgsBMinimizer (1e-5, 1e-5, 1e-5, maximumIterations= 1000)
        let lowerBound = new DenseVector([|-0.99;0.001|]);
        let upperBound = new DenseVector([|0.99;50.001|]);
        let initialGuess = new DenseVector([|0.1;10.0|]);

        let result = solver.FindMinimum(obj, lowerBound, upperBound, initialGuess);
        let res = result.FunctionInfoAtMinimum.Point.ToArray()

        let atm_vol = (smile |> Array.minBy(fun p -> Math.Abs(p.strike-f))).volatility
        let alpha = Solve_alpha_for_ATM(atm_vol,beta,res.[1],texp,res.[0],f)
        {texp=texp;tenor=tenor;alpha=alpha;beta=beta;nu=res.[1];rho=res.[0];f=f}
       


    ///Calibrates a given volatility surface using SABR model.
    ///Parameters:
    /// f: current forward
    /// nu0:  initial vol of vol guess parameter to pass to the underlying convex optimization algorithm
    /// rho0: initial correlation  guess parameter to pass to the underlying convex optimization algorithm
    /// beta: exponent paramter of the SABR model (b=0 normal, b=0.5 CIR, b=1 lognormal)
    let sigma_calibrate(volsurface:VolSurface,nu0:float,rho0:float,beta:float)=
        volsurface.maturities
        |> Array.map(fun texp -> let ftexp = float(texp)*1.0<day>*timeconversions.days2year
                                 ftexp,volsurface.tenors_by_maturity(ftexp) |> Array.ofSeq
                                 |> Array.map(fun tenor -> tenor,calibrate_smile(volsurface.Smile(ftexp,tenor),nu0,rho0,beta))|> Map.ofArray)
                                 |>Map.ofArray