namespace GManufacturingERP.ViewModels
{
    public class UserManagementViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? UserType { get; set; }

        public string AccountStatus { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Role { get; set; } = string.Empty;
    }
}