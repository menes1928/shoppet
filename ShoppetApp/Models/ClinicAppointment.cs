namespace ShoppetApp.Models;

public class ClinicAppointment
{
    public int Id { get; set; }
    public int ClinicId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string PetName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = "Pending";
}