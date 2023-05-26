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

    let inline tofloat x = float x
    let rec binary_search<'T >(lo:int,hi:int,x:float,arr:float array,tol:float)=        
        match (lo,hi) with        
        |(i,j) when (j-i)=1 || i=j -> (i,j)
        |(i,j) when i>j -> failwith "Wrong indexing. Should not reach here."
        |_ -> let mid= float(lo+hi)*0.5 |> int
              let xmid=arr.[mid]
              if Math.Abs(xmid-x)<tol then (mid,mid)
              elif xmid>x then binary_search(lo,mid,x,arr,tol)
              else  binary_search(mid,hi,x,arr,tol)




    //Record holding a smile interpolation function for a given tenor and maturity.
    type SmileInterpolator={
                            f:float; //Forward on which the smile was calibrated
                            fsmile:float->float //function strike to volatility
    }
    
    type SABRInterpolator_Total_Variance(surface:VolSurface,beta)=
        let cube = SABR.sigma_calibrate(surface,10.0,0.2,beta)
        let maturities =  cube.Keys |> Array.ofSeq |> Array.map(float)

        (**
            Interpolates the SABR volatility for the given maturity using total variance interpolation.
            The tenor provided must be part of the surface, otherwise the function throws.
            Interpolation taken from: (Eq 21) https://www.iasonltd.com/doc/old_rps/2007/2013_The_implied_volatility_surfaces.pdf
        **)              
        member self.Smile(texp:float<year>,tenor:int<month>)=

            //locating the maturity tranche in the sabr cube
            let (lo,hi)=binary_search(0,maturities.Length-1,float texp,maturities,1.0/360.0)
            let Tlo = maturities.[lo]*1.0<year>
            let Thi = maturities.[hi]*1.0<year>


            let c_lo = cube.[Tlo].[tenor].[0]
            let c_hi = cube.[Thi].[tenor].[0]

            let fsigma_lo = SABR.Sigma_SABR_Smile(c_lo)
            let fsigma_hi = SABR.Sigma_SABR_Smile(c_hi)
            let T= texp
            let interpolator strike=
                let s2_lo = fsigma_lo(strike)**2.0
                let s2_hi = fsigma_hi(strike)**2.0
                let s2 = (T-Tlo)/(Thi-Tlo)*Thi/T*s2_hi+(Thi-T)/(Thi-Tlo)*Tlo/T*s2_lo
                Math.Sqrt(s2)

            {SmileInterpolator.f=c_lo.f;SmileInterpolator.fsmile=interpolator}