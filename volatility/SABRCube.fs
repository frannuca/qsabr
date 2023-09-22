namespace qirvol.volatility

open System
open System.Collections.Immutable
open qirvol.qtime
open Deedle


//Each calibrated smile is charaterized by its alpha, beta, nu, rho and current forward 'f'.
    type SABRSolution={texp:float<year>;tenor:float<year>;alpha:float;beta:float;nu:float;rho:float;f:float}

type SABRCsvColumns=
    |Tenor
    |Expiry
    |Fwd
    |Alpha
    |Beta
    |Nu
    |Rho
    with
    override self.ToString()=
        match self with
        |Tenor  -> "Tenor"
        |Expiry -> "Expiry"
        |Fwd    -> "Fwd"
        |Alpha  -> "alpha"
        |Beta   -> "beta"
        |Nu     -> "nu"
        |Rho    -> "rho"
     

    static member FromString(x)=
        match x with
        |"Tenor"  -> Tenor
        |"Expiry" -> Expiry
        |"Fwd"    -> Fwd
        |"alpha"  -> Alpha
        |"beta"   ->  Beta
        |"nu"     -> Nu
        |"rho"    -> Rho
        |_ -> failwith $"Unknown sasbr column name {x}"
///Volatility cube class.
///The ctor. requires an array of VolPillars (or surface points)
type SABRCube(cube:Map<float<year>,Map<float<year>,SABRSolution array>>)=
      inherit BaseSabrCube<SABRSolution>(cube)
        
      override self.to_csv(filepath:string)=        
          let frame = cube
                      |>Seq.map(fun kv -> let texp=kv.Key
                                          let tenorframe = kv.Value
                                          tenorframe
                                          |>Seq.map(fun kv2->
                                                      let tenor=kv2.Key
                                                      let pillars=kv2.Value
                                                      pillars |>
                                                      Seq.map(fun p -> [SABRCsvColumns.Tenor,float(p.tenor)/12.0;
                                                                        SABRCsvColumns.Expiry,float(p.texp);
                                                                        SABRCsvColumns.Fwd,float(p.f)*1e4;
                                                                        SABRCsvColumns.Alpha,float(p.alpha);
                                                                        SABRCsvColumns.Beta,float(p.beta);
                                                                        SABRCsvColumns.Nu,float(p.nu);
                                                                        SABRCsvColumns.Rho,float(p.rho)
                                                                       ]|> Series.ofObservations
                                                      ) 
                                                      
                                          )|> Seq.concat
                      )|>Seq.concat   |> Seq.mapi(fun i x->i,x)                                               
                      |>Frame.ofRows
                      
                      
          frame.SaveCsv(filepath,includeRowKeys=false)
                     
      new(x:System.Collections.Generic.IDictionary<double, System.Collections.Generic.IDictionary<float, SABRSolution[]>>)=
            SABRCube(x
            |> Seq.map(fun kv -> (kv.Key*1.0<year>,kv.Value
                                         |> Seq.map(fun kv2 -> (kv2.Key*1.0<year>,kv2.Value) )
                                         |> Map.ofSeq))
            |> Map.ofSeq
            )
               
      //Factory method from file in csv format builds the associated SABR cube object
      static member from_csv(filepath:string):SABRCube=
          let frame= Frame.ReadCsv(path=filepath)
          let cube = frame
                      |>Frame.mapRowValues(fun series -> {SABRSolution.tenor=series.GetAs<float>(SABRCsvColumns.Tenor.ToString())*1.0<year>;
                                                          SABRSolution.texp=series.GetAs<float>(SABRCsvColumns.Expiry.ToString())*1.0<year>;
                                                          SABRSolution.f=series.GetAs<float>(SABRCsvColumns.Fwd.ToString())*1e-4
                                                          SABRSolution.alpha=series.GetAs<float>(SABRCsvColumns.Alpha.ToString());
                                                          SABRSolution.beta=series.GetAs<float>(SABRCsvColumns.Beta.ToString());
                                                          SABRSolution.nu=series.GetAs<float>(SABRCsvColumns.Nu.ToString());
                                                          SABRSolution.rho=series.GetAs<float>(SABRCsvColumns.Rho.ToString());

                      })|> Series.values
                      |> Seq.groupBy(fun x -> x.texp)
                      |> Map.ofSeq
                      |> Map.map(fun _ frame -> frame |> Array.ofSeq |>Array.groupBy(fun y -> y.tenor) |> Map.ofSeq)

          

          SABRCube(cube)
