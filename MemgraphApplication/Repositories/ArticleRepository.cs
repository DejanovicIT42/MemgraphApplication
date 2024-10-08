using Neo4j.Driver;
using MemgraphApplication.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Transactions;

namespace MemgraphApplication.Repositories
{
    
    public class ArticleRepository : IArticleRepository
    {
        private readonly IDriver _driver;

        public ArticleRepository(IDriver driver)
        {
            //_driver = driver;
            _driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.None);
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

                var query = @"MATCH path = (a:Article {ArticleID: 1})-[r]-(b:Article)" +
                        "RETURN path";

                //var query = @"MATCH path = (a:Article {ArticleID: 13974})-[r]-(b:Article)" +
                //        "RETURN path";


                //hardcoding the solution
                //var query = @"MATCH path = (a:Article {ArticleID: 16716})<-[r]-(b:Article)" +
                //        "RETURN path";



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

                        var startNode = path.Start.As<INode>();
                        int sourceId = startNode.Properties["ArticleID"].As<int>();


                        if (!nodes.Any(n => n.ArticleID == sourceId))
                        {
                            nodes.Add(new Article(sourceId));
                        }

                        for (int i = 0; i < path.Relationships.Count; i++)
                        {
                            var relationship = path.Relationships[i];
                            var currentNode = path.Nodes[i + 1].As<INode>();

                            int targetId = currentNode.Properties["ArticleID"].As<int>();
                            if (!nodes.Any(n => n.ArticleID == targetId))
                            {
                                nodes.Add(new Article(targetId));
                            }

                            if (relationship.StartNodeId == startNode.Id)
                            {
                                // Outgoing relationship
                                links.Add(new Citation(sourceId, targetId, relationship.Id));
                            }
                            else
                            {
                                // Incoming relationship
                                links.Add(new Citation(targetId, sourceId, relationship.Id));
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

    }

}
