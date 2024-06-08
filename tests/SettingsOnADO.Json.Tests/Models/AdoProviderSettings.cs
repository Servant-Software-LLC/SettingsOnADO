namespace SettingsOnADO.Json.Tests.Models;

public class AdoProviderSettings
{
    public CertificatePolicyEnum CertificatePolicy { get; set; } = CertificatePolicyEnum.TrustMockDBCertificates;
}
