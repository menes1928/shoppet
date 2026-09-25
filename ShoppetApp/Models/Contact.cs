using SQLite;

namespace ShoppetApp.Models;

public class Contact
{
    [PrimaryKey]
    public int Id { get; set; }

    [Indexed]
    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public bool IsEmergency { get; set; }
}
