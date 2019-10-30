using System;
using System.Activities;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DHI.Workflow.Activities.Core;
using Newtonsoft.Json.Linq;

namespace DHI.Workflow.Activities.ARRWebPortal
{
    public class Json2String : CoreCodeActivity
    {
        /// <summary>
        /// Gets or sets the Input
        /// </summary>
        [RequiredArgument]
        public InArgument<string> FilePath { get; set; }

        /// <summary>
        /// Gets or sets the Property
        /// </summary>
        [RequiredArgument]
        public InArgument<string> Property { get; set; }

        /// <summary>
        /// Gets or sets the Value
        /// </summary>
        public OutArgument<string> Value { get; set; }

        /// <summary>
        /// Gets or sets the ValueList
        /// </summary>
        public OutArgument<string[]> ValueArray { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            try
            {
                JObject jObj = JObject.Parse(File.ReadAllText(FilePath.Get(context)));

                string value = jObj[Property.Get(context)].ToString();
                Value.Set(context, value);

                ValueArray.Set(context, value.Split(new char[] { ',' }));
            }
            catch (Exception exception)
            {
                Value.Set(context, "Exception: " + exception.Message);
            }
        }
    }
}