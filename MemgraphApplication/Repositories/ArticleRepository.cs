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

        public async Task<Graph> FetchGraph()
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

                //var query = @"MATCH path = (n:Article {ArticleID: 16716})<- [*WSHORTEST(r, n | r.weight)]-(m)" +
                //    "RETURN DISTINCT path";

                var query = @"CALL katz_centrality.get() " +
                    "YIELD node, rank " +
                    "WITH node, rank " +
                    "ORDER BY rank DESC " +
                    "LIMIT 1 " +
                    "MATCH path = (node)<-[r *]-(m) " +
                    "RETURN DISTINCT path";


                //var query = "CALL katz_centrality.get() " +
                //    "YIELD node, rank " +
                //    "WITH node, rank " +
                //    "ORDER BY rank DESC " +
                //    "LIMIT 1 " +
                //    "MATCH path = (node)-[*WSHORTEST(r, node | r.weight)]-(m) " +
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
                var query = @"MATCH (a:Article {ArticleID: $articleId})-[r]-(b:Article)
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


        public async Task<int> FetchMostRelevantArticleId()
        {
            var session = _driver.AsyncSession();
            try
            {
                var query = @"CALL katz_centrality.get()" +
                      "YIELD node, rank" +
                      "RETURN node, rank" +
                      "ORDER BY rank DESC" +
                      "LIMIT 1";

                return await session.ExecuteReadAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(query);
                    var articleId = 0;

                    await cursor.ForEachAsync(record =>
                    {
                        var node = record["node"].As<INode>();
                        articleId = node.Properties["ArticleID"].As<int>();
                    });

                    return articleId;
                });
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }

}
