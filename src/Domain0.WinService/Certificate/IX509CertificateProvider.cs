using System.Security.Cryptography.X509Certificates;

namespace Domain0.WinService.Certificate
{
    public interface IX509CertificateProvider
    {
        X509Certificate2 GetCert();
    }
}