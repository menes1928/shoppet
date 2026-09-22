using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;

namespace ShoppetAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommunityController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CommunityController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetPosts([FromQuery] int userId = 0)
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                var posts = new List<object>();

                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    
                    var query = @"
                        SELECT p.Id, p.UserId, p.PetId, p.AuthorName, p.PetName, p.Content, p.ImageUrl, p.Timestamp, p.IsEdited,
                               u.FullName AS UserFullName,
                               (SELECT COUNT(*) FROM communitylikes cl WHERE cl.PostId = p.Id) AS LikesCount,
                               (SELECT COUNT(*) FROM communitycomments cc WHERE cc.PostId = p.Id) AS CommentsCount,
                               EXISTS(SELECT 1 FROM communitylikes cl WHERE cl.PostId = p.Id AND cl.UserId = @UserId) AS IsLikedByMe
                        FROM communityposts p
                        LEFT JOIN users u ON p.UserId = u.Id
                        ORDER BY p.Timestamp DESC;";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                posts.Add(new
                                {
                                    Id = reader.GetInt32("Id"),
                                    UserId = reader.GetInt32("UserId"),
                                    PetId = reader.IsDBNull(reader.GetOrdinal("PetId")) ? (int?)null : reader.GetInt32("PetId"),
                                    AuthorName = reader.IsDBNull(reader.GetOrdinal("UserFullName")) ? reader.GetString("AuthorName") : reader.GetString("UserFullName"),
                                    PetName = reader.IsDBNull(reader.GetOrdinal("PetName")) ? "" : reader.GetString("PetName"),
                                    Content = reader.GetString("Content"),
                                    ImageUrls = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? "" : reader.GetString("ImageUrl"),
                                    Timestamp = reader.GetDateTime("Timestamp"),
                                    IsEdited = reader.GetBoolean("IsEdited"),
                                    LikesCount = reader.GetInt32("LikesCount"),
                                    CommentsCount = reader.GetInt32("CommentsCount"),
                                    IsLikedByMe = reader.GetBoolean("IsLikedByMe")
                                });
                            }
                        }
                    }
                }

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error fetching community posts: " + ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var query = @"INSERT INTO communityposts (UserId, PetId, AuthorName, PetName, Content, ImageUrl, Timestamp, IsEdited) 
                                  VALUES (@UserId, @PetId, @AuthorName, @PetName, @Content, @ImageUrl, NOW(), 0);";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", request.UserId);
                        cmd.Parameters.AddWithValue("@PetId", request.PetId.HasValue ? request.PetId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@AuthorName", request.AuthorName ?? "");
                        cmd.Parameters.AddWithValue("@PetName", request.PetName ?? "");
                        cmd.Parameters.AddWithValue("@Content", request.Content ?? "");
                        cmd.Parameters.AddWithValue("@ImageUrl", string.IsNullOrEmpty(request.ImageUrls) ? DBNull.Value : request.ImageUrls);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error creating post: " + ex.Message);
            }
        }

        [HttpPost("{id}/like")]
        public async Task<IActionResult> ToggleLike(int id, [FromBody] ToggleLikeRequest request)
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    
                    var checkQuery = "SELECT Id FROM communitylikes WHERE PostId = @PostId AND UserId = @UserId";
                    bool exists = false;
                    using (var cmd = new MySqlCommand(checkQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@PostId", id);
                        cmd.Parameters.AddWithValue("@UserId", request.UserId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            exists = reader.HasRows;
                        }
                    }

                    if (exists)
                    {
                        var deleteQuery = "DELETE FROM communitylikes WHERE PostId = @PostId AND UserId = @UserId";
                        using (var cmd = new MySqlCommand(deleteQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@PostId", id);
                            cmd.Parameters.AddWithValue("@UserId", request.UserId);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        return Ok(new { IsLiked = false });
                    }
                    else
                    {
                        var insertQuery = "INSERT INTO communitylikes (PostId, UserId, CreatedAt) VALUES (@PostId, @UserId, NOW())";
                        using (var cmd = new MySqlCommand(insertQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@PostId", id);
                            cmd.Parameters.AddWithValue("@UserId", request.UserId);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        return Ok(new { IsLiked = true });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error toggling like: " + ex.Message);
            }
        }

        [HttpGet("{id}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                var comments = new List<object>();

                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    
                    var query = @"
                        SELECT c.Id, c.PostId, c.UserId, c.ParentCommentId, c.Content, c.CreatedAt,
                               u.FullName AS UserFullName
                        FROM communitycomments c
                        LEFT JOIN users u ON c.UserId = u.Id
                        WHERE c.PostId = @PostId
                        ORDER BY c.CreatedAt ASC;";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@PostId", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                comments.Add(new
                                {
                                    Id = reader.GetInt32("Id"),
                                    PostId = reader.GetInt32("PostId"),
                                    UserId = reader.GetInt32("UserId"),
                                    ParentCommentId = reader.IsDBNull(reader.GetOrdinal("ParentCommentId")) ? (int?)null : reader.GetInt32("ParentCommentId"),
                                    AuthorName = reader.IsDBNull(reader.GetOrdinal("UserFullName")) ? "Unknown" : reader.GetString("UserFullName"),
                                    Content = reader.GetString("Content"),
                                    CreatedAt = reader.GetDateTime("CreatedAt")
                                });
                            }
                        }
                    }
                }

                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error fetching comments: " + ex.Message);
            }
        }

        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentRequest request)
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var query = @"INSERT INTO communitycomments (PostId, UserId, ParentCommentId, Content, CreatedAt) 
                                  VALUES (@PostId, @UserId, @ParentCommentId, @Content, NOW());";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@PostId", id);
                        cmd.Parameters.AddWithValue("@UserId", request.UserId);
                        cmd.Parameters.AddWithValue("@ParentCommentId", request.ParentCommentId.HasValue ? request.ParentCommentId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Content", request.Content ?? "");

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error adding comment: " + ex.Message);
            }
        }
    }

    public class CreatePostRequest
    {
        public int UserId { get; set; }
        public int? PetId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string PetName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrls { get; set; }
    }

    public class ToggleLikeRequest
    {
        public int UserId { get; set; }
    }

    public class AddCommentRequest
    {
        public int UserId { get; set; }
        public int? ParentCommentId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
