using System;
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
                        new Tracer();
                    else
                        Console.WriteLine("<osm version='0.6'></osm>");
                }
                else
                    Console.Error.WriteLine("Incorrect parameters");
            }
        }
    }
}
