using SQLite;

namespace ShoppetApp.Models;

public class Product
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    [Ignore]
    public List<string> Categories { get; set; } = [];

    [Ignore]
    public List<string> Species { get; set; } = [];

    // Fixed: Added setter so DatabaseService can assign Category directly
    private string _category = "General";
    public string Category
    {
        get => _category;
        set { _category = value; if (!Categories.Contains(value)) Categories.Add(value); }
    }

    public int StockQuantity { get; set; } = 0;
    public bool IsAvailable { get; set; } = true;

    [Ignore]
    public int Stock => StockQuantity;
}