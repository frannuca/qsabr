
namespace qirvol.volatility

open System
open System.Collections.Immutable
open qirvol.qtime


///Volatility cube class.
///The ctor. requires an array of VolPillars (or surface points)
[<AbstractClass>]
type BaseSabrCube<'T>(cube:Map<float<year>,Map<int<month>,'T array>>)=

    ///Map of Map. This filed maps maturity(days) -> tenor<months> -> SABR coefficients array
    ///Limitations are:
    ///    i) the floating key used requires search of maturity with tolerance, to solve this issue maturity as expressed as int<days>
    ///   ii) minimum tenor to be used is 1m as it is declared in month unites.
    /// Swaption quotes are though starting at 1m tenors up to 50y (50x12 month) rendering this option sound.    
    let  cubekeys= cube |> Seq.map(fun kv -> int(kv.Key*timeconversions.years2days)*1<day>,kv.Value) |> Map.ofSeq
    let cube_years = cube                          
    ///Maturities expressed in days. Day unit allows to use int keys for maturities with a resolution of 1day.    
    member self.maturities with get()= cubekeys.Keys |> Array.ofSeq

    ///List of tenors in the surface provided for a given maturity. Tenors are expressed in months.
    member self.tenors_by_maturity(texp:float<year>)=
        cubekeys.[int(texp*timeconversions.years2days)*1<day>].Keys

    ///Extractino of the vol smile for a given expiry and tenor.
    /// Expiries are provided in float<year> but internally converted to days to use int<day> as keys.
    member self.Smile(texp:float<year>,tenor:int<month>)=
        cubekeys.[int(texp*timeconversions.years2days)*1<day>].[tenor]


    member self.Cube with get() = cubekeys
    member self.Cube_Ty  with get()= cube_years
    //Writes the coefficents cube to csv file.
    abstract member to_csv: string -> unit
    