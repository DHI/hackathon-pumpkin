using System;
using System.Activities;
using System.IO;
using DHI.Services.ARRWebPortal;
using DHI.Workflow.Activities.Core;
using Newtonsoft.Json.Linq;

namespace DHI.Workflow.Activities.ARRWebPortal
{
    public class ResultFileStatistics : CoreCodeActivity
    {
        public enum StatisticsEnum
        {
            None,
            Max,
            Min,
            Average
        }

        /// <summary>
        ///     The path of Dfs file to provide statistics for
        /// </summary>
        [RequiredArgument]
        public InArgument<string> InputFileName { get; set; }

        /// <summary>
        ///     The statistics type
        /// </summary>
        public StatisticsEnum Statistics { get; set; } = StatisticsEnum.None;

        /// <summary>
        ///     The resulting spatial statistics
        /// </summary>
        public OutArgument<string> StatisticsResult { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            try
            {

                //StatisticsResult.Set(context, NetworkProcess.GetResultStatictics(File.ReadAllBytes(InputFileName.Get(context))));
                StatisticsResult.Set(context, NetworkProcess.GetPointGeoJson(File.ReadAllBytes(InputFileName.Get(context))).ToString());
            }
            catch (Exception exception)
            {
                StatisticsResult.Set(context, "Exception: " + exception.Message);
            }
        }
    }
}