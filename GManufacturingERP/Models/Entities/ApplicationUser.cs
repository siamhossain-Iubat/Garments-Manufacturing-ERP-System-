using Microsoft.AspNetCore.Identity;

namespace GManufacturingERP.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        public string? Country { get; set; }

        public string? CompanyAddress { get; set; }
        public string? UserType { get; set; }

        public string AccountStatus { get; set; } = "PendingApproval";
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

       
    }
}