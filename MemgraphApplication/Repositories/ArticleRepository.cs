using Neo4j.Driver;
using MemgraphApplication.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Transactions;
using System.Text.Json;

namespace MemgraphApplication.Repositories
{
    
    public class ArticleRepository : IArticleRepository
    {
        private readonly IDriver _driver;

        public ArticleRepository(IDriver driver)
        {
            _driver = driver;
            using var session = _driver.Session();
        }

        public async Task<Graph> FetchGraph(int limit)
        {

            var session = _driver.AsyncSession();
            try
            {
                //var query = @"MATCH path = (a:Article {ArticleID: 7402})-[r]->(b:Article {ArticleID: 16908})" +
                //        "RETURN path";

                //var query = @"MATCH path = (a:Article {ArticleID: 4})-[r]-(b:Article)" +
                //        "RETURN path";

                //var query = @"MATCH path = (a:Article {ArticleID: 13974})-[r]-(b:Article)" +
                //        "RETURN path";

                var query = @"MATCH path = (n:Article {ArticleID: 16716})<- [*WSHORTEST(r, n | r.weight)]-(m)" +
                    "RETURN DISTINCT path";


                //var query = "MATCH path = (n)- [*WSHORTEST(r, n | r.weight)]->(m) " +
                //    "FOREACH(i IN CASE WHEN m IS NOT NULL THEN[1] ELSE[] END | " +
                //    "FOREACH(x IN[m] | SET x.visited = true)) " +
                //    "RETURN DISTINCT path";

                return await session.ExecuteReadAsync(async transaction =>
                {

                    var cursor = await transaction.RunAsync(query);

                    var nodes = new List<Article>();
                    var links = new List<Citation>();

                    await cursor.ForEachAsync(record =>
                    {
                        var path = record["path"].As<IPath>();

                        foreach (var node in path.Nodes)
                        {
                            int nodeId = node.Properties["ArticleID"].As<int>();
                            if (!nodes.Any(n => n.ArticleID == nodeId))
                            {
                                nodes.Add(new Article(nodeId));
                            }
                        }

                        foreach (var relationship in path.Relationships)
                        {
                            int sourceId = path.Nodes.First(n => n.Id == relationship.StartNodeId).Properties["ArticleID"].As<int>();
                            int targetId = path.Nodes.First(n => n.Id == relationship.EndNodeId).Properties["ArticleID"].As<int>();

                            if (!links.Any(l => l.Source == sourceId && l.Target == targetId))
                            {
                                links.Add(new Citation(sourceId, targetId, relationship.Id));
                            }
                        }
                    });

                    return new Graph(nodes, links);

                });
            }
            finally
            {
                await session.CloseAsync();
            }

        }


        public async Task<Graph> FetchNodeRelationships(int articleId)
        {
            var session = _driver.AsyncSession();
            try
            {
                var query = @"MATCH (a:Article {ArticleID: $articleId})-[r]->(b:Article)
                      RETURN a, r, b";

                return await session.ExecuteReadAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(query, new { articleId });
                    var nodes = new List<Article>();
                    var links = new List<Citation>();

                    await cursor.ForEachAsync(record =>
                    {
                        var startNode = record["a"].As<INode>();
                        var endNode = record["b"].As<INode>();
                        var relationship = record["r"].As<IRelationship>();

                        int sourceId = startNode.Properties["ArticleID"].As<int>();
                        int targetId = endNode.Properties["ArticleID"].As<int>();

                        if (!nodes.Any(n => n.ArticleID == sourceId))
                            nodes.Add(new Article(sourceId));
                        if (!nodes.Any(n => n.ArticleID == targetId))
                            nodes.Add(new Article(targetId));

                        if (relationship.StartNodeId == startNode.Id)
                        {
                            links.Add(new Citation(sourceId, targetId, relationship.Id));
                        }
                        else
                        {
                            links.Add(new Citation(targetId, sourceId, relationship.Id));
                        }
                    });

                    return new Graph(nodes, links);
                });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        //bruh side
        //public async Task<Graph> FetchMostReferencedNode()
        //{
        //    using (var session = _driver.Session())
        //    {
        //        var result = session.Run(
        //            "MATCH path = (n:Article {ArticleID: 16716})<- [*WSHORTEST(r, n | r.weight)]-(m) RETURN DISTINCT path");

        //        var nodes = new HashSet<object>();
        //        var relationships = new List<object>();

        //        foreach (var record in result)
        //        {
        //            var path = record["path"].As<IPath>();

        //            // Collect Nodes
        //            foreach (var node in path.Nodes)
        //            {
        //                nodes.Add(new
        //                {
        //                    id = node.Id,
        //                    labels = node.Labels,
        //                    properties = node.Properties
        //                });
        //            }

        //            // Collect Relationships
        //            foreach (var relationship in path.Relationships)
        //            {
        //                relationships.Add(new
        //                {
        //                    id = relationship.Id,
        //                    startNodeId = relationship.StartNodeId,
        //                    endNodeId = relationship.EndNodeId,
        //                    type = relationship.Type,
        //                    properties = relationship.Properties
        //                });
        //            }
        //        }

        //        // Serialize to JSON (using System.Text.Json)
        //        var graphData = new
        //        {
        //            nodes = nodes.ToArray(),
        //            relationships = relationships.ToArray()
        //        };

        //        return JsonSerializer.Serialize(graphData);
        //    }
        //}



    }

}
