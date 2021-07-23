using CommandLine;

using Rocksmith2014.XML;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace ShowLightGenerator.CLI
{
    public static class Program
    {
        public class Options
        {
            [Option('f',
                "FogSourceArrangement",
                Required = true,
                HelpText = "Sets the file from which fog colors are generated.")]
            public string? FogSourceArrangement { get; set; }

            [Option("FogMethod",
                Required = false,
                Default = "bars",
                HelpText = "Sets the method used for fog generation (Options: bars, sections, octave, chords, time).")]
            public string? FogMethod { get; set; }

            [Option("FogBars",
                Required = false,
                Default = 16,
                HelpText = "Sets the minimum number of measures between fog note changes when using the 'bars' method.")]
            public int FogBars { get; set; }

            [Option("FogTime",
                Required = false,
                Default = 5.0f,
                HelpText = "Sets the minimum time in seconds between fog note changes when using the 'time' method.")]
            public float FogTime { get; set; }

            [Option('b',
                "BeamSourceArrangement",
                Required = false,
                HelpText = "(Default: Same as fog) Sets the file from which beam colors are generated.")]
            public string? BeamSourceArrangement { get; set; }

            [Option("BeamMethod",
                Required = false,
                Default = "time",
                HelpText = "Sets the method used for beam generation (Options: time, fog).")]
            public string? BeamMethod { get; set; }

            [Option("BeamTime",
                Required = false,
                Default = 0.5f,
                HelpText = "Sets the minimum time in seconds between beam note changes when using the 'time' method.")]
            public float BeamTime { get; set; }

            [Option('o',
                "Output",
                Required = false,
                HelpText = "(Default: './showlights.xml') Sets the path for the XML file that will be generated.")]
            public string? TargetPath { get; set; }

            [Option('l',
                "EnableLasers",
                Required = false,
                Default = true,
                HelpText = "Enables laser lights.")]
            public bool? EnableLasers { get; set; }

            [Option('c',
                "UseCompatibleColors",
                Required = false,
                Default = false,
                HelpText = "When enabled, color combinations that may look bad are avoided.")]
            public bool UseCompatibleColors { get; set; }

            [Option("Randomize",
                Required = false,
                Max = 2,
                HelpText = "Enables or disables randomization.")]
            public IEnumerable<string> Randomize { get; set; } = Enumerable.Empty<string>();

            [Option('v',
                "Verbose",
                Required = false,
                Default = false,
                HelpText = "If enabled, prints the selected options to the console.")]
            public bool Verbose { get; set; }
        }

        private static BeamGenerationMethod ParseBeamMethod(string? arg) =>
            arg?.ToLowerInvariant() switch
            {
                "fog" => BeamGenerationMethod.FollowFogNotes,
                _ => BeamGenerationMethod.MinTimeBetweenChanges
            };

        private static FogGenerationMethod ParseFogMethod(string? arg) =>
            arg?.ToLowerInvariant() switch
            {
                "bars" => FogGenerationMethod.ChangeEveryNthBar,
                "sections" => FogGenerationMethod.FromSectionNames,
                "octave" => FogGenerationMethod.FromLowestOctaveNotes,
                "chords" => FogGenerationMethod.FromChords,
                "time" => FogGenerationMethod.MinTimeBetweenChanges,
                _ => FogGenerationMethod.ChangeEveryNthBar
            };


        private static int Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            int returnCode = 0;

            Parser.Default
                .ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    var beamSourceArrangement = o.BeamSourceArrangement switch
                    {
                        null => o.FogSourceArrangement,
                        _ => o.BeamSourceArrangement
                    };

                    var targetPath = o.TargetPath switch
                    {
                        null => Path.Combine(".", "showlights.xml"),
                        _ => o.TargetPath
                    };

                    var fogOptions = new FogGenerationOptions
                    {
                        ShouldGenerate = true,
                        GenerationMethod = ParseFogMethod(o.FogMethod),
                        MinTimeBetweenNotes = Math.Max(0.01f, o.FogTime),
                        ChangeFogColorEveryNthBar = Math.Max(1, o.FogBars),
                        RandomizeColors = o.Randomize.Contains("fog")
                    };

                    var beamOptions = new BeamGenerationOptions
                    {
                        ShouldGenerate = true,
                        GenerationMethod = ParseBeamMethod(o.BeamMethod),
                        MinTimeBetweenNotes = Math.Max(0.01f, o.BeamTime),
                        UseCompatibleColors = o.UseCompatibleColors,
                        RandomizeColors = o.Randomize.Contains("beam")
                    };

                    var laserOptions = new LaserGenerationOptions
                    {
                        ShouldGenerate = true,
                        DisableLaserLights = o.EnableLasers != true
                    };

                    if (o.Verbose)
                    {
                        Console.WriteLine("FOG");
                        Console.WriteLine("Source: {0}", o.FogSourceArrangement);
                        Console.WriteLine("Method: {0}", fogOptions.GenerationMethod);
                        if(fogOptions.GenerationMethod == FogGenerationMethod.ChangeEveryNthBar)
                        {
                            Console.WriteLine("Minimum bars: {0}", fogOptions.ChangeFogColorEveryNthBar);
                        }
                        else if (fogOptions.GenerationMethod == FogGenerationMethod.MinTimeBetweenChanges)
                        {
                            Console.WriteLine("Minimum time: {0}", fogOptions.MinTimeBetweenNotes);
                        }
                        Console.WriteLine("Randomized: {0}", fogOptions.RandomizeColors);

                        Console.WriteLine("--------------------------------------------");

                        Console.WriteLine("BEAMS");
                        Console.WriteLine("Source: {0}", beamSourceArrangement);
                        Console.WriteLine("Method: {0}", beamOptions.GenerationMethod);
                        if (beamOptions.GenerationMethod == BeamGenerationMethod.MinTimeBetweenChanges)
                        {
                            Console.WriteLine("Minimum time: {0}", beamOptions.MinTimeBetweenNotes);
                        }
                        Console.WriteLine("Randomized: {0}", beamOptions.RandomizeColors);

                        Console.WriteLine("--------------------------------------------");

                        Console.WriteLine("LASERS");
                        Console.WriteLine("Enabled: {0}", !laserOptions.DisableLaserLights);

                        Console.WriteLine("--------------------------------------------");

                        Console.WriteLine("Output path: {0}", targetPath);
                    }

                    try
                    {
                        var generator = new Generator(o.FogSourceArrangement, beamSourceArrangement, fogOptions, beamOptions, laserOptions);

                        ShowLights.Save(targetPath, generator.Generate());

                        if (o.Verbose)
                        {
                            Console.WriteLine("File generated successfully.");
                        }
                    } catch (Exception e)
                    {
                        Console.WriteLine("ERROR: Generation failed: {0}", e.Message);
                        Console.WriteLine(e.StackTrace);
                        returnCode = 1;
                    }
                });

            return returnCode;
        }
    }
}
