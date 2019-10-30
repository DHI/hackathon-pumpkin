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
    public class RandomizeModelSetup : CoreCodeActivity
    {
        /// <summary>
        /// Gets or sets the Input
        /// </summary>
        [RequiredArgument]
        public InArgument<string> Input { get; set; }

        /// <summary>
        /// Gets or sets the Input
        /// </summary>
        [RequiredArgument]
        public InArgument<string> ModelSetup { get; set; }

        /// <summary>
        /// Gets or sets the Output
        /// </summary>
        [RequiredArgument]
        public InArgument<string> Output { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            JObject jObj = JObject.Parse(File.ReadAllText(Input.Get(context)));

            Dictionary<string, string> dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(jObj.ToString());
            dictionary.Remove("model");
            dictionary.Remove("result");

            Stream zipStream = ProcessModelSetup.RandomizeModelSetup(dictionary, new MemoryStream(File.ReadAllBytes(ModelSetup.Get(context))));
            string output = Output.Get(context);

            GridProcess.Stream2File(zipStream, output);
        }
    }
}
