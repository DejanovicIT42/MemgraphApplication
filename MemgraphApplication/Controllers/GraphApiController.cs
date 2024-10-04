using Microsoft.AspNetCore.Mvc;

namespace MemgraphApplication.Controllers
{
   
    public class GraphApiController : Controller
    {
        [HttpGet]
        public IActionResult GetGraphData()
        {
            var nodes = new[]
            {
                new { id = 1, label = "Orb" },
                new { id = 2, label = "Graph" },
                new { id = 3, label = "Canvas" }
            };

            var edges = new[]
            {
                new { id = 1, start = 1, end = 2, label = "DRAWS" },
                new { id = 2, start = 2, end = 3, label = "ON" }
            };

            return Ok(new { nodes, edges });
        }
    }
}
