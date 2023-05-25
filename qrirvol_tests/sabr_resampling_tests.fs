


module Test_SURFACE_RESAMPLING_SABR

open System
open Xunit
open qirvol.volatility
open qirvol.qtime
open qirvol.qtime.timeconversions
open MathNet.Numerics.LinearAlgebra

type Testing_Surface_SABR()=                 
    [<Fact>]
    let ``File operations checks`` () =
        (**
            Reads vol surface from csv, calibrates SABR and resamples in strike.
            The new surface is serialized back to drive and re-read again to check that original
            and recovered are the same.
        **)

        
        //Reading pillars from file ..
        let volpillars = SABRInterpolator.get_surface_from_csv("./data/volsurface.csv")

        // Generation of the surface data ...
        let surface = VolSurfaceBuilder().withPillars(volpillars).Build()


        // Resampling the surface with 1000  strike samples
        let resampled_surface,sabrcube = SABRInterpolator.get_cube_coeff_and_resampled_volsurface(surface,0.5,-150.0,150.0,1000)


        //Serializing to disk
        resampled_surface.to_csv("./data/resampled_1000.csv")
        sabrcube.to_csv("./data/sabrcube.csv")

        //Recovering the vol surface back:
        let surface_from_disk = VolSurfaceBuilder().withPillars(SABRInterpolator.get_surface_from_csv("./data/resampled_1000.csv")).Build()

        Assert.Equal(surface_from_disk.maturities.Length,resampled_surface.maturities.Length)

        //Checking one smile volatilility values.
        let expected_smile,computed_smile = (surface_from_disk.Smile(2.0<year>,24<month>),resampled_surface.Smile(2.0<year>,24<month>))
        (expected_smile,computed_smile) ||> Array.iter2(fun a b -> Assert.Equal(a.volatility,b.volatility))
            
        0.0