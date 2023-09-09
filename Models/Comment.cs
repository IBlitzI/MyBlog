namespace MyBlog.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string AuthorName { get; set; }

        public int BlogId { get; set; }
        public Blog Blog { get; set; }
    }
}
