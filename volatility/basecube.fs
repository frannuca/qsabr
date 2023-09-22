
namespace qirvol.volatility

open System
open System.Collections.Immutable
open qirvol.qtime



///Volatility cube class.
///The ctor. requires an array of cube points of type T.
//Ctors requires a map from maturity in year -> tenor in months -> array of point (vol-strike or sabr coefficients).
[<AbstractClass>]
type BaseSabrCube<'T>(cube:Map<float<year>,Map<float<year>,'T array>>)=
    
    let KeyRoundTol:int = 4;
    ///Map of Map. This filed maps maturity(days) -> tenor<months> -> SABR coefficients array
    ///Limitations are:
    ///    i) the floating key used requires search of maturity with tolerance, to solve this issue maturity as expressed as int<days>
    ///   ii) minimum tenor to be used is 1m as it is declared in month unites.
    /// Swaption quotes are though starting at 1m tenors up to 50y (50x12 month) rendering this option sound.    
    let  cube_expirations= cube |>  Seq.map(fun kv -> Math.Round(float kv.Key,4)*1.0<year>)
    let _cube = cube |> Seq.map(fun kv -> Math.Round(float kv.Key,KeyRoundTol)*1.0<year>,
                                          kv.Value |> Seq.map(fun kv2 -> Math.Round(float kv2.Key,KeyRoundTol)*1.0<year>,kv2.Value)
                                          |> Map.ofSeq
                                         )|> Map.ofSeq
                                            
    ///Maturities expressed in days. Day unit allows to use int keys for maturities with a resolution of 1day.    
    member self.maturities with get()= cube_expirations |> Seq.sort |> Array.ofSeq
    
    ///List of tenors in the surface provided for a given maturity. Tenors are expressed in months.
    member self.tenors_by_maturity(texp:float<year>)=
        _cube.[Math.Round(float texp,KeyRoundTol)*1.0<year>].Keys |> Array.ofSeq

    ///Extractino of the vol smile for a given expiry and tenor.
    /// Expiries are provided in float<year> but internally converted to days to use int<day> as keys.
    member self.Smile(texp:float<year>,tenor:float<year>)=
        let texpx = Math.Round(float texp,KeyRoundTol)*1.0<year>
        let tenorx = Math.Round(float tenor,KeyRoundTol)*1.0<year>
        _cube.[texpx].[tenorx]


    member self.Cube with get() = _cube    
    //Writes the coefficents cube to csv file.
    abstract member to_csv: string -> unit
    