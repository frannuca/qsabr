// See https://aka.ms/new-console-template for more information
using static System.Net.Mime.MediaTypeNames;
using System;
using qirvol.volatility;
using System;
using System.CommandLine;
using System.IO;
using Microsoft.FSharp.Linq.RuntimeHelpers;
using Microsoft.VisualBasic.FileIO;
using qirvol.comands;
using CommandLine;
using qirvol.comands;
// Main Method
static class Program {
    static public void Main(String[] args)
    {

        Parser.Default.ParseArguments<CVolSurfaceData>(args)
                  .WithParsed<CVolSurfaceData>(o =>
                  {
                      o.Execute();
                  });

        Console.WriteLine("Success!!");
        
    }
}