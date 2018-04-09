using System.Security.Cryptography.X509Certificates;

namespace Sdl.Domain0.Infrastructure
{
    public interface IX509CertificateProvider
    {
        X509Certificate2 GetCert();
    }
}