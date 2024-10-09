using MemgraphApplication.Models;

namespace MemgraphApplication.Repositories
{
    public interface IArticleRepository
    {
        Task<Graph> FetchGraph(int limit);
        Task<int> FetchMostRelevantArticleId();
        Task<Graph> FetchNodeRelationships(int nodeId);

    }
}
