namespace hashtag.wordcloud.twitter.Models
{
    public class TagItem
    {
        public string Name { get; set; }
        public int Weight { get; set; }
        public Tweet Tweet { get; set; }
        public bool IsShown { get; set; }
    }
}