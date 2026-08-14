using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace LoopFlow.Services
{
    public class LoopSignatureResult
    {
        public string Timestamp { get; set; }
        public string Nonce { get; set; }
        public string Signature { get; set; }
    }

    public interface ILoopSignatureService
    {
        LoopSignatureResult GenerateSignature(string merchantTill);
        string ComputeHmacSha256(string secret, string canonicalString);
    }

    public class LoopSignatureService : ILoopSignatureService
    {
        private readonly string _signingSecret;

        public LoopSignatureService()
        {
            _signingSecret = ConfigurationManager.AppSettings["LOOP_SIGNING_SECRET"] ?? "sandbox_signing_secret_133238";
        }

        public LoopSignatureResult GenerateSignature(string merchantTill)
        {
            if (string.IsNullOrEmpty(merchantTill))
            {
                merchantTill = ConfigurationManager.AppSettings["LOOP_MERCHANT_TILL"] ?? "133238";
            }

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            string nonce = Guid.NewGuid().ToString("D").ToLower(); // Fresh lowercase UUID v4

            // Canonical string: merchantTill|timestamp|nonce
            string canonicalString = merchantTill + "|" + timestamp + "|" + nonce;
            string signature = ComputeHmacSha256(_signingSecret, canonicalString);

            return new LoopSignatureResult
            {
                Timestamp = timestamp,
                Nonce = nonce,
                Signature = signature
            };
        }

        public string ComputeHmacSha256(string secret, string canonicalString)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(secret ?? "");
            byte[] messageBytes = Encoding.UTF8.GetBytes(canonicalString ?? "");

            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(messageBytes);
                var sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2")); // Lowercase hexadecimal
                }
                return sb.ToString();
            }
        }
    }
}
