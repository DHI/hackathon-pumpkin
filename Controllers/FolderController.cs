using System;
using System.Collections.Generic;
using System.Web;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.IO;

namespace DHI.SeaStatus.Deploy.WebAPI.Controllers
{
    [Route("api/folders")]
    [ApiController]
    [Authorize(Policy = "EditorsOnly")]
    [SwaggerTag("Actions for managing folders and folder structure.")]
    public class FolderController : ControllerBase
    {
        // GET: api/Folder
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // POST: api/Folder
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT: api/Folder/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete]
        [Route("deletescenariofolder")]
        public void DeleteScenarioFolder([FromBody] FolderInfo body)
        {
            string folder = Directory.GetCurrentDirectory() + (@"\..\Data\Scenarios\") + body.ScenarioId;
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
    }

    public class FolderInfo
    {
        public string ScenarioId { get; set; }
        public string File { get; set; }
    }
}
