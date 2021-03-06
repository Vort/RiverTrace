﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;

namespace RiverTrace
{
    class Program
    {
        static bool ProcessArgs(string[] args, ref bool go)
        {
            try
            {
                if (args.Length == 2)
                {
                    if (args[0].Contains(",") && args[1].Contains(","))
                    {
                        if (!double.TryParse(args[0].Split(',')[0], out Config.Data.lon1))
                            return false;
                        if (!double.TryParse(args[0].Split(',')[1], out Config.Data.lat1))
                            return false;
                        if (!double.TryParse(args[1].Split(',')[0], out Config.Data.lon2))
                            return false;
                        if (!double.TryParse(args[1].Split(',')[1], out Config.Data.lat2))
                            return false;
                        go = true;
                        return true;
                    }
                }

                for (int i = 0; i < args.Length; )
                {
                    string cmd = args[i++];
                    if (cmd == "lat1")
                        Config.Data.lat1 = double.Parse(args[i++]);
                    else if (cmd == "lon1")
                        Config.Data.lon1 = double.Parse(args[i++]);
                    else if (cmd == "lat2")
                        Config.Data.lat2 = double.Parse(args[i++]);
                    else if (cmd == "lon2")
                        Config.Data.lon2 = double.Parse(args[i++]);
                    else if (cmd == "go")
                        go = true;
                    else
                        return false;
                }
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
            return true;
        }

        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(exeDir);

            if (args.Length == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  RiverTrace [lat1 <lat1> lon1 <lon1>] [lat2 <lat2> lon2 <lon2>] [go]");
                Console.WriteLine();
                Console.WriteLine("Example:");
                Console.WriteLine("  RiverTrace go");
                Console.WriteLine("    (or)");
                Console.WriteLine("  RiverTrace lat1 64.9035637 lon1 52.2209239");
                Console.WriteLine("  RiverTrace lat2 64.9032122 lon2 52.2213061");
                Console.WriteLine("    (or)");
                Console.WriteLine("  RiverTrace lat1 64.9035637 lon1 52.2209239 lat2 64.9032122 lon2 52.2213061 go");
            }
            else
            {
                bool go = false;
                if (ProcessArgs(args, ref go))
                {
                    Config.Write();
                    if (go)
                    {
                        if (!Debugger.IsAttached)
                        {
                            try
                            {
                                new Tracer();
                            }
                            catch (Exception e)
                            {
                                Console.Error.WriteLine(e);
                            }
                        }
                        else
                            new Tracer();
                    }
                    else
                        Console.WriteLine("<osm version='0.6'></osm>");
                }
                else
                    Console.Error.WriteLine("Incorrect parameters");
            }
        }
    }
}
