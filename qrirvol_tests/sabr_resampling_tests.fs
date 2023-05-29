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
    let ``File operations checks`` () =
        (**
            Reads vol surface from csv, calibrates SABR and resamples in strike.
            The new surface is serialized back to drive and re-read again to check that original
            and recovered are the same.
        **)

        
        //Reading pillars from file ..
        let volpillars = SABRInterpolator.get_surface_from_csv("./data/input_volsurface.csv")

        // Generation of the surface data ...
        let surface = VolSurfaceBuilder().withPillars(volpillars).Build()


        let nstrikes=50
        let beta=0.5
        let minStrikeSpread = -150.0
        let maxStrikeSpread = 50.0

        // Resampling the surface with 1000  strike samples
        let computed_resampled_surface,computed_sabrcube = SABRInterpolator.get_cube_coeff_and_resampled_volsurface(surface,beta,minStrikeSpread,maxStrikeSpread,nstrikes)

        computed_resampled_surface.to_csv("./data/output_resampled.csv")
        computed_sabrcube.to_csv("./data/output_sabrcube.csv")
        //getting expected results:
        let expected_surface = VolSurface.from_csv("./data/output_resampled.csv")
        let expected_sabr = SabrCube.from_csv("./data/output_sabrcube.csv")

        //Checks on surface maturities
        Assert.Equal(computed_resampled_surface.maturities.Length,expected_surface.maturities.Length)
        computed_resampled_surface.maturities|>Array.iteri(fun i m -> Assert.Equal(m,expected_surface.maturities.[i]))

        computed_resampled_surface.Cube_Ty
        |> Map.iter(fun (t:float<year>) (frame:Map<int<month>,VolPillar array>) ->
                            frame
                            |> Map.iter(fun tenor  pillars ->
                                            let epillars = expected_surface.Cube_Ty.[t].[tenor]
                                            (pillars,epillars) ||> Array.iter2(fun a b ->
                                                                        Assert.Equal(a.forwardrate,b.forwardrate,2)
                                                                        Assert.Equal(a.strike,b.strike,2)
                                                                        Assert.Equal(int a.tenor,int b.tenor)
                                                                        Assert.Equal(float a.maturity,float b.maturity,1)
                                            )
                            ))

        computed_sabrcube.Cube_Ty
        |> Map.iter(fun (t:float<year>) (frame:Map<int<month>,SABRSolu array>) ->
                            frame
                            |> Map.iter(fun tenor  pillars ->
                                            let epillars = expected_sabr.Cube_Ty.[t].[tenor]
                                            (pillars,epillars) ||> Array.iter2(fun a b ->
                                                                        Assert.Equal(a.alpha,b.alpha,2)
                                                                        Assert.Equal(a.beta,b.beta,2)
                                                                        Assert.Equal(a.nu,b.nu,2)
                                                                        Assert.Equal(a.rho,b.rho,2)
                                                                        Assert.Equal(int a.tenor,int b.tenor)
                                                                        Assert.Equal(float a.texp,float b.texp,1)
                                            )
                            ))

        0.0