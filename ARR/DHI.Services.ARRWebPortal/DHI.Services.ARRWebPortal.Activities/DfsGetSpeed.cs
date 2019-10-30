using System;
using System.Activities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DHI.Services.ARRWebPortal;
using DHI.Workflow.Activities.Core;

namespace DHI.Workflow.Activities.Dfs
{
    /// <summary>
    ///     Extraction of statistics from a dfs file
    /// </summary>
    public class DfsGetSpeed : CoreCodeActivity
    {        
        /// <summary>
        ///     The path of Dfs file to input
        /// </summary>
        [RequiredArgument]
        public InArgument<string> InputFilePath { get; set; }

        /// <summary>
        ///     Item name for U vector
        /// </summary>
        [RequiredArgument]
        public InArgument<string> UItemName { get; set; }

        /// <summary>
        ///     Item name for V vector
        /// </summary>
        [RequiredArgument]
        public InArgument<string> VItemName { get; set; }

        /// <summary>
        ///     Item name for V vector
        /// </summary>
        [RequiredArgument]
        public InArgument<string> DepthItemName { get; set; }

        /// <summary>
        ///     The path of Dfs file to input
        /// </summary>
        [RequiredArgument]
        public InArgument<string> OutputFilePath { get; set; }

        /// <summary>
        ///     The path of Dfs file for stat8c output
        /// </summary>
        [RequiredArgument]
        public InArgument<string> StaticOutputFilePath { get; set; }

        /// <summary>
        ///     For newly added items, replace the null values with 0
        /// </summary>
        [RequiredArgument]
        public InArgument<bool> ZeroDelete { get; set; }

        /// <inheritdoc />
        protected override void Execute(CodeActivityContext context)
        {            
            //float deleteValue = MeshProcess.GetDeleteValue(InputFilePath.Get(context));
            //List<float[]> velocityList = MeshProcess.GetSpeed(InputFilePath.Get(context), UItemName.Get(context), VItemName.Get(context));
            //List<float[]> depthList = MeshProcess.GetDepth(InputFilePath.Get(context), DepthItemName.Get(context));
            //List<float[]> vxdList = MeshProcess.Multiply(velocityList, depthList, deleteValue);
            //float[] maxVxd = MeshProcess.GetMax(vxdList, deleteValue);
            //float[] maxV = MeshProcess.GetMax(velocityList, deleteValue);
            //float[] maxD = MeshProcess.GetMax(depthList, deleteValue);
            //MeshProcess.AddItem(InputFilePath.Get(context), OutputFilePath.Get(context), velocityList, vxdList, ZeroDelete.Get(context));
            //MeshProcess.AddStaticAsDynamic(InputFilePath.Get(context), StaticOutputFilePath.Get(context), maxVxd, maxV, maxD, ZeroDelete.Get(context));
        }
    }
}