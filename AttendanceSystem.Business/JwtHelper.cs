// AttendanceSystem.Business/JwtHelper.cs
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AttendanceSystem.Business
{
    public static class JwtHelper
    {
        // These will be set from Program.cs
        public static string SecretKey { get; set; } = "MySuperSecretKey123456789danishisagoodboy";
        public static string Issuer { get; set; } = "http://localhost:49403/";
        public static string Audience { get; set; } = "http://localhost:49403/";
        public static int ExpireMinutes { get; set; } = 60;

        public static string GenerateToken(string email, string role)
        {
            var header = new { alg = "HS256", typ = "JWT" };
            var payload = new
            {
                sub = email,
                role = role,
                iss = Issuer,
                aud = Audience,
                iat = ToUnixTime(DateTime.UtcNow),
                exp = ToUnixTime(DateTime.UtcNow.AddMinutes(ExpireMinutes))
            };

            var headerJson = JsonSerializer.Serialize(header);
            var payloadJson = JsonSerializer.Serialize(payload);

            var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

            var signature = SignData($"{headerBase64}.{payloadBase64}");
            var signatureBase64 = Base64UrlEncode(signature);

            return $"{headerBase64}.{payloadBase64}.{signatureBase64}";
        }
        public static (string email, string role)? ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            var parts = token.Split('.');
            if (parts.Length != 3) return null;

            var header = parts[0];
            var payload = parts[1];
            var signature = parts[2];

            // Verify signature
            var originalSignature = Base64UrlDecode(signature);
            var expectedSignature = SignData($"{header}.{payload}");
            if (!CryptographicOperations.FixedTimeEquals(originalSignature, expectedSignature))
                return null;

            // Parse payload
            try
            {
                var payloadBytes = Base64UrlDecode(payload);
                var payloadJson = Encoding.UTF8.GetString(payloadBytes);
                using var doc = JsonDocument.Parse(payloadJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("exp", out var exp) && exp.GetInt64() < ToUnixTime(DateTime.UtcNow))
                    return null; // Expired

                var email = root.GetProperty("sub").GetString();
                var role = root.GetProperty("role").GetString();

                return (email!, role!);
            }
            catch
            {
                return null;
            }
        }

        private static byte[] SignData(string data)
        {
            var key = Encoding.UTF8.GetBytes(SecretKey);
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        private static string Base64UrlEncode(byte[] input)
        {
            var output = Convert.ToBase64String(input)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
            return output;
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var output = input
                .Replace('-', '+')
                .Replace('_', '/');
            switch (output.Length % 4)
            {
                case 2: output += "=="; break;
                case 3: output += "="; break;
            }
            return Convert.FromBase64String(output);
        }

        private static long ToUnixTime(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
        }
    }
}