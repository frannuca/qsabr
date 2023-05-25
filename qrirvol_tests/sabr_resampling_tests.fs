


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
            The new surface is serialized back to drive.
        **)

        
        //Reading pillars from file ..
        let volpillars = SABRInterpolator.get_surface_from_csv("./data/volsurface.csv")

        // Generation of the surface data ...
        let surface = VolSurfaceBuilder().withPillars(volpillars).Build()


        // Resampling the surface with 1000  strike samples
        let resampled_surface,sabrcube = SABRInterpolator.get_cube_coeff_and_resampled_volsurface(surface,0.5,-150.0,150.0,1000)

       
        resampled_surface.to_csv("./data/resampled_300.csv")
        sabrcube.to_csv("./data/sabrcube.csv")

        
        0.0