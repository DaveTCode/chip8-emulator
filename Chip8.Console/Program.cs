using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Chip8.VM;
using CommandLine;

namespace Chip8.Console
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ProgramOptions
    {
        [Option('f', "programPath", Required = true, HelpText = "The path to a chip 8 program to load")]
        public string ProgramPath { get; }

        [Option('t', "programType", Required = false, Default = ProgramType.Chip8, HelpText = "One of Chip8 or ETI660Chip8 depending on what program you are loading. Defaults to Chip8")]
        public ProgramType ProgramType { get; }
        
        [Option('r', "randomSeed", Required = false, HelpText = "An optional random seed to ensure the same randomness on multiple runs")]
        public int? RandomSeed { get; }
        
        [Option('s', "ticksPerSecond", Default = 480, HelpText = "The number of ticks per second, around 480hz is generally considered appropriate but must be exact multiple of 60hz")]
        public int TicksPerSecond { get; }
        
        [Option('p', "pixelSize", Default = 4, HelpText = "The size of the square that we use to represent a single pixel")]
        public int PixelSize { get; }

        [Option("debug", Default = false, HelpText = "Set to enable logging debug information to console")]
        public bool Debug { get; }

        public ProgramOptions(string programPath, ProgramType programType, int? randomSeed, int ticksPerSecond, int pixelSize, bool debug)
        {
            ProgramPath = programPath;
            ProgramType = programType;
            RandomSeed = randomSeed;
            TicksPerSecond = ticksPerSecond;
            Debug = debug;
            PixelSize = pixelSize;
        }
    }

    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments<ProgramOptions>(args)
                .MapResult(
                    RunProgram, 
                    async errs => await Task.FromResult(1));
        }

        private static async Task<int> RunProgram(ProgramOptions options)
        {
            if (options.Debug)
            {
                var consoleTracer = new ConsoleTraceListener();
                Trace.Listeners.Add(consoleTracer);
            }
            
            if (options.TicksPerSecond % 60 != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.TicksPerSecond), options.TicksPerSecond, 
                    "Cycles per second must be multiple of 60");
            }
            var random = options.RandomSeed.HasValue ? new Random(options.RandomSeed.Value) : new Random();
            
            var program = await File.ReadAllBytesAsync(options.ProgramPath);
            var vm = new Computer(random, options.TicksPerSecond);
            vm.LoadProgramAndReset(program, options.ProgramType);
            
            using (var application = new Sdl2Application(vm, options.PixelSize))
            {
                application.ExecuteProgram(options.TicksPerSecond);
            }

            return 0;
        }
    }
}
