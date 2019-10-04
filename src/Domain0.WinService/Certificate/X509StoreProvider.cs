using System;
using System.Security.Cryptography.X509Certificates;

namespace Domain0.WinService.Certificate
{
    public class X509StoreSettings
    {
        public string Subject { get; set; }

        public StoreName Name { get; set; }

        public StoreLocation Location { get; set; }
    }

    public class X509StoreProvider : IX509CertificateProvider
    {
        private readonly X509StoreSettings _settings;

        public X509StoreProvider(X509StoreSettings settings)
        {
            _settings = settings;
        }

        public X509Certificate2 GetCert()
        {
            try
            {
                using (var store = new X509Store(_settings.Name, _settings.Location))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var certs = store.Certificates.Find(X509FindType.FindBySubjectName, _settings.Subject, true);
                    return certs.Count > 0 ? certs[0] : null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
