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
        public string Title { get; }
        public string Label { get; }

        public Article(string title, string label)
        {
            Title = title;
            Label = label;
        }

        public override bool Equals(object obj)
        {
            return obj is Article node &&
                   Title == node.Title &&
                   Label == node.Label;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Title, Label);
        }
    }

    public class Citation
    {
        public string Source { get; }
        public string Target { get; }

        public Citation(string source, string target)
        {
            Source = source;
            Target = target;
        }
    }
}
