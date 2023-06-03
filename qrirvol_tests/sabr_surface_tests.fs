

module Test_SURFACE_SABR

open System
open Xunit
open qirvol.volatility
open qirvol.qtime
open qirvol.qtime.timeconversions
open MathNet.Numerics.LinearAlgebra

type Testing_Surface_SABR()=
    
    [<Fact>]
    let ``smile calibration of synthetic vol needs to match expected value`` () =
        (**
           Providing a known optimization results for a given smile this test checks
           that the optimization process returns the expecte alpha, nu and rho
           coefficients.
        .**)
        
        let f =  0.028436364    //spot forward
        let beta = 0.5          //CIR model is assumed
        let texp = 5.0<year>    // five years Swaption maturity
        let tenor = 3<month>    // 3 months tenor.
        let nu0=0.05            // initial vol of vol parameter for the underlying optimization algorithm
        let rho0 = 0.1          // initial rho  parameter for the underlying optimization algorithm


        //Surface data (one single smile in this test)
        let sigmaB =[|0.4040; 0.3541; 0.3218; 0.3107; 0.3048; 0.2975; 0.2923; 0.2873; 0.2870|]         
        let strikes_in_bps = [|-150;-100;-50;-25;0;25;50;100;150|] |> Array.map(fun k -> (float k )*1e-4+ f)

        //Constructing the vol surface using a builder object.
        let surface = VolSurfaceBuilder()
                        .withPillars([0 .. sigmaB.Length-1]
                                        |> Seq.map(fun i ->{maturity=texp;strike=strikes_in_bps.[i];VolPillar.tenor=tenor;VolPillar.volatility=sigmaB.[i];VolPillar.forwardrate=f}))
                        .Build()

        // calibration of the surface (smile) 
        let res = SABR.sigma_calibrate(surface,nu0,rho0,beta)

        //Since we only have one smile for the 3m in 5y we just extract the calibrated parameters for the smile.
        //TODO: In this version expiries as expressed as floating key, which is not ideal as matching requires tolerance.        
        let coeff = res.[texp].[tenor]


        //Checking the results
        Assert.Equal(coeff.[0].alpha,0.048400445737410716,2)
        Assert.Equal(coeff.[0].beta,0.5,2)
        Assert.Equal(coeff.[0].nu,0.37371398762385843,2)
        Assert.Equal(coeff.[0].rho,-0.03295848492102798,2)
        Assert.Equal(float coeff.[0].tenor,float 3,2)
        Assert.Equal(float coeff.[0].texp,float texp,2)
        Assert.Equal(float coeff.[0].f,float f,2)
        
        
   
    [<Fact>]
    let ``calibration of entire surface with b=0.5`` () =
        (**
            In this test we construct a voltility surface and run a full parameter
            cube calibration with the assumptionof CIR model (beta=0.5). Once the calibration for all the surface smiles is
            finished, the per smile error is calculated and checked against
            a threshold.
        **)

        ///Strike deltas
        let strikes_in_bps = test_commons.strikes_in_bps


        //Constructing the vol surface using a builder object.
        let surface = test_commons.get_benchmark_surface()
        let nu0=0.1
        let rho0=0.3
        let beta=0.5

        // calibration of the surface (smile) 
        let res = SABR.sigma_calibrate(surface,nu0,rho0,beta)
        test_commons.compare_surfaces(strikes_in_bps,res,surface)
        
        0.0
    [<Fact>]
    let ``calibration of entire surface with b=1`` () =
        (**
            In this test we construct a voltility surface and run a full parameter
            cube calibration with the asusmption of lognormal model (beta=1). Once the calibration for all the surface smiles is
            finished, the per smile error is calculated and checked against
            a threshold.
        **)

        ///Strike deltas
        let strikes_in_bps = test_commons.strikes_in_bps

        
        //Constructing the vol surface using a builder object.
        let surface = test_commons.get_benchmark_surface()
        let nu0=0.1
        let rho0=0.3
        let beta=1.0

        // calibration of the surface (smile) 
        let res = SABR.sigma_calibrate(surface,nu0,rho0,beta)
        test_commons.compare_surfaces(strikes_in_bps,res,surface)
        
        0.0