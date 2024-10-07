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
                var query = @"MATCH path = (a:Article {ArticleID: 7402})-[r]->(b:Article)" +
                        "RETURN path";

                //var query = "MATCH path = (n)- [*WSHORTEST(r, n | r.weight)]->(m) " +
                //"FOREACH(i IN CASE WHEN m IS NOT NULL THEN[1] ELSE[] END | " +
                //"FOREACH(x IN[m] | SET x.visited = true)) " +
                //"RETURN DISTINCT path";

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

                            links.Add(new Citation(sourceId, targetId, relationship.Id));

                            //debugging
                            Console.WriteLine($"ArticleID {sourceId} - [{relationship.Id}] -> ArticleID {targetId}");
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
