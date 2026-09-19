using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;

namespace ShoppetAPI.Controllers
{
    [Route("api/pets/{petId}/food")]
    [ApiController]
    public class FoodLogsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public FoodLogsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetFoodLogs(int petId)
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                var logs = new List<object>();

                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT Id, PetId, FoodName, AmountGrams, IntervalHours, IntervalMinutes, StartTimestamp, LastFedTimestamp, FedDate, Notes, CreatedAt FROM foodlogs WHERE PetId = @PetId";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@PetId", petId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                logs.Add(new
                                {
                                    Id = reader.GetInt32("Id"),
                                    PetId = reader.GetInt32("PetId"),
                                    FoodName = reader.GetString("FoodName"),
                                    AmountGrams = reader.GetDouble("AmountGrams"),
                                    IntervalHours = reader.GetInt32("IntervalHours"),
                                    IntervalMinutes = reader.GetInt32("IntervalMinutes"),
                                    StartTimestamp = reader.GetString("StartTimestamp"),
                                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? "" : reader.GetString("Notes")
                                });
                            }
                        }
                    }
                }
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching food logs: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateFoodLog(int petId, [FromBody] FoodLogRequest request)
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                long newId = 0;

                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var query = @"INSERT INTO foodlogs (PetId, FoodName, AmountGrams, IntervalHours, IntervalMinutes, StartTimestamp, LastFedTimestamp, FedDate, Notes, CreatedAt) 
                                  VALUES (@PetId, @FoodName, @AmountGrams, @IntervalHours, @IntervalMinutes, @StartTimestamp, '', '', @Notes, @CreatedAt);
                                  SELECT LAST_INSERT_ID();";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@PetId", petId);
                        cmd.Parameters.AddWithValue("@FoodName", request.FoodName ?? "");
                        cmd.Parameters.AddWithValue("@AmountGrams", request.AmountGrams);
                        cmd.Parameters.AddWithValue("@IntervalHours", request.IntervalHours);
                        cmd.Parameters.AddWithValue("@IntervalMinutes", request.IntervalMinutes);
                        cmd.Parameters.AddWithValue("@StartTimestamp", request.StartTimestamp ?? "");
                        cmd.Parameters.AddWithValue("@Notes", request.Notes ?? "");
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                        object result = await cmd.ExecuteScalarAsync();
                        if (result != null) newId = Convert.ToInt64(result);
                    }
                }

                return Ok(new { Id = (int)newId, PetId = petId, request.FoodName, request.AmountGrams });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error saving food log: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFoodLog(int petId, int id)
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var cmd = new MySqlCommand("DELETE FROM foodlogs WHERE Id = @Id AND PetId = @PetId", connection);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@PetId", petId);
                    await cmd.ExecuteNonQueryAsync();
                }
                return Ok(new { message = "Deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting food log: {ex.Message}");
            }
        }
    }

    public class FoodLogRequest
    {
        public string FoodName { get; set; } = string.Empty;
        public double AmountGrams { get; set; }
        public int IntervalHours { get; set; }
        public int IntervalMinutes { get; set; }
        public string StartTimestamp { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}