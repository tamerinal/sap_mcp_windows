using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace SapMcp.Core;

public sealed class AdtPackageUnavailableException() : InvalidOperationException(UiText.Get("Adt.PackageUpgrade"));

public sealed record TrustedCertificate(string File, string Sha256);
public sealed record CertificatePreview(string Pem, string Subject, string Issuer, DateTime NotBefore, DateTime NotAfter, string San, string Sha256)
{
    public string Display => $"{Subject}\n{Issuer}\nUTC: {NotBefore:u} — {NotAfter:u}\nSAN: {San}\nSHA-256: {Sha256}";
}

public static class AdtProfiles
{
    private static readonly HashSet<string> Codes = new("OK NOT_RUN ADT_URL_REQUIRED ADT_URL_INVALID ADT_BASE_URL_IS_SERVICE_PATH ADT_PROFILE_INVALID ADT_TIMEOUT_INVALID TLS_CA_FILE_INVALID TLS_CA_FILE_UNREADABLE DNS_FAILED TCP_REFUSED NETWORK_TIMEOUT TLS_NAME_MISMATCH TLS_EXPIRED TLS_NOT_YET_VALID TLS_UNTRUSTED TLS_CHAIN_UNRESOLVED TLS_FAILED CONNECTION_FAILED AUTHENTICATION_FAILED AUTHORIZATION_DENIED AUTHENTICATION_NOT_PROVEN ADT_RESPONSE_TOO_LARGE ADT_REDIRECT_REQUIRES_URL ADT_CROSS_ORIGIN_BLOCKED ADT_DISCOVERY_HTTP_ERROR ADT_DISCOVERY_INVALID ADT_UNEXPECTED_CONTENT ADT_READ_ONLY_HTTP_ERROR ADT_READ_ONLY_INVALID ADT_SERVICE_HTTP_ERROR HTTP_NO_TLS PROXY_INVALID PROXY_UNSUPPORTED PROXY_AUTH_FAILED PROXY_CONNECT_FAILED CREDENTIAL_UNREADABLE".Split(' '), StringComparer.Ordinal);
    public static string NormalizeUrl(string value)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0 || value.Contains('\\'))
            throw new InvalidDataException(UiText.Get("Adt.InvalidUrl"));
        var path = uri.AbsolutePath.TrimEnd('/');
        var at = path.IndexOf("/sap/bc/adt", StringComparison.OrdinalIgnoreCase);
        if (at >= 0)
        {
            if (!path[at..].Equals("/sap/bc/adt", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException(UiText.Get("Adt.ServicePath"));
            path = path[..at];
        }
        if (path.Contains("%2f", StringComparison.OrdinalIgnoreCase) || path.Contains("%5c", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException(UiText.Get("Adt.InvalidUrl"));
        return uri.GetLeftPart(UriPartial.Authority) + path;
    }

    public static async Task<IReadOnlyList<CertificatePreview>> ReadCertificatesAsync(string file, CancellationToken ct = default)
    {
        if (new FileInfo(file).Length > 1024 * 1024) throw new InvalidDataException(UiText.Get("Adt.InvalidCertificate"));
        var bytes = await System.IO.File.ReadAllBytesAsync(file, ct); var text = Encoding.UTF8.GetString(bytes);
        if (text.Contains("PRIVATE KEY", StringComparison.Ordinal) || Path.GetExtension(file).ToLowerInvariant() is ".pfx" or ".p12") throw new InvalidDataException(UiText.Get("Adt.InvalidCertificate"));
        var certs = new X509Certificate2Collection();
        try
        {
            if (text.Contains("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal)) certs.ImportFromPem(text);
            else certs.Add(X509CertificateLoader.LoadCertificate(bytes));
            if (certs.Count is 0 or > 20 || certs.Any(c => c.HasPrivateKey)) throw new CryptographicException();
            return certs.Select(c => new CertificatePreview(c.ExportCertificatePem(), c.Subject, c.Issuer,
                c.NotBefore.ToUniversalTime(), c.NotAfter.ToUniversalTime(), c.Extensions.OfType<X509SubjectAlternativeNameExtension>().FirstOrDefault()?.Format(false) ?? "",
                Convert.ToHexStringLower(SHA256.HashData(c.RawData)))).ToArray();
        }
        catch (CryptographicException) { throw new InvalidDataException(UiText.Get("Adt.InvalidCertificate")); }
        finally { foreach (var c in certs) c.Dispose(); }
    }

    public static async Task<IReadOnlyList<TrustedCertificate>> StoreCertificatesAsync(AppSettings settings, ConnectionProfile profile, IReadOnlyList<CertificatePreview> certificates, CancellationToken ct = default)
    {
        var key = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(profile.Id)));
        var folder = Path.Combine(settings.DataDirectory, "certificates", key);
        var result = new List<TrustedCertificate>();
        foreach (var c in certificates)
        {
            // Reparse the public material; a caller-supplied fingerprint never chooses a path.
            using var cert = X509Certificate2.CreateFromPem(c.Pem);
            var hash = Convert.ToHexStringLower(SHA256.HashData(cert.RawData));
            var file = Path.Combine(folder, hash + ".pem");
            await AtomicFile.WriteAsync(file, cert.ExportCertificatePem(), ct, privateData: true);
            result.Add(new(file, hash));
        }
        return result;
    }

    public static string SupportReport(ConnectionProfile profile, JsonElement diagnosis)
    {
        // Allowlist, not string replacement: never copy endpoints, account values or arbitrary upstream text.
        var stages = new List<object>();
        if (diagnosis.TryGetProperty("stages", out var rows) && rows.ValueKind == JsonValueKind.Array)
            foreach (var row in rows.EnumerateArray().Take(20))
            {
                string Safe(string key)
                {
                    if (!row.TryGetProperty(key, out var v) || v.ValueKind != JsonValueKind.String || v.GetString() is not { } s) return "REDACTED";
                    return (key == "code" ? Codes.Contains(s) : key == "stage" ? new[] { "configuration", "dns", "tcp", "tls", "authentication", "discovery", "readOnly" }.Contains(s) : new[] { "passed", "failed", "skipped", "pending", "unverified" }.Contains(s)) ? s : "REDACTED";
                }
                stages.Add(new { stage = Safe("stage"), status = Safe("status"), code = Safe("code") });
            }
        return JsonSerializer.Serialize(new { schemaVersion = 1, component = "adt", configuration = new { endpoint = "[redacted]", account = "[redacted]", profile = "[redacted]", certificateCount = profile.AdtCertificates.Count, tlsVerification = !profile.AllowInsecureTls, timeoutSeconds = profile.AdtTimeoutSeconds }, stages }, JsonDefaults.Options);
    }

    public static async Task<(AppSettings Settings, string Directory)> CreateTestSnapshotAsync(AppSettings source, string profileId, CancellationToken ct = default)
    {
        var next = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(source, JsonDefaults.Options), JsonDefaults.Options)!;
        var profile = next.Profiles.SingleOrDefault(p => p.Id == profileId) ?? throw new InvalidDataException(UiText.Get("Adt.SaveFirst"));
        foreach (var p in next.Profiles) p.SecretRef = ProfileService.ResolveSecret(source.CredentialsFile, p.SecretRef);
        var directory = Path.Combine(source.DataDirectory, "cache", "adt-tests", Guid.NewGuid().ToString("N"));
        next.CredentialsFile = Path.Combine(directory, "sap_credentials.json");
        next.ActiveProfiles[profile.TargetKey] = profile.Id;
        await ProfileService.MaterializeAsync(next, ct);
        return (next, directory);
    }

    public static string Advice(string code) => UiText.Get(code switch
    {
        "ADT_URL_REQUIRED" or "ADT_URL_INVALID" or "ADT_BASE_URL_IS_SERVICE_PATH" => "Adt.UrlAdvice",
        "TLS_NAME_MISMATCH" => "Adt.NameAdvice",
        "TLS_EXPIRED" or "TLS_NOT_YET_VALID" => "Adt.DateAdvice",
        "TLS_UNTRUSTED" or "TLS_CHAIN_UNRESOLVED" or "TLS_CA_FILE_INVALID" or "TLS_CA_FILE_UNREADABLE" => "Adt.CaAdvice",
        "AUTHENTICATION_FAILED" => "Adt.AuthAdvice",
        "AUTHENTICATION_NOT_PROVEN" => "Adt.IdentityAdvice",
        "AUTHORIZATION_DENIED" => "Adt.AuthorizationAdvice",
        "DNS_FAILED" or "TCP_REFUSED" or "NETWORK_TIMEOUT" => "Adt.NetworkAdvice",
        _ => "Adt.ServiceAdvice"
    });
}
