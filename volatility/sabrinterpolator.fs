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
           
    open MathNet.Numerics.Distributions
    type SurfaceInterpolator(surface:VolSurface,beta:float)=
    
        let cube = SABR.sigma_calibrate(surface,10.0,0.2,beta)

        let rec binary_search(lo:int,hi:int,x:float,arr:float array,tol:float)=
            if arr.Length=0 then invalidArg (nameof arr) $" Array must include at least two for binary search"
            else if arr.Length=1 && Math.Abs(x-arr.[0])>tol then invalidArg (nameof arr) $" Array must include at least two for binary search"
            else if arr.Length=1 && Math.Abs(x-arr.[0])<=tol then (lo,lo)
            else if x < arr.[0] then (lo,lo+1)
            else if x> arr.[arr.Length-1] then (arr.Length-2,arr.Length-1)
            else
                match (lo,hi) with        
                |(i,j) when (j-i)=1 || i=j -> (i,j)
                |(i,j) when i>j -> failwith "Wrong indexing. Should not reach here."
                |_ -> let mid= float(lo+hi)*0.5 |> int
                      let xmid=arr.[mid]
                      if Math.Abs(xmid-x)<tol then (mid,mid)
                      elif xmid>x then binary_search(lo,mid,x,arr,tol)
                      else  binary_search(mid,hi,x,arr,tol)

        let interpolate_in_tenors(T:float<year>,tenor:int<month>,k:float,f:float)=
            let tenors = surface.tenors_by_maturity(T) |> Array.ofSeq 
            let itenor_lo,itenor_hi = binary_search(0,tenors.Length,float tenor,tenors|> Array.map(float),0.5)
            let ten_lo = tenors.[itenor_lo]
            let ten_hi = tenors.[itenor_hi]

            let params_tenorlo = cube.Smile(T,tenors.[itenor_lo]).[0]
            let params_tenorhi = cube.Smile(T,tenors.[itenor_hi]).[0]
            let v_lo = SABR.Sigma_SABR(params_tenorlo.alpha,params_tenorlo.beta,params_tenorlo.nu,T,params_tenorlo.rho,k,f)
            let v_hi = SABR.Sigma_SABR(params_tenorhi.alpha,params_tenorhi.beta,params_tenorhi.nu,T,params_tenorhi.rho,k,f)

            if itenor_lo=itenor_hi then
                v_hi
            else
                v_lo+(v_hi-v_lo)/float (ten_hi - ten_lo) * float (tenor - ten_lo)

        let interpolate_in_tenors_moneyness(T:float<year>,tenor:int<month>,logf_k:float)=
            let tenors = surface.tenors_by_maturity(T) |> Array.ofSeq 
            let itenor_lo,itenor_hi = binary_search(0,tenors.Length,float tenor,tenors|> Array.map(float),0.5)
            let ten_lo = tenors.[itenor_lo]
            let ten_hi = tenors.[itenor_hi]

            
            let params_tenorlo = cube.Smile(T,tenors.[itenor_lo]).[0]
            let params_tenorhi = cube.Smile(T,tenors.[itenor_hi]).[0]
            let k_low=params_tenorlo.f/Math.Exp(logf_k)
            let k_hi=params_tenorhi.f/Math.Exp(logf_k)

            let v_lo = SABR.Sigma_SABR(params_tenorlo.alpha,params_tenorlo.beta,params_tenorlo.nu,T,params_tenorlo.rho,k_low,params_tenorlo.f)
            let v_hi = SABR.Sigma_SABR(params_tenorhi.alpha,params_tenorhi.beta,params_tenorhi.nu,T,params_tenorhi.rho,k_hi,params_tenorhi.f)

            if itenor_lo=itenor_hi then
                v_hi
            else
                v_lo+(v_hi-v_lo)/float (ten_hi - ten_lo) * float (tenor - ten_lo)

        member self.interpolate(texp:float<year>,tenor:int<month>,k:float,f:float)=
            //locating the maturity tranche in the sabr cube
            let maturities = surface.maturities_years |> Array.map(float)
            
            let (lo,hi)=binary_search(0,maturities.Length-1,float texp,maturities,1.0/360.0)
            let Tlo = maturities.[lo]*1.0<year>
            let Thi = maturities.[hi]*1.0<year>

            //inteporlating tenor in low maturity:
            let v_Tlo = interpolate_in_tenors(Tlo,tenor,k,f)           
            let v_Thi = interpolate_in_tenors(Thi,tenor,k,f)

            if Tlo=Thi then
                v_Tlo
            else                 
                let T= texp
            
                let s2_lo = v_Tlo**2.0
                let s2_hi = v_Thi**2.0
                let s2 = (T-Tlo)/(Thi-Tlo)*Thi/T*s2_hi+(Thi-T)/(Thi-Tlo)*Tlo/T*s2_lo
                Math.Sqrt(s2)
            
        
        member self.interpolate_moneyness(texp:float<year>,tenor:int<month>,logf_k:float)=
            //locating the maturity tranche in the sabr cube
            let maturities = surface.maturities_years |> Array.map(float)
            
            let (lo,hi)=binary_search(0,maturities.Length-1,float texp,maturities,1.0/360.0)
            let Tlo = maturities.[lo]*1.0<year>
            let Thi = maturities.[hi]*1.0<year>

            //inteporlating tenor in low maturity:
            let v_Tlo = interpolate_in_tenors_moneyness(Tlo,tenor,logf_k)           
            let v_Thi = interpolate_in_tenors_moneyness(Thi,tenor,logf_k)
           
            if Tlo = Thi then
                v_Tlo
            else
                let T= texp
            
                let s2_lo = v_Tlo**2.0
                let s2_hi = v_Thi**2.0
                let s2 = (T-Tlo)/(Thi-Tlo)*Thi/T*s2_hi+(Thi-T)/(Thi-Tlo)*Tlo/T*s2_lo
                Math.Sqrt(s2)
            
    
    
        member self.resample_surface(expiries:float<year> array,tenors:int<month> array,logf_k:float array,fwd:float)=
            //resampling the vol surface:
            let slogf_k = logf_k |> Array.sortDescending
            expiries
                |> Array.map(fun texp ->
                                    texp,
                                    tenors
                                    |> Array.map(fun tenor ->
                                                        tenor,
                                                        slogf_k
                                                        |> Array.map(fun log_f_k ->
                                                                    {VolPillar.forwardrate=fwd;
                                                                     VolPillar.maturity=texp;
                                                                     VolPillar.strike=fwd/Math.Exp(log_f_k);
                                                                     VolPillar.tenor=tenor;
                                                                     VolPillar.volatility=self.interpolate_moneyness(texp,tenor,log_f_k)
                                                                    })
                                    ) |> Map.ofArray
                )|>Map.ofArray
                |> VolSurface

        member self.get_smile(expirie:float<year>,tenor:int<month>,logf_k:float array,fwd:float)=
            //resampling the vol surface:
            self.resample_surface([|expirie|],[|tenor|],logf_k,fwd)

        member self.SABRCube with get()=cube
        
             
        member self.Vega(T:float<year>,tenor:int<month>,K:float,F:float,r:float)=
             let sigma_F = self.interpolate(T,tenor,K,F)
             let sigma_ATM = self.interpolate(T,tenor,F,F)
             let d1 = 1.0/(sigma_F*Math.Sqrt(float T))*(Math.Log(F/K)+sigma_F**2.0/2.0* float T)         
             let bsvega = Math.Exp(-r*float T)*F*Normal.PDF(0.0,1.0,d1)*Math.Sqrt(float T)

             bsvega*sigma_F/sigma_ATM

         //https://www.next-finance.net/IMG/pdf/pdf_SABR.pdf (3.9)
        member self.Delta(T:float<year>,tenor:int<month>,K:float,F:float,r:float,isCall:bool)=
            let sigma= self.interpolate(T,tenor,K,F)
            
            
            let d1 = 1.0/(sigma*Math.Sqrt(float T))*(Math.Log(F/K)+sigma**2.0/2.0* float T)
            
            let bsDelta =
                    let aux = Math.Exp(-r* float T)*Normal.CDF(0.0,1.0,d1)
                    if isCall then aux else aux - 1.0

            let sigma = self.interpolate(T,tenor,K,F)                        
            let bsvega = Math.Exp(-r*float T)*F*Normal.PDF(0.0,1.0,d1)*Math.Sqrt(float T)

            let xoptfunc = System.Func<float array, float>(fun (x:float array) -> self.interpolate(T,tenor,K,x.[0]))
            let gg =   new FiniteDifferences(1, xoptfunc,1,1e-6)

            let dsigma_F = gg.Gradient([|F|]).[0]
            bsDelta+bsvega*dsigma_F

        member self.Gamma(T:float<year>,tenor:int<month>,K:float,F:float,r:float,isCall:bool)=
            
            let xoptfunc = System.Func<float array, float>(fun (x:float array) -> self.Delta(T,tenor,K,x.[0],r,isCall))
            let gg =   new FiniteDifferences(1, xoptfunc,1,1e-6)

            let d_delta_F = gg.Gradient([|F|]).[0]
            d_delta_F

