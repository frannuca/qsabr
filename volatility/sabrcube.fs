namespace qirvol.volatility

open System
open System.Collections.Immutable
open qirvol.qtime
open qirvol.volatility.SABR

///Volatility cube class.
///The ctor. requires an array of VolPillars (or surface points)
type SabrCube(cube:Map<float<year>,Map<int<month>,SABRSolu array>>)=
    inherit BaseSabrCube<SABRSolu>(cube)
    
    
    //Writes the coefficents cube to csv file.
    override self.to_csv(filepath:string)=
        use file=new System.IO.StreamWriter(filepath)
        file.WriteLine("Tenor,Expiry,Fwd,alpha,beta,nu,rho")
        self.Cube
        |> Map.iter(fun texp_days frame ->
                             let texp = float(texp_days)*1.0<day>*timeconversions.days2year
                             frame
                             |> Map.iter(fun tenor  pillararr ->
                                           let pillar=pillararr.[0]
                                           file.WriteLine($"{float tenor/timeconversions.years2months},{texp},{pillar.f},{pillar.alpha},{pillar.beta},{pillar.nu},{pillar.rho}")))
        
                

