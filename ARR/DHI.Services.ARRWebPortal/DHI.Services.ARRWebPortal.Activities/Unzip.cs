using System;
using System.Activities;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DHI.Workflow.Activities.Core;

namespace DHI.Services.ARRWebPortal.Activities
{
    public class Unzip : CoreCodeActivity
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

        /// <summary>
        /// Gets or sets the Status
        /// </summary>
        public OutArgument<string> Status { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            if (Directory.Exists(Output.Get(context)))
            {
                Status.Set(context, "Error: Model Setupe Folder Exists, has it been ran already");
            }
            else
            {
                ProcessModelSetup.UnZipStreamToFile(new MemoryStream(File.ReadAllBytes(Input.Get(context))), Output.Get(context));

                Status.Set(context, "Success: Unzipped");
            }
        }
    }
}
