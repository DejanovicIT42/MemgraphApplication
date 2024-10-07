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
                var query = @"MATCH path = (a:Article {ArticleID: 7402})-[r]->(b:Article {ArticleID: 16908})" +
                        "RETURN path";

                return await session.ExecuteReadAsync(async transaction =>
                {
                    //var cursor = await transaction.RunAsync(@"MATCH (a:Article)-[r]->(b:Article)" +
                    //    "RETURN a.ArticleID AS source, a.PMID AS sourcePMID, b.ArticleID AS target, b.PMID AS targetPMID" +
                    //    "LIMIT $limit",
                    //    new { limit });

                    //var cursor = await transaction.RunAsync(@"MATCH (a:Article {ArticleID: 7402})-[r]->(b:Article {ArticleID: 16908})" +
                    //    "RETURN a.ArticleID AS source, r, b.ArticleID AS target");


                    //var cursor = await transaction.RunAsync(@"MATCH (a:Article {ArticleID: 7402})-[r]->(b:Article {ArticleID: 16908})" +
                    //    "RETURN a, r, b;");

                    var cursor = await transaction.RunAsync(query);

                    var nodes = new List<Article>();
                    var links = new List<Citation>();

                    //var records = await query.ToListAsync();

                    await cursor.ForEachAsync(record =>
                    {
                        var path = record["path"].As<IPath>();

                        // Add the start node (source)
                        var startNode = path.Start.As<INode>();
                        int sourceId = startNode.Properties["ArticleID"].As<int>();
                        if (!nodes.Any(n => n.ArticleID == sourceId))
                        {
                            nodes.Add(new Article(sourceId));
                        }

                        // Iterate through relationships and nodes in the path
                        for (int i = 0; i < path.Relationships.Count; i++)
                        {
                            var relationship = path.Relationships[i];
                            var currentNode = path.Nodes[i + 1].As<INode>(); // Next node in the path

                            int targetId = currentNode.Properties["ArticleID"].As<int>();
                            if (!nodes.Any(n => n.ArticleID == targetId))
                            {
                                nodes.Add(new Article(targetId));
                            }

                            // Create a Citation from source to target
                            links.Add(new Citation(sourceId, targetId, relationship.Id));

                            // Print the relationship for debugging
                            Console.WriteLine($"ArticleID {sourceId} - [{relationship.Id}] -> ArticleID {targetId}");
                        }
                    });

                    //foreach (var record in records)
                    //{
                    //    var sourceArticle = new Article(record["source"].As<int>());
                    //    var originArticleIndex = nodes.Count;
                    //    nodes.Add(sourceArticle);

                    //    var targetArticle = new Article(record["target"].As<int>());
                    //    var destArticleIndex = nodes.IndexOf(targetArticle);
                    //    destArticleIndex = destArticleIndex == -1 ? nodes.Count : destArticleIndex;
                    //    nodes.Add(targetArticle);


                    //    links.Add(new Citation(sourceArticle.ArticleID, targetArticle.ArticleID));

                    //    //test stuff
                    //    Console.WriteLine(sourceArticle.ArticleID + "  - [{relationship}] -> " + targetArticle.ArticleID);

                    //}
                    return new Graph(nodes, links);

                });
            }
            finally
            {
                await session.CloseAsync();
            }

        }

        private void WithDatabase(SessionConfigBuilder sessionConfigBuilder)
        {
            sessionConfigBuilder.WithDatabase(Database());

        }

        private string Database()
        {
            return System.Environment.GetEnvironmentVariable("DATABASE") ?? "articles";

        }

    }

}
