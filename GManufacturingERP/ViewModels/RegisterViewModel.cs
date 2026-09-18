using System.ComponentModel.DataAnnotations;

namespace GManufacturingERP.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(150)]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact person name is required.")]
        [StringLength(100)]
        [Display(Name = "Contact Person")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        [Display(Name = "Business Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required.")]
        [Display(Name = "Country")]
        public string Country { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(300)]
        [Display(Name = "Company Address")]
        public string CompanyAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password",
            ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true",
            ErrorMessage = "You must accept the terms and conditions.")]
        [Display(Name = "I agree to the terms and conditions")]
        public bool AcceptTerms { get; set; }
    }
}