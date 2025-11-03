using System;

namespace Patient_Information_System_CS.Models
{
    public sealed class AdminProvisioningRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string GivenName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? Suffix { get; set; }
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;
        public string RelationshipToEmergencyContact { get; set; } = string.Empty;
        public string Sex { get; set; } = "Unspecified";
        public string Nationality { get; set; } = "Unknown";
        public DateTime BirthDate { get; set; } = DateTime.Today.AddYears(-30);
    }
}
