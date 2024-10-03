namespace MemgraphApplication.Models
{
    public class PathRepresentation
    {

        public int StartArticleID { get; set; }
        public int EndArticleID { get; set; }
        public List<int> IntermediateArticleIDs { get; set; }
    }
}
