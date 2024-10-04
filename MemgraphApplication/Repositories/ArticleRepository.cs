using Neo4j.Driver;
using MemgraphApplication.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

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
                return await session.ExecuteReadAsync(async transaction =>
                {
                    //var cursor = await transaction.RunAsync(@"MATCH (a:Article)-[r]->(b:Article)" +
                    //    "RETURN a.ArticleID AS source, a.PMID AS sourcePMID, b.ArticleID AS target, b.PMID AS targetPMID" +
                    //    "LIMIT $limit",
                    //    new { limit });

                    var cursor = await transaction.RunAsync(@"MATCH (a:Article {ArticleID: 7402})-[r]->(b:Article {ArticleID: 16908})" +
                        "RETURN a.ArticleID AS source, b.ArticleID AS target");


                    //var cursor = await transaction.RunAsync(@"MATCH path = (n)-[r]->(m) " +
                    //    "WITH DISTINCT path, n, m" +
                    //    "FOREACH(i IN CASE WHEN m IS NOT NULL THEN[1] ELSE[] END | " +
                    //    "FOREACH(x IN[m] | SET x.visited = true)) " +
                    //    "RETURN DISTINCT path" +
                    //    "LIMIT 100");

                    //var cursor = await transaction.RunAsync(@"MATCH (a:Article {ArticleID: 7402})-[r]->(b:Article {ArticleID: 16908})" +
                    //    "RETURN a, r, b;");


                    var nodes = new List<Article>();
                    var links = new List<Citation>();

                    var records = await cursor.ToListAsync();

                    foreach (var record in records)
                    {
                        var sourceArticle = new Article(record["source"].As<int>());
                        var originArticleIndex = nodes.Count;
                        nodes.Add(sourceArticle);

                        var targetArticle = new Article(record["target"].As<int>());
                        var destArticleIndex = nodes.IndexOf(targetArticle);
                        destArticleIndex = destArticleIndex == -1 ? nodes.Count : destArticleIndex;
                        nodes.Add(targetArticle);


                        links.Add(new Citation(sourceArticle.ArticleID, targetArticle.ArticleID));

                        //test stuff
                        Console.WriteLine(sourceArticle.ArticleID + "  - [{relationship}] -> " + targetArticle.ArticleID);

                    }
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
