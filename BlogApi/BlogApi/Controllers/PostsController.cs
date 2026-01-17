using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogApi.Models;
using System.Linq;

namespace BlogApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PostsController : ControllerBase
    {
        private readonly BlogDbContext _context;
        private const int MaxPageSize = 100;

        public PostsController(BlogDbContext context) => _context = context;

        // GET: /api/posts?pageNumber=1&pageSize=10
        // Returns a paginated list of posts belonging to the authenticated user
        [HttpGet]
        public async Task<IActionResult> GetPosts(int pageNumber = 1, int pageSize = 10)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var baseQuery = _context.Posts
                .AsNoTracking()
                .Where(p => p.UserId == userId.Value);

            var totalCount = await baseQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await baseQuery
                .Include(p => p.Tags)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<Post>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };

            return Ok(result);
        }

        // GET: /api/posts/{id}
        // Returns a single post by id
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetPost(long id)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var post = await _context.Posts
                .Include(p => p.Tags)
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId.Value);

            if (post == null) return NotFound();
            return Ok(post);
        }

        // POST: /api/posts
        // Create a new post for the authenticated user.
        // Accepts tag names; existing tags are reused, new tags are created.
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            if (request == null || string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { message = "Title is required." });

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return Unauthorized();

            var post = new Post
            {
                Title = request.Title,
                Excerpt = request.Excerpt ?? string.Empty,
                Context = request.Context ?? string.Empty,
                PublishedAt = request.PublishedAt,
                PublishStatus = request.PublishStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = userId.Value,
                User = user,
                Author = string.IsNullOrWhiteSpace(user.Name) ? user.Email : user.Name
            };

            if (request.Tags != null && request.Tags.Any())
            {
                foreach (var raw in request.Tags)
                {
                    var name = raw?.Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var existing = await _context.Tags.FirstOrDefaultAsync(t => t.Name == name);
                    if (existing != null) post.Tags.Add(existing);
                    else
                    {
                        var newTag = new Tag { Name = name };
                        _context.Tags.Add(newTag);
                        post.Tags.Add(newTag);
                    }
                }
            }

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
        }

        // Helper: read user id from JWT claims (sub or nameidentifier)
        private long? GetUserIdFromClaims()
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (long.TryParse(sub, out var id)) return id;
            return null;
        }

        // DTOs + Paged result
        public class CreatePostRequest
        {
            public string Title { get; set; } = string.Empty;
            public string? Excerpt { get; set; }
            public string? Context { get; set; }
            public DateTime? PublishedAt { get; set; }
            public bool PublishStatus { get; set; }
            public List<string>? Tags { get; set; }
        }

        public class PagedResult<T>
        {
            public List<T> Items { get; set; } = new();
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
            public int TotalPages { get; set; }
        }
    }
}
