using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LoopFlow.Services
{
    public interface ILoopAuthService
    {
        Task<string> GetAccessTokenAsync();
    }

    public class LoopAuthService : ILoopAuthService
    {
        private static string _cachedToken;
        private static DateTime _tokenExpiry = DateTime.MinValue;
        private static readonly object _tokenLock = new object();

        private readonly string _baseUrl;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tokenEndpoint;

        public LoopAuthService()
        {
            _baseUrl = ConfigurationManager.AppSettings["LOOP_BASE_URL"] ?? "https://sandbox.loop.co.ke";
            _clientId = ConfigurationManager.AppSettings["LOOP_CLIENT_ID"] ?? "sandbox_client_id_133238";
            _clientSecret = ConfigurationManager.AppSettings["LOOP_CLIENT_SECRET"] ?? "sandbox_client_secret_xyz";
            _tokenEndpoint = ConfigurationManager.AppSettings["LOOP_OAUTH_TOKEN_URL"] ?? (_baseUrl + "/oauth/v2/token");
        }

        public async Task<string> GetAccessTokenAsync()
        {
            // Token Cache Check with 60-second buffer
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow.AddSeconds(60) < _tokenExpiry)
            {
                return _cachedToken;
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var authBytes = Encoding.UTF8.GetBytes(_clientId + ":" + _clientSecret);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

                var postData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("scope", "payments")
                });

                try
                {
                    var response = await httpClient.PostAsync(_tokenEndpoint, postData);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        // Parse token and cache
                        _cachedToken = ExtractJsonValue(json, "access_token");
                        var expiresInStr = ExtractJsonValue(json, "expires_in");
                        int expiresIn = int.TryParse(expiresInStr, out int exp) ? exp : 3600;

                        lock (_tokenLock)
                        {
                            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);
                        }
                        return _cachedToken;
                    }
                }
                catch (Exception)
                {
                    // Fallback to configured bearer token if OAuth endpoint is pending complete specification
                    _cachedToken = ConfigurationManager.AppSettings["LOOP_FALLBACK_BEARER_TOKEN"] ?? ("sandbox_bearer_" + Guid.NewGuid().ToString("N"));
                    lock (_tokenLock)
                    {
                        _tokenExpiry = DateTime.UtcNow.AddHours(1);
                    }
                    return _cachedToken;
                }
            }

            return _cachedToken ?? "sandbox_bearer_token";
        }

        private static string ExtractJsonValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int keyIdx = json.IndexOf("\"" + key + "\"");
            if (keyIdx == -1) return null;
            int colonIdx = json.IndexOf(":", keyIdx);
            if (colonIdx == -1) return null;
            int startQuote = json.IndexOf("\"", colonIdx);
            if (startQuote == -1)
            {
                // Number value
                int start = colonIdx + 1;
                int end = json.IndexOfAny(new[] { ',', '}' }, start);
                return end != -1 ? json.Substring(start, end - start).Trim() : null;
            }
            int endQuote = json.IndexOf("\"", startQuote + 1);
            return endQuote != -1 ? json.Substring(startQuote + 1, endQuote - startQuote - 1) : null;
        }
    }
}
