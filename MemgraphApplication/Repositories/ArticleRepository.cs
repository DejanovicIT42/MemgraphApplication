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
            _driver = driver;
        }

        public async Task<Graph> FetchGraph(int limit)
        {
            var session = _driver.AsyncSession(WithDatabase);
            try
            {
                return await session.ReadTransactionAsync(async transaction =>
                {
                    var cursor = await transaction.RunAsync(@"MATCH (origin:Airport)-[f:IS_FLYING_TO]->(dest:Airport) " +
                        "WITH origin, dest ORDER BY origin.name, dest.name " +
                        "RETURN origin.NAME AS origin_air, dest.NAME AS dest_air", new { limit });
                    var nodes = new List<Article>();
                    var links = new List<Citation>();
                    var records = await cursor.ToListAsync();
                    foreach (var record in records)
                    {
                        var orgAirport = new Article(title: record["origin_air"].As<string>(), label: "airport");
                        var originAirportIndex = nodes.Count;
                        nodes.Add(orgAirport);

                        var destAirport = new Article(record["dest_air"].As<string>(), "airport");
                        var destAirportIndex = nodes.IndexOf(destAirport);
                        destAirportIndex = destAirportIndex == -1 ? nodes.Count : destAirportIndex;
                        nodes.Add(destAirport);
                        links.Add(new Citation(destAirport.Title, orgAirport.Title));

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
            return System.Environment.GetEnvironmentVariable("DATABASE") ?? "flights";
        }

    }

}
