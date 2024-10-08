using MemgraphApplication.Repositories;
using MemgraphApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;

namespace MemgraphApplication.Controllers
{
    [ApiController]
    [Route("/")]
    public class GraphController : Controller
    {

        private readonly IArticleRepository _articleRepository;
        public GraphController(IArticleRepository articleRepository)
        {
            _articleRepository = articleRepository;
        }


        //[Route("/graph")]
        //[HttpGet]
        //public async Task<Graph> FetchGraph([FromQuery(Name = "limit")] int limit)
        //{
        //    return await _articleRepository.FetchGraph(limit <= 0 ? 50 : limit);
        //}

        [Route("/graph")]
        [HttpGet]
        public async Task<IActionResult> FetchGraph([FromQuery(Name = "limit")] int limit)
        {
            var graph = await _articleRepository.FetchGraph(limit <= 0 ? 50 : limit);
            return Json(graph);
        }

        [Route("/graph/expand")]
        [HttpGet]
        public async Task<IActionResult> ExpandNode([FromQuery(Name = "nodeId")] int nodeId)
        {
            var graph = await _articleRepository.FetchNodeRelationships(nodeId);
            return Json(graph);
        }
    }
}
