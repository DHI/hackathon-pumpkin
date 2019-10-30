using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Workflow.Activities.Core;
using System.Activities;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace DHI.Services.ARRWebPortal.Activities
{
    public class Dfs2GeoJson : CoreCodeActivity
    {
        /// <summary>
        /// Gets or sets the Input
        /// </summary>
        [RequiredArgument]
        public InArgument<string> Input { get; set; }

        /// <summary>
        /// Gets or sets the Output
        /// </summary>
        [RequiredArgument]
        public InArgument<string> Output { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            
            string filePath = Input.Get(context);
            byte[] input = File.ReadAllBytes(filePath);
            JObject jObject = Path.GetExtension(filePath) == "." + Definition.IndexFileType.dfs2.ToString() ? GridProcess.GetGeoJSON(input) : MeshProcess.GetGeoJSON(input);
            string output = Output.Get(context);
            File.WriteAllText(output, jObject.ToString());
        }
    }
}
