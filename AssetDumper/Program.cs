using System;
using CommandLine;
using Serilog;

namespace AssetDumper
{
    internal static class Program
    {
        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <param name="args"></param>
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();

            try
            {
                return Parser.Default.ParseArguments(args, typeof(ExportBundleCommand))
                    .MapResult((BaseCommand cmd) => cmd.Execute(), _ => 1);
            }
            catch (Exception e)
            {
                Log.Error(e, "An unhandled error occurred in the application.");
                return 1;
            }
        }
    }
}