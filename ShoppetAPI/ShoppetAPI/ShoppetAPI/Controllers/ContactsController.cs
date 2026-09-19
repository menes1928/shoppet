using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;

namespace ShoppetAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ContactsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetContacts()
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                var contacts = new List<object>();

                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT Id, UserId, Name, Role, Address, Phone, IsEmergency FROM emergencycontacts";

                    using (var cmd = new MySqlCommand(query, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            contacts.Add(new
                            {
                                Id = reader.GetInt32("Id"),
                                UserId = reader.GetInt32("UserId"),
                                Name = reader.GetString("Name"),
                                Role = reader.GetString("Role"),
                                Address = reader.GetString("Address"),
                                Phone = reader.GetString("Phone"),
                                IsEmergency = reader.GetBoolean("IsEmergency")
                            });
                        }
                    }
                }
                return Ok(contacts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching contacts: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateContact([FromBody] ContactRequest request)
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                long newId = 0;

                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    var query = @"INSERT INTO emergencycontacts (UserId, Name, Role, Address, Phone, IsEmergency) 
                                  VALUES (1, @Name, @Role, @Address, @Phone, @IsEmergency);
                                  SELECT LAST_INSERT_ID();";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Name", request.Name ?? "");
                        cmd.Parameters.AddWithValue("@Role", request.Role ?? "");
                        cmd.Parameters.AddWithValue("@Address", request.Address ?? "");
                        cmd.Parameters.AddWithValue("@Phone", request.Phone ?? "");
                        cmd.Parameters.AddWithValue("@IsEmergency", request.IsEmergency);

                        object result = await cmd.ExecuteScalarAsync();
                        if (result != null) newId = Convert.ToInt64(result);
                    }
                }

                return Ok(new { Id = (int)newId, request.Name, request.Role, request.Phone });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error saving contact: {ex.Message}");
            }
        }
    }

    public class ContactRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsEmergency { get; set; }
    }
}