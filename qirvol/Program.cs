// See https://aka.ms/new-console-template for more information
using static System.Net.Mime.MediaTypeNames;
using System;
using System;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using qirvol.comands;
using CommandLine;

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
