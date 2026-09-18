namespace GManufacturingERP.Models.Entities
{
    public class SSLCommerzSettings
    {
        public string StoreId { get; set; } = string.Empty;

        public string StorePassword { get; set; } = string.Empty;

        public bool IsSandbox { get; set; } = true;

        public string BaseUrl { get; set; } =
            "https://sandbox.sslcommerz.com";

        public string? ReturnBaseUrl { get; set; }
    }
}