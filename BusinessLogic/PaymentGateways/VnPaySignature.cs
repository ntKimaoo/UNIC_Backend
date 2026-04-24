using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace BusinessLogic.PaymentGateways;

public static class VnPaySignature

{
    public static string BuildSignedPaymentUrl(string paymentBaseUrl, SortedDictionary<string, string> vnpParameters, string hashSecret)
    {

        var signData = BuildSignData(vnpParameters);
        var secureHash = HmacSha512Hex(hashSecret, signData);
        var query = BuildQuery(vnpParameters);
        var sep = paymentBaseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{paymentBaseUrl.TrimEnd('/')}{sep}{query}&vnp_SecureHash={secureHash}";
    }

    public static bool VerifyIpn(
        IReadOnlyDictionary<string, string> query,
        string hashSecret,
        out string? error)
    {
        error = null;
        if (!query.TryGetValue("vnp_SecureHash", out var received) || string.IsNullOrWhiteSpace(received))
        {
            error = "missing_secure_hash";
            return false;
        }

        var signParams = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in query)
        {
            if (!kv.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.Equals(kv.Key, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kv.Key, "vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrEmpty(kv.Value))
                continue;

            signParams[kv.Key] = kv.Value;
        }

        var signData = BuildSignData(signParams);
        var expected = HmacSha512Hex(hashSecret, signData);
        if (!string.Equals(expected, received.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            error = "checksum_mismatch";
            return false;
        }
        return true;
    }

    private static string BuildSignData(SortedDictionary<string, string> vnpParameters)
    {
        var parts = new List<string>();
        foreach (var kv in vnpParameters)
        {
            if (string.IsNullOrEmpty(kv.Value))
                continue;
            parts.Add($"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}");
        }

        return string.Join("&", parts);
    }

    private static string BuildQuery(SortedDictionary<string, string> vnpParameters)
    {
        var parts = new List<string>();
        foreach (var kv in vnpParameters)
        {
            if (string.IsNullOrEmpty(kv.Value))
                continue;
            parts.Add($"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}");
        }
        return string.Join("&", parts);
    }

    private static string HmacSha512Hex(string key, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string VietnamCreateDateUtcPlus7()
    {
        var utc = DateTime.UtcNow;
        var vn = utc.AddHours(7);
        return vn.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
    }
}

