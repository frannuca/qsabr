namespace qirvol.volatility

open System
open System.Collections
open MathNet.Numerics
open qirvol
open qirvol.qtime
open MathNet.Numerics.Optimization
open Accord.Math.Differentiation
open MathNet.Numerics.LinearAlgebra
open MathNet.Numerics.LinearAlgebra.Double;

module SABRInterpolator=
    open qdata.csvextractor    

    ///Loads a csv file with volatility surface data and generates the corresponding array of Volatility pillars.
    ///inputs csv and returns VolPillar[]
    ///This function can be used in comp
    let get_surface_from_csv(path2file:string)=
        VolSurfaceCsvHelper.ExtractSurface(path2file)
        |> Array.ofSeq
        |> Array.map(fun pillar -> {forwardrate=pillar.Fwd;
                                  maturity=pillar.Expiry*1.0<year>;
                                  tenor= int(pillar.Tenor*qtime.timeconversions.years2months)*1<month>;
                                  volatility=pillar.Vol;
                                  strike=pillar.Strike})
        

    ///Given an existing volatility surface it re-samples the strike for all the smiles as per user specification.
    ///This method re-alibrates the surface to obtained the relevant SABR cube.
    ///Params:
    /// cube: volatility surface
    /// beta: SABR exponent coefficient
    /// minStrikeSpread: lower values from which the strike will start (as the spread with respect to the forward)
    /// maxStrikeSpread: upper values from which the strike will end (as the spread with respect to the forward)
    let  get_cube_coeff_and_resampled_volsurface(cube:VolSurface,beta:float,minStrikeSpread:float,maxStrikeSpread:float,nstrikes:int)=                
        
            let surface = SABR.sigma_calibrate(cube,10.0,0.2,beta)
            
            let strikes_bps = [|0 .. nstrikes-1|]
                                |> Array.map(fun n -> float n/(float nstrikes-1.0)*(maxStrikeSpread - minStrikeSpread) + minStrikeSpread)
            let res=
                    surface            
                    |>Map.map(fun t tenorframe ->
                                       tenorframe
                                       |>  Map.map(fun tenor paramarr ->
                                               let param= paramarr |> Array.last
                                               strikes_bps
                                               |> Array.map(fun dk ->                                                                    
                                                                      let v = SABR.Sigma_SABR(param.alpha,param.beta,param.nu,param.texp,param.rho,param.f+dk*1e-4,param.f)                                                              
                                                                      {VolPillar.forwardrate=param.f;VolPillar.maturity=t;VolPillar.strike=param.f+dk*1e-4;VolPillar.tenor=tenor;VolPillar.volatility=v}
                                                                      )))

            VolSurfaceBuilder().withMap(res).Build(),SabrCube(surface)