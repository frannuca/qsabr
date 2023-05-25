namespace qirvol.volatility

open System
open System.Collections.Immutable
open qirvol.qtime

///Volatility data structure to identify a single point in the volatility surface. 
///i.e: maturity, tenor, strike and vol surface point.
type VolPillar = {tenor:int<month>;strike:float;maturity:float<year>;volatility:float;forwardrate:float;}


///Volatility cube class.
///The ctor. requires an array of VolPillars (or surface points)
type VolSurface(pillars:VolPillar array)=

    ///Map of Map. This filed maps maturity(days) -> tenor<months> -> VolPillar array
    ///Limitations are:
    ///    i) the floating key used requires search of maturity with tolerance, to solve this issue maturity as expressed as int<days>
    ///   ii) minimum tenor to be used is 1m as it is declared in month unites.
    /// Swaption quotes are though starting at 1m tenors up to 50y (50x12 month) rendering this option sound.    
    let  cubekeys=
                    pillars
                    |> Array.groupBy(fun p -> int(p.maturity*timeconversions.years2days)*1<day>)
                    |> Map.ofSeq
                    |> Map.map(fun texp volarr -> volarr |> Array.groupBy(fun p -> p.tenor)
                                                         |> Map.ofArray
                                                         |> Map.map(fun _ x -> x|> Array.sortBy(fun x -> x.strike)))
                    
           


    ///Maturities expressed in days. Day unit allows to use int keys for maturities with a resolution of 1day.    
    member self.maturities with get()= cubekeys.Keys |> Array.ofSeq

    ///List of tenors in the surface provided for a given maturity. Tenors are expressed in months.
    member self.tenors_by_maturity(texp:float<year>)=
        cubekeys.[int(texp*timeconversions.years2days)*1<day>].Keys

    ///Extractino of the vol smile for a given expiry and tenor.
    /// Expiries are provided in float<year> but internally converted to days to use int<day> as keys.
    member self.Smile(texp:float<year>,tenor:int<month>)=
        cubekeys.[int(texp*timeconversions.years2days)*1<day>].[tenor]

    /// Serializes into csv the vol sureface
    member self.to_csv(filepath:string)=
        use file=new System.IO.StreamWriter(filepath)
        file.WriteLine("Tenor,Expiry,Fwd,Strike,Vol")
        cubekeys
        |> Map.iter(fun texp_days frame ->
                             let texp = float(texp_days)*1.0<day>*timeconversions.days2year
                             frame
                             |> Map.iter(fun tenor  pillars ->
                                            pillars
                                            |> Array.iter(fun pillar ->
                                                            file.WriteLine($"{float tenor/timeconversions.years2months},{texp},{pillar.forwardrate},{pillar.strike},{pillar.volatility}")))
        )
                

///Helper vol surface builder.
type VolSurfaceBuilder()=
    let _pillars = new System.Collections.Generic.List<VolPillar>()


    ///Add to the surface a new point (VolPillar)
    member self.withPillar(vol:VolPillar)=
        _pillars.Add(vol)
        self


    ///Add to the surface a sequence of points (VolPillar's)
    member self.withPillars(vol:VolPillar seq)=
        _pillars.AddRange(vol |> Seq.filter(fun p -> p.volatility>0.0))
        self

    member self.withMap(m:Map<float<year>,Map<int<month>,VolPillar array>>)=        
        m |> Map.iter(fun _ frme -> frme |> Map.iter(fun _ x -> _pillars.AddRange(x)))
        self
        
    /// builds the surface  and returns a VolSurface object.
    ///TODO: Add validation logic, throwing exceptions in case of surface mispecifications.
    member self.Build()=
        VolSurface(_pillars.ToArray())