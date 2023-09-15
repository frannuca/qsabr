namespace qirvol.volatility

open System
open System.Collections.Immutable
open qirvol.qtime
open Deedle

///Volatility data structure to identify a single point in the volatility surface. 
///i.e: maturity, tenor, strike and vol surface point.
type VolPillar = {tenor:int<month>;strike:float;maturity:float<year>;volatility:float;forwardrate:float;}
type SurfaceCsvColumns=
    |Tenor
    |Expiry
    |Fwd
    |Strike
    |Vol
    with
    override self.ToString()=
        match self with
        |Tenor -> "Tenor"
        |Expiry -> "Expiry"
        |Fwd -> "Fwd"
        |Strike -> "Strike"
        |Vol -> "Vol"

    static member FromString(x)=
        match x with
        |"Tenor" -> Tenor
        |"Expiry" -> Expiry
        |"Fwd" -> Fwd
        |"Strike" -> Strike
        |"Vol" -> Vol
        |_ -> failwith $"Unknown surface column name {x}"


///Volatility cube class.
///The ctor. requires an array of VolPillars (or surface points)
type VolSurface(cube:Map<float<year>,Map<int<month>,VolPillar array>>)=
    
    inherit BaseSabrCube<VolPillar>(cube)
    ///Maturities expressed in days. Day unit allows to use int keys for maturities with a resolution of 1day.    
    //Tenor,Expiry,Fwd,Strike,Vol
    /// Serializes into csv the vol sureface

    new(dcube:System.Collections.Generic.IDictionary<float<year>,System.Collections.Generic.IDictionary<int<month>,VolPillar array>>)=
        let cube = dcube |> Seq.map(fun kv -> kv.Key,kv.Value |> Seq.map(fun kv2 -> kv2.Key,kv2.Value) |> Map.ofSeq) |> Map.ofSeq
        VolSurface(cube)
   
        
    override self.to_csv(filepath:string)=        
        let frame = cube
                    |>Seq.map(fun kv -> let texp=kv.Key
                                        let tenorframe = kv.Value
                                        tenorframe
                                        |>Seq.map(fun kv2->
                                                    let tenor=kv2.Key
                                                    let pillars=kv2.Value
                                                    pillars |>
                                                    Seq.map(fun p -> [SurfaceCsvColumns.Tenor.ToString(),float(p.tenor)/12.0;
                                                                      SurfaceCsvColumns.Expiry.ToString(),float(p.maturity);
                                                                      SurfaceCsvColumns.Fwd.ToString(),p.forwardrate*1e4;
                                                                      SurfaceCsvColumns.Strike.ToString(),p.strike*1e4;
                                                                      SurfaceCsvColumns.Vol.ToString(),p.volatility*100.0]
                                                                      |>Series.ofObservations)

                                        )|> Seq.concat
                    )|> Seq.concat|>Seq.mapi(fun i x -> i,x)|>Frame.ofRows

             
        frame.SaveCsv(path=filepath,separator=',',culture= System.Globalization.CultureInfo.InvariantCulture)
                   

    //Factory method from file in csv format builds the associated VolSurface object
    static member from_csv(filepath:string):VolSurface=
        let us = "en-US"       
        let frame= Frame.ReadCsv(path=filepath,hasHeaders=true,separators=",",culture="")
        let cube = frame
                    |>Frame.mapRowValues(fun series -> {VolPillar.maturity=series.GetAs<float>(SurfaceCsvColumns.Expiry.ToString())*1.0<year>;
                                                        VolPillar.tenor= int(series.GetAs<float>(SurfaceCsvColumns.Tenor.ToString())*12.0)*1<month>;
                                                        VolPillar.forwardrate=series.GetAs<float>(SurfaceCsvColumns.Fwd.ToString())*1e-4
                                                        VolPillar.strike=series.GetAs<double>(SurfaceCsvColumns.Strike.ToString())*1e-4;
                                                        VolPillar.volatility=series.GetAs<float>(SurfaceCsvColumns.Vol.ToString())*1e-2

                    })|> Series.values
                    |> Seq.groupBy(fun x -> x.maturity)
                    |> Map.ofSeq
                    |> Map.map(fun _ frame -> frame |> Array.ofSeq |>Array.groupBy(fun y -> y.tenor) |> Map.ofSeq)

        

        VolSurface(cube)
    static member from_frame(frame:Frame<int,string>)=        
        let cube = frame
                    |>Frame.mapRowValues(fun series -> {VolPillar.maturity=series.GetAs<float>(SurfaceCsvColumns.Expiry.ToString())*1.0<year>;
                                                        VolPillar.tenor= int(series.GetAs<float>(SurfaceCsvColumns.Tenor.ToString())*12.0)*1<month>;
                                                        VolPillar.forwardrate=series.GetAs<float>(SurfaceCsvColumns.Fwd.ToString())*1e-4
                                                        VolPillar.strike=series.GetAs<double>(SurfaceCsvColumns.Strike.ToString())*1e-4;
                                                        VolPillar.volatility=series.GetAs<float>(SurfaceCsvColumns.Vol.ToString())*1e-2

                    })|> Series.values
                    |> Seq.groupBy(fun x -> x.maturity)
                    |> Map.ofSeq
                    |> Map.map(fun _ frame -> frame |> Array.ofSeq |>Array.groupBy(fun y -> y.tenor) |> Map.ofSeq)
        VolSurface(cube)       

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
        ///Map of Map. This filed maps maturity(days) -> tenor<months> -> VolPillar array
        ///Limitations are:
        ///    i) the floating key used requires search of maturity with tolerance, to solve this issue maturity as expressed as int<days>
        ///   ii) minimum tenor to be used is 1m as it is declared in month unites.
        /// Swaption quotes are though starting at 1m tenors up to 50y (50x12 month) rendering this option sound.    
        let  cubekeys=
                        _pillars.ToArray()
                        |> Array.groupBy(fun p -> p.maturity)
                        |> Map.ofSeq
                        |> Map.map(fun texp volarr -> volarr |> Array.groupBy(fun p -> p.tenor)
                                                             |> Map.ofArray
                                                             |> Map.map(fun _ x -> x|> Array.sortBy(fun x -> x.strike)))
                        
               
        VolSurface(cubekeys)