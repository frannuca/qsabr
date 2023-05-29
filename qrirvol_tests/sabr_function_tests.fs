
module Test_SABR

open System
open Xunit
open qirvol.volatility
open qirvol.qtime
open qirvol.qtime.timeconversions
open MathNet.Numerics.LinearAlgebra

type Testing_Funtion_SABR()=
    
   
    [<Fact>]
    let ``Cubic root finder for selected atm SABR solver must match`` () =
        (**
            The alpha parameter is not really optimized, but rather for each
            nu and rho we resolve the ATM black volatitily as the solution
            of a polynome of order 3 which guarantees at least one real solution.
            This test checks that the alpha is really the solution to recover
            the ATM vol.
        **)

        let atmvol=0.25
        
        let nu=0.05
        let te= 1.0<year>
        let rho=0.8
        let f =  0.028436364
        let beta = 0.5        
        

        //resolving atm for the given parameters
        let alpha = SABR.Solve_alpha_for_ATM(atmvol,beta,nu,te,rho,f)

        //re-applying SABR interpolation using the calibrated alpha which should match the original one.
        let atm_computed = SABR.Sigma_SABR_ATM(alpha,beta,nu,te,rho,f)
        Assert.Equal(atmvol,atm_computed,6)


    [<Fact>]
    let ``Negative forwards, Strikes or alphas must return zero vol`` () =
        (**
            The SABR model here implemented does not allow negative forwards
            or strikes and hence the functions accepting these parameter must
            avoid numerical issue by returning zero.
            Alpha coefficient represents a volatility and volatility
            calculation with this coefficient negative are returned as zero.
        **)

        let atmvol=0.25
        
        let nu=0.05
        let te= 1.0<year>
        let rho=0.8
        let K=0.03
        let f =  0.028436364
        let beta = 0.5
        let alpha=0.4
        

        //resolving atm for the given parameters
        Assert.Equal(SABR.Solve_alpha_for_ATM(-1.0,beta,nu,te,rho,f),0.0)
        Assert.Equal(SABR.Solve_alpha_for_ATM(atmvol,beta,nu,te,rho,-1.0),0.0)
        Assert.Equal(SABR.Sigma_SABR_ATM(alpha,beta,nu,te,rho,-1.0),0.0)
        Assert.Equal(SABR.Sigma_SABR_ATM(-1.0,beta,nu,te,rho,f),0.0)
        Assert.Equal(SABR.Sigma_SABR(-1.0,beta,nu,te,rho,K,f),0.0)
        Assert.Equal(SABR.Sigma_SABR(alpha,beta,nu,te,rho,-1.0,f),0.0)
        Assert.Equal(SABR.Sigma_SABR(alpha,beta,nu,te,rho,K,-1.0),0.0)
        Assert.Equal(SABR.Sigma_SABR(0.0,beta,nu,te,rho,K,f),0.0)

        



    