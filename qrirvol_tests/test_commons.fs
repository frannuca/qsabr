module test_commons
open System
open qirvol.volatility
open qirvol.qtime
open qirvol.qtime.timeconversions
open MathNet.Numerics.LinearAlgebra
open qirvol.qtime
open qirvol.volatility.SABR
open Xunit
    ///Strike deltas
    let strikes_in_bps = [|-150.0;-100.0;-50.0;-25.0;0.0;25.0;50.0;100.0;150.0|]

    let generate_pillar(tenor:int<month>,texp:float<year>,f:float,sigmaB:float array)=
        strikes_in_bps
        |> Array.mapi(fun i dk -> {maturity=texp;strike=f+dk*1e-4;VolPillar.tenor=tenor;VolPillar.volatility=sigmaB.[i];VolPillar.forwardrate=f})
              
    let compare_surfaces (strikes_in_bps,res:Map<float<year>,Map<int<month>,SABRSolu array>>,surface:VolSurface)=
        res
        |> Map.iter(fun (T:float<year>) (tenor2param:Map<int<month>,SABR.SABRSolu array>) ->
                          tenor2param
                          |> Map.iter(fun tenor x ->
                                        let computed = strikes_in_bps                                                   
                                                        |> Array.map(fun k-> SABR.Sigma_SABR(x.[0].alpha,x.[0].beta,x.[0].nu,T,x.[0].rho,x.[0].f+k*1e-4,x.[0].f))
                                                    
                                        let expected = surface.Smile(T,tenor)
                                                        |> Array.map(fun y -> y.volatility)
                                                    

                                        let computed_no_zeros=
                                                                if computed.Length > expected.Length then
                                                                    computed.[1..]
                                                                else
                                                                    expected

                                        (computed_no_zeros,expected)
                                        ||> Array.iter2(fun a b -> Assert.Equal(a,b,0.15))))
    

    let compare_sbar_coeff (res:Map<float<year>,Map<int<month>,SABRSolu array>>,surface:SabrCube)=
           res
           |> Map.iter(fun (T:float<year>) (tenor2param:Map<int<month>,SABR.SABRSolu array>) ->
                             tenor2param
                             |> Map.iter(fun tenor x ->
                                           let computed = x.[0]
                                           let expected = surface.Smile(T,tenor).[0]
                                           Assert.Equal(computed.alpha,expected.alpha,1)
                                           Assert.Equal(computed.nu,expected.nu,1)
                                           Assert.Equal(computed.rho,expected.rho,1)
                                           Assert.Equal(computed.beta,expected.beta,1)

                             ))
                                                           
                                                       

      
    let get_benchmark_surface()=
         ///Strike deltas
        let strikes_in_bps = [|-150.0;-100.0;-50.0;-25.0;0.0;25.0;50.0;100.0;150.0|]

        let generate_pillar(tenor:int<month>,texp:float<year>,f:float,sigmaB:float array)=
            strikes_in_bps
            |> Array.mapi(fun i dk -> {maturity=texp;strike=f+dk*1e-4;VolPillar.tenor=tenor;VolPillar.volatility=sigmaB.[i];VolPillar.forwardrate=f})
           
        VolSurfaceBuilder()
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