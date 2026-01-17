namespace BlogApi.Models
{
    public class Role
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty; // Mapped from 'role' in Java
        public List<User> Users { get; set; } = new();
    }
}
