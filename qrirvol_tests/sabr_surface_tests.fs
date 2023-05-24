

module Test__SURFACE_SABR

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
        Assert.Equal(coeff.alpha,0.048400445737410716,2)
        Assert.Equal(coeff.beta,0.5,2)
        Assert.Equal(coeff.nu,0.37371398762385843,2)
        Assert.Equal(coeff.rho,-0.03295848492102798,2)
        Assert.Equal(float coeff.tenor,float 3,2)
        Assert.Equal(float coeff.texp,float texp,2)
        Assert.Equal(float coeff.f,float f,2)
        
        
   
    [<Fact>]
    let ``calibration of entire surface`` () =
        (**
            In this test we construct a voltility surface and run a full parameter
            cube calibration. Once the calibration for all the surface smiles is
            finished, the per smile error is calculated and checked against
            a threshold.
        **)

        ///Strike deltas
        let strikes_in_bps = [|-150.0;-100.0;-50.0;-25.0;0.0;25.0;50.0;100.0;150.0|]

        let generate_pillar(tenor:int<month>,texp:float<year>,f:float,sigmaB:float array)=
            strikes_in_bps
            |> Array.mapi(fun i dk -> {maturity=texp;strike=f+dk*1e-4;VolPillar.tenor=tenor;VolPillar.volatility=sigmaB.[i];VolPillar.forwardrate=f})
        


        //Constructing the vol surface using a builder object.
        let surface = VolSurfaceBuilder()
                        .withPillars(generate_pillar(int(2.0<year>*years2months)*1<month>,
                                                    0.25<year>,
                                                    0.0107638332259373,
                                                    [|0.0000;1.0470;0.4812;0.4327;0.4268;0.4148;0.4253;0.4322;0.4495|]))
                        .withPillars(generate_pillar(int(2.0<year>*years2months)*1<month>,
                                                     0.5<year>,
                                                     0.011099189328091,
                                                     [|0.000;0.9647;0.5079;0.4637;0.4477;0.4390;0.4377;0.4452;0.4576|]))
                        .withPillars(generate_pillar(int(2.0<year>*years2months)*1<month>,
                                                     0.75<year>,
                                                     0.0116024287527238,
                                                     [|0.0000;0.8253;0.5033;0.4648;0.4494;0.4387;0.4348;0.4375;0.4463|]))
                        .withPillars(generate_pillar(int(2.0<year>*years2months)*1<month>,
                                                     1.0<year>,
                                                     0.0121935636676584,
                                                     [|0.0000;0.6796;0.4788;0.4474;0.4501;0.4435;0.4478;0.4611;0.4754|]))
                        .withPillars(generate_pillar(int(2.0<year>*years2months)*1<month>,
                                                     2.0<year>,
                                                     0.0161959844231264,
                                                     [|0.0000;0.9119;0.5417;0.4628;0.4529;0.4461;0.4386;0.4387;0.4442|]))
                        .withPillars(generate_pillar(int(2.0<year>*years2months)*1<month>,
                                                     5.0<year>,
                                                     0.0284363638504981,
                                                     [|0.4040;0.3541;0.3218;0.3107;0.3048;0.2975;0.2923;0.2873;0.2870|]))
                        .withPillars(generate_pillar(int(2.0<year>*years2months)*1<month>,
                                                     10.0<year>,
                                                     0.0338734710965737,
                                                     [|0.3026;0.2725;0.2510;0.2422;0.2343;0.2279;0.2228;0.2161;0.2128|]))
                        .Build()


        let nu0=0.1
        let rho0=0.3
        let beta=0.5
        // calibration of the surface (smile) 
        let res = SABR.sigma_calibrate(surface,nu0,rho0,beta)

        res
        |> Map.iter(fun (T:float<year>) (tenor2param:Map<int<month>,SABR.SABRSolu>) ->
                          tenor2param
                          |> Map.iter(fun tenor x ->
                                        let computed = strikes_in_bps                                                   
                                                        |> Array.map(fun k-> SABR.Sigma_SABR(x.alpha,x.beta,x.nu,T,x.rho,x.f+k*1e-4,x.f))
                                                        
                                        let expected = surface.Smile(T,tenor)
                                                        |> Array.map(fun y -> y.volatility)
                                                        

                                        let computed_no_zeros=
                                                                if computed.Length > expected.Length then
                                                                    computed.[1..]
                                                                else
                                                                    expected

                                        (computed_no_zeros,expected)
                                        ||> Array.iter2(fun a b -> Assert.Equal(a,b,tolerance=0.15))  
                                            

                                        



                                            
                          )
        )
        
        
        0.0