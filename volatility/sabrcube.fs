namespace qirvol.volatility

open System
open System.Collections.Immutable
open qirvol.qtime
open qirvol.volatility.SABR
open Deedle

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
type SabrCube(cube:Map<float<year>,Map<int<month>,SABRSolu array>>)=
    inherit BaseSabrCube<SABRSolu>(cube)
    
    ///Maturities expressed in days. Day unit allows to use int keys for maturities with a resolution of 1day.    
      //Tenor,Expiry,Fwd,Strike,Vol
      /// Serializes into csv the vol sureface
      //file.WriteLine("Tenor,Expiry,Fwd,alpha,beta,nu,rho")
      override self.to_csv(filepath:string)=        
          let frame = cube
                      |>Seq.map(fun kv -> let texp=kv.Key
                                          let tenorframe = kv.Value
                                          tenorframe
                                          |>Seq.map(fun kv2->
                                                      let tenor=kv2.Key
                                                      let pillars=kv2.Value
                                                      pillars |>
                                                      Seq.map(fun p -> [SABRCsvColumns.Tenor,float(p.tenor);
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
                     

      //Factory method from file in csv format builds the associated SABR cube object
      static member from_csv(filepath:string):SabrCube=
          let frame= Frame.ReadCsv(path=filepath)
          let cube = frame
                      |>Frame.mapRowValues(fun series -> {SABRSolu.tenor=series.GetAs<int>(SABRCsvColumns.Tenor.ToString())*1<month>;
                                                          SABRSolu.texp=series.GetAs<float>(SABRCsvColumns.Expiry.ToString())*1.0<year>;
                                                          SABRSolu.f=series.GetAs<float>(SABRCsvColumns.Fwd.ToString())*1e-4
                                                          SABRSolu.alpha=series.GetAs<float>(SABRCsvColumns.Alpha.ToString());
                                                          SABRSolu.beta=series.GetAs<float>(SABRCsvColumns.Beta.ToString());
                                                          SABRSolu.nu=series.GetAs<float>(SABRCsvColumns.Nu.ToString());
                                                          SABRSolu.rho=series.GetAs<float>(SABRCsvColumns.Rho.ToString());

                      })|> Series.values
                      |> Seq.groupBy(fun x -> x.texp)
                      |> Map.ofSeq
                      |> Map.map(fun _ frame -> frame |> Array.ofSeq |>Array.groupBy(fun y -> y.tenor) |> Map.ofSeq)

          

          SabrCube(cube)
    