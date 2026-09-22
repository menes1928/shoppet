using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;

namespace ShoppetAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PetsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PetsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetPets([FromQuery] int? userId)
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                var pets = new List<object>();

                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT Id, UserId, Name, Species, Breed, AgeYears, Weight, PhotoUrl, CreatedAt FROM pets";
                    if (userId.HasValue)
                    {
                        query += " WHERE UserId = @UserId";
                    }

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        if (userId.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@UserId", userId.Value);
                        }
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                pets.Add(new
                                {
                                    Id = reader.GetInt32("Id"),
                                    UserId = reader.GetInt32("UserId"),
                                    Name = reader.GetString("Name"),
                                    Species = reader.GetString("Species"),
                                    Breed = reader.GetString("Breed"),
                                    AgeYears = reader.GetInt32("AgeYears"),
                                    Weight = reader.IsDBNull(reader.GetOrdinal("Weight")) ? "" : reader.GetString("Weight"),
                                    PhotoUrl = reader.IsDBNull(reader.GetOrdinal("PhotoUrl")) ? "" : reader.GetString("PhotoUrl"),
                                    CreatedAt = reader.GetDateTime("CreatedAt")
                                });
                            }
                        }
                    }
                }

                return Ok(pets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddPet([FromBody] PetRequest request)
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var query = "INSERT INTO pets (UserId, Name, Species, Breed, AgeYears, Weight, PhotoUrl, CreatedAt) VALUES (@UserId, @Name, @Species, @Breed, @AgeYears, @Weight, @PhotoUrl, NOW())";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", request.UserId);
                        cmd.Parameters.AddWithValue("@Name", request.Name);
                        cmd.Parameters.AddWithValue("@Species", request.Species);
                        cmd.Parameters.AddWithValue("@Breed", request.Breed);
                        cmd.Parameters.AddWithValue("@AgeYears", request.AgeYears);
                        cmd.Parameters.AddWithValue("@Weight", string.IsNullOrEmpty(request.Weight) ? DBNull.Value : request.Weight);
                        cmd.Parameters.AddWithValue("@PhotoUrl", string.IsNullOrEmpty(request.PhotoUrl) ? DBNull.Value : request.PhotoUrl);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Ok(new { message = "Pet added successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePet(int id, [FromBody] PetRequest request)
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var query = "UPDATE pets SET Name = @Name, Species = @Species, Breed = @Breed, AgeYears = @AgeYears, Weight = @Weight, PhotoUrl = @PhotoUrl WHERE Id = @Id";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Name", request.Name);
                        cmd.Parameters.AddWithValue("@Species", request.Species);
                        cmd.Parameters.AddWithValue("@Breed", request.Breed);
                        cmd.Parameters.AddWithValue("@AgeYears", request.AgeYears);
                        cmd.Parameters.AddWithValue("@Weight", string.IsNullOrEmpty(request.Weight) ? DBNull.Value : request.Weight);
                        cmd.Parameters.AddWithValue("@PhotoUrl", string.IsNullOrEmpty(request.PhotoUrl) ? DBNull.Value : request.PhotoUrl);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows == 0) return NotFound("Pet not found.");
                    }
                }
                return Ok(new { message = "Pet updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePet(int id)
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var query = "DELETE FROM pets WHERE Id = @Id";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        if (affectedRows == 0) return NotFound("Pet not found.");
                    }
                }
                return Ok(new { message = "Pet deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    public class PetRequest
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Species { get; set; }
        public string Breed { get; set; }
        public int AgeYears { get; set; }
        public string Weight { get; set; }
        public string PhotoUrl { get; set; }
    }
}
