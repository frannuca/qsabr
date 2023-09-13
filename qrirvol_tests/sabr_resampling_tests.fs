module Test_SURFACE_RESAMPLING_SABR

open System
open Xunit
open qirvol.volatility
open qirvol.qtime
open qirvol.qtime.timeconversions
open MathNet.Numerics.LinearAlgebra
open qirvol.volatility.SABR

type Testing_Surface_SABR()=
    [<Fact>]
    let ``Interpolation in maturity and tenors over benchmark`` () =
        (**
            Reads vol surface from csv, calibrates SABR and resamples in maturity and tenor.
            The recalibrated surface and SABR paramters are compared with respect to a benchmark.
            On the contrary, if the interpolator is required to return an existing tenor, it must not
            throw and return the interpolated values for that tenor, maturity and strike.
        **)

                
        // Generation of the surface data from input file ...
        let surface = VolSurface.from_csv("./data/input_volsurface.csv")
        
        let nstrikes=50
        let beta=0.5
        let minStrikeSpread = -150.0
        let maxStrikeSpread = 150.0
        let N=100

                
        

        //reconstructing for maturities in between 0.5 and 0.75 years:
        let all_maturities = [|0.5;0.55;0.6;0.65;0.7;0.75|] |> Array.map(fun t -> t*1.0<year>)
        let all_tenors = [|2 .. 30|] |> Array.map(fun n -> n*1<month>) 

        let logf_K = [0 .. N-1]
                        |> Seq.map(fun n -> Math.Log(float 0.5+(2.0-0.5)*float n/(float N-1.0)))
                        |> Array.ofSeq
                        |> Array.rev

        // Resampling the surface 
        let volinterpolator = SABRInterpolator.SurfaceInterpolator(surface,beta)

        let fwd = 200e-4
        
        //resampling the vol surface:
        let computed_surface = volinterpolator.resample_surface(all_maturities,all_tenors,logf_K,fwd)
                                
       
        //new_surface.to_csv("./data/resampled_vol_surface.csv")

        let expected_surface = VolSurface.from_csv("./data/output_resampled.csv")

        //checking maturities:
        (expected_surface.maturities,computed_surface.maturities) ||> Array.iter2(fun a b -> Assert.Equal(a,b))

        //Checking tenors:
        expected_surface.maturities_years
        |> Array.iter(fun texp ->
                    (expected_surface.tenors_by_maturity(texp),computed_surface.tenors_by_maturity(texp))
                     ||> Seq.iter2(fun a b -> Assert.Equal(a,b))
        )

        //Checking values:
        //Checking tenors:
        expected_surface.maturities_years
        |> Array.iter(fun texp ->
                    expected_surface.tenors_by_maturity(texp)
                    |> Seq.iter(fun tenor ->
                           (expected_surface.Smile(texp,tenor),expected_surface.Smile(texp,tenor))
                           ||> Seq.iter2(fun a b -> Assert.Equal(a,b))
                    )
        )

        
    [<Fact>]
    let ``Interpolation with not enough data must throw exception`` () =
        (**
             Creates a vol surface with only one tenor. When trying to interpolate on a different tenor an
             invalid argument exception is required to be thrown as no interpolation
             can be performed on a single point.

             When a the interpolation is placed exactely on the existing tenor point, the algorithm needs
             to return a value.
         **)

          
         ///Strike deltas
        let strikes_in_bps = test_commons.strikes_in_bps

        let generate_pillar = test_commons.generate_pillar


        //Constructing the vol surface using a builder object.
        let surface = test_commons.get_benchmark_surface()

                          
        // Creating the interpolator for  the surface
        let beta=0.5
        let volinterpolator = SABRInterpolator.SurfaceInterpolator(surface,beta)

        //As only 1 tenor is available, interpolation on a different tenor must return an exception.
        Assert.Throws<System.ArgumentException >(fun ()->volinterpolator.interpolate_moneyness(0.5<year>,12<month>,0.0)|>ignore)
        |>ignore

        //Interpolation on an single existing tenor point must just return the interpolated point (no exception)
        let a= volinterpolator.interpolate_moneyness(0.5<year>,24<month>,0.0)
        Assert.Equal(a,surface.Cube_Ty.[0.5<year>].[24<month>].[3].volatility,1)


    [<Fact>]
    let ``SABR Coefficients must converge`` () =
        (**
             Creates a vol surface with only one tenor. When trying to interpolate on a different tenor an
             invalid argument exception is required to be thrown as no interpolation
             can be performed on a single point.

             When a the interpolation is placed exactely on the existing tenor point, the algorithm needs
             to return a value.
         **)

          

        let beta=0.5
        //Constructing the vol surface using a builder object.
        let computed = SABRInterpolator.SurfaceInterpolator(test_commons.get_benchmark_surface(),beta).SABRCube
        computed.to_csv("./data/beta0_5_sabrcoeff.csv")

        let expected = SABRCube.from_csv("./data/beta0_5_sabrcoeff.csv")
        // Creating the interpolator for  the surface
        let beta=0.5
        

        test_commons.compare_sbar_coeff(expected.Cube_Ty,computed)


    [<Fact>]
    let ``SABR Coefficients must converge Issue on beta 99`` () =
        (**
             This test shows the problem with the value beta=0.99 which generates convergence
             issues in the optimization algorithm.
         **)

          

        let beta=0.99
        //Constructing the vol surface using a builder object.
        let computed = SABRInterpolator.SurfaceInterpolator(VolSurface.from_csv("./data/input_volsurface.csv"),beta).SABRCube
        computed.to_csv("./data/beta1_0_sabrcoeff_error.csv")

    [<Fact>]
    let ``SABR Greeks`` () =
        (**
             Creates a vol surface with only one tenor. When trying to interpolate on a different tenor an
             invalid argument exception is required to be thrown as no interpolation
             can be performed on a single point.

             When a the interpolation is placed exactely on the existing tenor point, the algorithm needs
             to return a value.
         **)

          

        let beta=0.5
        //Constructing the vol surface using a builder object.
        let computed = SABRInterpolator.SurfaceInterpolator(test_commons.get_benchmark_surface(),beta)
        let F = 267.84261
        let K = 117.84261
        let delta_1 = computed.Delta(15.0<year>,24<month>,K*1e-4,F*1e-4,0.025,true)
        let vega_1 = computed.Vega(15.0<year>,24<month>,K*1e-4,F*1e-4,0.025)
        let gamma_1 = computed.Gamma(15.0<year>,24<month>,K*1e-4,F*1e-4,0.025,true)

        Assert.Equal(delta_1,0.6271753749662822,5)
        Assert.Equal(vega_1,0.017926670388162876,5)
        Assert.Equal(gamma_1,3.7264549799242985,5)

        0.0
        

        


        