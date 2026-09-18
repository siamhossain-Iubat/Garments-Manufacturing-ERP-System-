using System.Text.Json.Serialization;

namespace GManufacturingERP.Models.Entities
{
    public class SSLCommerzValidationResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("tran_date")]
        public string TranDate { get; set; } = string.Empty;

        [JsonPropertyName("tran_id")]
        public string TranId { get; set; } = string.Empty;

        [JsonPropertyName("val_id")]
        public string ValId { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("bank_tran_id")]
        public string BankTranId { get; set; } = string.Empty;

        [JsonPropertyName("card_type")]
        public string CardType { get; set; } = string.Empty;

        [JsonPropertyName("card_brand")]
        public string CardBrand { get; set; } = string.Empty;

        [JsonPropertyName("card_issuer")]
        public string CardIssuer { get; set; } = string.Empty;
    }
}