using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using SapMcp.Core;
using Xunit;

namespace SapMcp.Tests;

public sealed class AdtTests
{
    [Theory]
    [InlineData("https://sap.example.invalid:8443/sap/bc/adt/", "https://sap.example.invalid:8443")]
    [InlineData("https://sap.example.invalid/gateway/sap/bc/adt", "https://sap.example.invalid/gateway")]
    [InlineData("http://[::1]:8123", "http://[::1]:8123")]
    public void BaseUrlNormalizesOnlyTheServiceRoot(string input, string expected) => Assert.Equal(expected, AdtProfiles.NormalizeUrl(input));

    [Theory]
    [InlineData("https://u:p@sap.example.invalid")]
    [InlineData("https://sap.example.invalid?client=100")]
    [InlineData("https://sap.example.invalid/#password")]
    [InlineData("https://sap.example.invalid/sap/bc/adt/discovery")]
    [InlineData("ftp://sap.example.invalid")]
    public void InvalidUrlsDoNotBecomeFallbackCandidates(string value) => Assert.Throws<InvalidDataException>(() => AdtProfiles.NormalizeUrl(value));

    [Fact] public void LegacyProfileKeepsValuesButDoesNotEnableCredentialBearingDiscovery()
    {
        var p = JsonSerializer.Deserialize<ConnectionProfile>("{\"name\":\"Old\",\"targetKey\":\"old:00\",\"user\":\"OLD\",\"secretRef\":\"old.password.dpapi\",\"allowInsecureTls\":true}", JsonDefaults.Options)!;
        Assert.False(p.AdtAutoDiscovery); Assert.Empty(p.AdtCertificates); Assert.Equal(15, p.AdtTimeoutSeconds);
        Assert.True(p.AllowInsecureTls); Assert.Equal("old.password.dpapi", p.SecretRef); Assert.Equal("OLD", p.User); p.Validate();
    }

    [Fact] public async Task PemDerMetadataAndProfileScopedTrustRoundTrip()
    {
        using var temp = new TestDirectory(); using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=fictional.example.invalid", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder(); san.AddDnsName("fictional.example.invalid"); request.CertificateExtensions.Add(san.Build());
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var pem = Path.Combine(temp.Root, "test.pem"); await File.WriteAllTextAsync(pem, cert.ExportCertificatePem());
        var der = Path.Combine(temp.Root, "test.cer"); await File.WriteAllBytesAsync(der, cert.Export(X509ContentType.Cert));
        var preview = Assert.Single(await AdtProfiles.ReadCertificatesAsync(pem));
        Assert.Equal(preview.Sha256, Assert.Single(await AdtProfiles.ReadCertificatesAsync(der)).Sha256);
        Assert.Contains("fictional.example.invalid", preview.Subject); Assert.Contains("fictional.example.invalid", preview.San);
        var s = AppSettings.CreateDefault(temp.Root); var p = new ConnectionProfile { Id = "../../unsafe", Name = "test", User = "TEST", TargetKey = "test:00", SecretRef = "not-a-password", AdtBaseUrl = "https://fictional.example.invalid" };
        var refs = await AdtProfiles.StoreCertificatesAsync(s, p, [preview]); p.AdtCertificates = refs.ToList();
        Assert.StartsWith(Path.Combine(s.DataDirectory, "certificates") + Path.DirectorySeparatorChar, refs[0].File);
        Assert.Equal(preview.Sha256, Assert.Single(await AdtProfiles.ReadCertificatesAsync(refs[0].File)).Sha256);
        s.Profiles.Add(p); s.ActiveProfiles[p.TargetKey] = p.Id;
        using var mapping = JsonDocument.Parse(ProfileService.SerializeMapping(s));
        Assert.Equal(preview.Sha256, mapping.RootElement.GetProperty("overridesByTarget").GetProperty(p.TargetKey).GetProperty("adtTlsCertificates")[0].GetProperty("sha256").GetString());
        var other = new ConnectionProfile { Name = "other", User = "OTHER", TargetKey = "other:00" }; Assert.Empty(other.AdtCertificates);
        await File.WriteAllTextAsync(pem, cert.ExportCertificatePem() + key.ExportPkcs8PrivateKeyPem());
        await Assert.ThrowsAsync<InvalidDataException>(() => AdtProfiles.ReadCertificatesAsync(pem));
        var pfx = Path.Combine(temp.Root, "test.pfx"); await File.WriteAllBytesAsync(pfx, cert.Export(X509ContentType.Pfx));
        await Assert.ThrowsAsync<InvalidDataException>(() => AdtProfiles.ReadCertificatesAsync(pfx));
    }

    [Fact] public async Task SelectedProfileSnapshotKeepsActiveMappingAndOriginalFilesUnchanged()
    {
        using var temp = new TestDirectory(); var s = AppSettings.CreateDefault(temp.Root);
        var active = new ConnectionProfile { Name = "active", User = "ONE", TargetKey = "test:00", SecretRef = "../secrets/one.password.dpapi" };
        var selected = new ConnectionProfile { Name = "selected", User = "TWO", TargetKey = "test:00", SecretRef = "../secrets/two.password.dpapi", AdtBaseUrl = "https://second.example.invalid" };
        s.Profiles.AddRange([active, selected]); s.ActiveProfiles[active.TargetKey] = active.Id;
        await ProfileService.MaterializeAsync(s); var before = await File.ReadAllTextAsync(s.CredentialsFile);
        var snapshot = await AdtProfiles.CreateTestSnapshotAsync(s, selected.Id);
        Assert.Equal(active.Id, s.ActiveProfiles[active.TargetKey]); Assert.Equal(before, await File.ReadAllTextAsync(s.CredentialsFile));
        Assert.Equal(selected.Id, snapshot.Settings.ActiveProfiles[active.TargetKey]);
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(snapshot.Settings.CredentialsFile));
        var node = json.RootElement.GetProperty("overridesByTarget").GetProperty(active.TargetKey);
        Assert.Equal("TWO", node.GetProperty("user").GetString()); Assert.Equal(ProfileService.ResolveSecret(s.CredentialsFile, selected.SecretRef), node.GetProperty("secretRef").GetString());
        Assert.False(node.TryGetProperty("passwd", out _));
    }

    [Fact] public void SupportExportAllowsOnlyKnownMetadata()
    {
        var p = new ConnectionProfile { Name = "CUSTOMER", User = "ACCOUNT", TargetKey = "sensitive.example:00", AdtBaseUrl = "https://sensitive.example" };
        var diagnosis = JsonSerializer.SerializeToElement(new { secret = "PASSWORD", certificates = new[] { "CERTIFICATE_IDENTITY" }, stages = new[] { new { stage = "tls", status = "failed", code = "TLS_NAME_MISMATCH" }, new { stage = "ACCOUNT", status = "PASSWORD", code = "CUSTOMER" } } });
        var text = AdtProfiles.SupportReport(p, diagnosis);
        foreach (var value in new[] { "CUSTOMER", "ACCOUNT", "PASSWORD", "sensitive.example", "CERTIFICATE_IDENTITY" }) Assert.DoesNotContain(value, text);
        Assert.Contains("TLS_NAME_MISMATCH", text); Assert.Contains("REDACTED", text);
    }

    [Fact] public void OldAdtPackageIsBlockedBeforeItCanGuessEndpoints()
    {
        using var temp = new TestDirectory(); var s = AppSettings.CreateDefault(temp.Root); s.Adt.Installed = true;
        Directory.CreateDirectory(Path.GetDirectoryName(s.NodeExecutable)!); File.WriteAllText(s.NodeExecutable, "fixture");
        var root = Path.Combine(s.PackageDirectory, "adt"); Directory.CreateDirectory(Path.Combine(root, "dist")); File.WriteAllText(Path.Combine(root, "dist", "server.js"), "fixture");
        Assert.Throws<AdtPackageUnavailableException>(() => LaunchConfiguration.Create(s, "adt", false));
        File.WriteAllText(Path.Combine(root, "build-info.json"), "{\"connectionPolicyVersion\":1}");
        Assert.NotNull(LaunchConfiguration.Create(s, "adt", false));
    }
}
