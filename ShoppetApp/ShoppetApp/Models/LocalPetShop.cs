namespace ShoppetApp.Models
{
    public class LocalPetShop
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string OperatingHours { get; set; } = string.Empty;
    }
}