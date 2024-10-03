namespace MemgraphApplication.Models
{
    public class Graph
    {

        public IEnumerable<Article> Nodes { get; }
        public IEnumerable<Citation> Links { get; }

        public Graph(IEnumerable<Article> nodes, IEnumerable<Citation> links)
        {
            Nodes = nodes;
            Links = links;
        }
    }

    public class Article
    {

        public int ArticleID { get; set; }
        public int PMID { get; set; }
        public string Title { get; }
        public string Label { get; }

        public Article(int articleID, int pmid)
        {
            ArticleID = articleID;
            PMID = pmid;
        }

        //public override bool Equals(object obj)
        //{
        //    return obj is Article node &&
        //           ArticleID == node.ArticleID &&
        //           PMID == node.pmid;
        //}

        public override int GetHashCode()
        {
            return HashCode.Combine(ArticleID, PMID);
        }
    }

    public class Citation
    {
        public int Source { get; }
        public int Target { get; }

        public Citation(int source, int target)
        {
            Source = source;
            Target = target;
        }
    }
}
