using Microsoft.Research.Science.FetchClimate2;
using System;
using System.IO;
using System.Linq;

public class FetchConfigApp
{
    static void Main(string[] args)
    {
        Console.Title = "FetchClimate2 Configuration Utility";
        var parser = new FetchParser();
        parser.Start(args.SelectMany(a =>
        {
            try
            {
                return File.ReadAllLines(a);
            }
            catch (Exception exc)
            {
                using (new ForegroundColor(ConsoleColor.Red))
                    Console.WriteLine("Error reading file {0}: {1}", a, exc.Message);
                return new string[0];
            }
        }).ToArray());
    }
}