using System.Net.Http;

namespace DuckDDNSSync.Core
{
    public static class DuckDnsClient
    {
        private static readonly HttpClient _http = new();

        public static async Task<(bool Success, string Message)> UpdateAsync(string domains, string token)
        {
            try
            {
                var url = $"https://www.duckdns.org/update?domains={Uri.EscapeDataString(domains)}&token={Uri.EscapeDataString(token)}&ip=";
                var response = (await _http.GetStringAsync(url)).Trim();
                var ok = response.Equals("OK", StringComparison.OrdinalIgnoreCase);
                return (ok, ok ? "OK" : $"Duck DNS respondió: {response}");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
    }
}
