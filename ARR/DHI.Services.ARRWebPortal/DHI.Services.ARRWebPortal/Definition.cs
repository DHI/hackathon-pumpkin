using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    public static class Definition
    {
        public static string FolderPrefix = @"C:\ModelSetup"; //@"P:\ModelSetup";
        public static string Folder = System.IO.Path.Combine(FolderPrefix, "temp");
        public static string PlotPath = @"C:\inetpub\wwwroot\AU\ARR\plot.csv"; // @"P:\ARR\plot.csv";
        public static string ConnectionString = "Server=172.16.127.5;Port=5432;Database=arrweb;Uid=postgres;Pwd=Solutions!;";

        // public static string Url = "http://data.arr-software.org/";
        public static string Url = "https://data-legacy.arr-software.org/";
        public static string TemporalPattern = "TemporalPatterns";
        public static string ArealTemporalPattern = "ArealTemporalPatterns";
        public static string TemporalLookup = "Temporal Patterns";
        public static string ArealTemporalLookup = "Areal Temporal Patterns";
        public static string RainfallResultsLookup = "Rainfall Results";

        public static string InitialLoss = "Storm Initial Losses (mm)";
        public static string ContinueLoss = "Storm Continuing Losses (mm/h)";
        public static List<string> arf = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h", "i" };

        public static int LongShortCutoffMinutes1 = 720;
        public static int LongShortCutoffMinutes2 = 1440;
        public static int LongShortCutoffMinutes3 = 10080;
        public static int Area1 = 1;
        public static int Area2 = 10;
        public static int Area3 = 1000;
        public static int Area4 = 30000;

        public static string UrbsGrossRainString = "Gross Rain";
        public static string UrbsFlowRatesString = "Flow Rates";
        public static string UrbsRiverLevelsString = "River Levels";

        public static string ArfTextFile = "arftext";
        
        public static List<KeyValuePair<string, string>> Probabilities = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("63.2%,1.00", "frequent"),
            new KeyValuePair<string, string>("50.00%,1.44", "frequent"),
            new KeyValuePair<string, string>("39.35%,2.00", "frequent"),
            new KeyValuePair<string, string>("20.00%,4.48", "frequent"),
            new KeyValuePair<string, string>("18.13%,5.00", "frequent"),
            new KeyValuePair<string, string>("10.00%,10.00", "intermediate"),
            new KeyValuePair<string, string>("5.00%,20.0","intermediate"),
            new KeyValuePair<string, string>("2.00%,49.5", "rare"),
            new KeyValuePair<string, string>("1.00%,100", "rare"),
            new KeyValuePair<string, string>("0.50%,200", "rare"),
            new KeyValuePair<string, string>("0.20%,500", "rare"),
            new KeyValuePair<string, string>("0.10%,1000", "rare"),
            new KeyValuePair<string, string>("0.05%,2000", "rare")
        };

        public static string GetProbability(double userAEP)
        {
            //double userAEP = GetAEP(userARI);

            if (userAEP > 0.144)
            {
                return "frequent";
            }
            if (userAEP < 0.032)
            {
                return "rare";
            }
            return "intermediate";
        }

        public static double GetAEP(double ari)
        {
            return 1 + (-1 * System.Math.Exp((-1 / ari)));
        }

        public enum ProbabilityType
        {
            veryFrequent,
            frequentInfrequent,
            rare
        }

        public static List<KeyValuePair<string, int>> Durations = new List<KeyValuePair<string, int>>
        {
            new KeyValuePair<string, int>("10 min", 10),
            new KeyValuePair<string, int>("15 min", 15),
            new KeyValuePair<string, int>("30 min", 30),
            new KeyValuePair<string, int>("1 hour", 60),
            new KeyValuePair<string, int>("2 hour", 120),
            new KeyValuePair<string, int>("3 hour", 180),
            new KeyValuePair<string, int>("6 hour", 360),
            new KeyValuePair<string, int>("12 hour", 720),
            new KeyValuePair<string, int>("24 hour", 1440),
            new KeyValuePair<string, int>("48 hour", 2880),
            new KeyValuePair<string, int>("72 hour", 4320),
            new KeyValuePair<string, int>("96 hour", 5760),
            new KeyValuePair<string, int>("120 hour", 7200),
            new KeyValuePair<string, int>("144 hour", 8640),
            new KeyValuePair<string, int>("168 hour", 10080),
        };

        public enum ResultType
        {
            //rafts is loc or tot
            loc,
            tot,
            rafts,
            urbs,
            rorb
        }

        public enum ProbabilityCategoryType
        {
            point,
            area,
            both
        }

        public enum TailType
        {
            no,
            two
        }

        public enum ModelType
        {
            m21,
            m21fm,
            m21fst,
            sim11,
            mhydro,
            couple
        }

        public enum IndexFileType
        {
            dfsu,
            mesh,
            dfs2
        }

        public static List<KeyValuePair<ModelType, string>> ModelEngines = new List<KeyValuePair<ModelType, string>>
        {
            new KeyValuePair<ModelType, string>(ModelType.m21, "mnmodel"),
            new KeyValuePair<ModelType, string>(ModelType.m21fm, "FemEngineHD"),
            new KeyValuePair<ModelType, string>(ModelType.couple, "FemEngineMF"),
            new KeyValuePair<ModelType, string>(ModelType.sim11, "mike11"),
            new KeyValuePair<ModelType, string>(ModelType.mhydro, "DHI.Mike1D.Application"),
            new KeyValuePair<ModelType, string>(ModelType.m21fst, "mnmodel")
        };

        public static double GetDesignatedArea(double area)
        {
            if (area >= 75 && area < 140)
            {
                return 100;
            }
            if (area >= 140 && area < 300)
            {
                return 200;
            }
            if (area >= 300 && area < 700)
            {
                return 500;
            }
            if (area >= 700 && area < 1600)
            {
                return 1000;
            }
            if (area >= 1600 && area < 3500)
            {
                return 2500;
            }
            if (area >= 3500 && area < 7000)
            {
                return 5000;
            }
            if (area >= 7000 && area < 14000)
            {
                return 10000;
            }
            if (area >= 14000 && area < 28000)
            {
                return 20000;
            }
            if (area >= 28000)
            {
                return 40000;
            }
            throw new Exception("value not in range for Getting Designated Area: " + area);
        }
    }
}
