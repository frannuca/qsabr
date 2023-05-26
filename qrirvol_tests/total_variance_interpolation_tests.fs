module Test_Total_Variance_interpolation
open System
open Xunit
open qirvol.volatility
open qirvol.qtime
open qirvol.qtime.timeconversions
open MathNet.Numerics.LinearAlgebra


type Testing_total_variance_SABR()=                 
    [<Fact>]
    let ``Total Variance Interpolate mush match calibrated smiles on given maturity points`` () =
        (**
            Reads a pre-defined vol surface and performs interpolation for already provided maturity using
            total variance interpolation. Since the maturity is already present in the surface the interpolation
            must exactly match the calibrated smile.
        **)

        let beta=0.5
        
        //Reading pillars from file ..
        let volpillars = SABRInterpolator.get_surface_from_csv("./data/volsurface.csv")

        // Generation of the surface data 
        let surface = VolSurfaceBuilder().withPillars(volpillars).Build()

        //getting the smile for a known maturity and tenor
        let Tknown = 10.0<year>
        let tenor= 5*12<month>
       
        //calibration of the smile at the given maturity and tenor
        let expected_coeff= SABR.sigma_calibrate(surface,10.0,0.2,beta).[Tknown].[tenor].[0]
        //smile interpolator strike -> volatility 
        let expected_interpolator =  SABR.Sigma_SABR_Smile(expected_coeff)

        //here we create the total variance interpolator which should be exactely the same as the calibrated smile above
        let tinterpolator = SABRInterpolator.SABRInterpolator_Total_Variance(surface,0.5)
        let computed_interpolator = tinterpolator.Smile(10.0<year>,5*12<month>)

        let fwd = computed_interpolator.f

        //Check that the smile calibrated and the total variance inteporlated match as the maturity is already part of the surface.
        [| for i in 0 .. 150 -> float(i)*1e-4 + fwd |]
        |> Array.iter(fun k ->
                            let c,e = computed_interpolator.fsmile(k),expected_interpolator(k)
                            //Console.WriteLine($"{e},{c}")
                            Assert.Equal(c,e,0.01))

                            
    [<Fact>]
    let ``Total Variance Interpolate on non-existing maturities must generate not arbritage conditions``() =
        (**
            Reads a pre-defined vol surface and performs interpolation for a non-present  maturity using
            total variance interpolation. This test does not perform any specific comparison but simple
            executes the calculation on two different maturies and expects no exceptions.
            As as result a csv file is created in the data folder at the test binary folder.
            TODO: Find arbritage conditions to be applied in this test in increasing maturities.
        **)

        let beta=0.5
        
        //Reading pillars from file ..
        let volpillars = SABRInterpolator.get_surface_from_csv("./data/volsurface.csv")

        // Generation of the surface data 
        let surface = VolSurfaceBuilder().withPillars(volpillars).Build()

        //getting the smile for a known maturity and tenor
        let T15y = 15.0<year>
        let T17y = 17.0<year>
        let T25y = 25.0<year>
        let tenor= 5*12<month>
       
        
        
        let tinterpolator = SABRInterpolator.SABRInterpolator_Total_Variance(surface,beta)
        let computed_interpolator_15y = tinterpolator.Smile(T15y,tenor)        
        let computed_interpolator_17y = tinterpolator.Smile(T17y,tenor)
        let computed_interpolator_25y = tinterpolator.Smile(T25y,tenor)


        let fwd = computed_interpolator_15y.f

        use file = new System.IO.StreamWriter("./data/interpolation_in_maturity.csv")
        file.WriteLine($"Maturity,tenor,Fwd,Strike,Volatility")
        //Check that the smile calibrated and the total variance inteporlated match as the maturity is already part of the surface.
        [| for i in 0 .. 150 -> float(i)*1e-4 + fwd |]
        |> Array.iter(fun k ->
                            let s_15y,s_17y,s_25y = computed_interpolator_15y.fsmile(k),
                                                    computed_interpolator_17y.fsmile(k),
                                                    computed_interpolator_25y.fsmile(k)

                            file.WriteLine($"{T15y},{tenor},{fwd},{k},{s_15y}")
                            file.WriteLine($"{T17y},{tenor},{fwd},{k},{s_17y}")
                            file.WriteLine($"{T25y},{tenor},{fwd},{k},{s_25y}")
                            )
                            
        

                            
        

     
    
