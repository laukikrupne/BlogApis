using Azure;

namespace BlogApi.Models
{
    public class Post
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public DateTime? PublishedAt { get; set; }
        public bool PublishStatus { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string Author { get; set; } = string.Empty;

        // Relationships
        public List<Comment> Comments { get; set; } = new();
        public List<Tag> Tags { get; set; } = new();
        public long UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
