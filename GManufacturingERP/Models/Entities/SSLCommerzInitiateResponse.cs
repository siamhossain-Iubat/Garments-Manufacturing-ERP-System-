using System.Text.Json.Serialization;

namespace GManufacturingERP.Models.Entities
{
    public class SSLCommerzInitiateResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("sessionkey")]
        public string SessionKey { get; set; } = string.Empty;

        [JsonPropertyName("GatewayPageURL")]
        public string GatewayPageURL { get; set; } = string.Empty;

        [JsonPropertyName("failedreason")]
        public string FailedReason { get; set; } = string.Empty;
    }
}