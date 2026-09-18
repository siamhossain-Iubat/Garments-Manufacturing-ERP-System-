using System.Text.Json;
using GManufacturingERP.Models.Entities;

namespace GManufacturingERP.Services
{
    public class SSLCommerzService
    {
        private readonly HttpClient _httpClient;
        private readonly SSLCommerzSettings _settings;

        public SSLCommerzService(
            HttpClient httpClient,
            SSLCommerzSettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
        }

        public async Task<SSLCommerzInitiateResponse> InitiatePaymentAsync(
            Dictionary<string, string> parameters)
        {
            var endpoint =
                $"{_settings.BaseUrl}/gwprocess/v4/api.php";

            using var content =
                new FormUrlEncodedContent(parameters);

            using var response =
                await _httpClient.PostAsync(endpoint, content);

            var json =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"SSLCOMMERZ connection failed. HTTP: {response.StatusCode}");
            }

            var result =
                JsonSerializer.Deserialize<SSLCommerzInitiateResponse>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (result == null)
            {
                throw new Exception(
                    "Invalid response received from SSLCOMMERZ.");
            }

            return result;
        }

        public async Task<SSLCommerzValidationResponse?> ValidatePaymentAsync(
            string validationId)
        {
            if (string.IsNullOrWhiteSpace(validationId))
            {
                return null;
            }

            var endpoint =
                $"{_settings.BaseUrl}/validator/api/validationserverAPI.php" +
                $"?val_id={Uri.EscapeDataString(validationId)}" +
                $"&store_id={Uri.EscapeDataString(_settings.StoreId)}" +
                $"&store_passwd={Uri.EscapeDataString(_settings.StorePassword)}" +
                $"&format=json";

            using var response =
                await _httpClient.GetAsync(endpoint);

            var json =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return JsonSerializer.Deserialize<SSLCommerzValidationResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        public string GenerateTransactionId()
        {
            return $"ERP{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        }
    }
}