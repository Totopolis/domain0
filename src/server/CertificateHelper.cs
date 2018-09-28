using System;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using Domain0.WinService.Certificate;

namespace Domain0.WinService.Infrastructure
{
    public class CertificateHelper
    {

        public static bool HasX509CertificateSettings()
        {
            return !string.IsNullOrEmpty(ConfigurationManager.AppSettings["X509_Filepath"])
                   || !string.IsNullOrEmpty(ConfigurationManager.AppSettings["X509_Subject"]);
        }

        public static X509Certificate2 GetX509Cert(Uri uri)
        {
            if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                return null;

            IX509CertificateProvider provider = null;

            var fileSettings = new X509FileSettings
            {
                FilePath = ConfigurationManager.AppSettings["X509_Filepath"],
                Password = ConfigurationManager.AppSettings["X509_Password"]
            };
            if (!string.IsNullOrEmpty(fileSettings.FilePath))
            {
                provider = new X509FileProvider(fileSettings);
            }
            else
            {
                if (!Enum.TryParse(ConfigurationManager.AppSettings["X509_Location"], out StoreLocation location))
                    location = StoreLocation.LocalMachine;
                if (!Enum.TryParse(ConfigurationManager.AppSettings["X509_StoreName"], out StoreName storeName))
                    storeName = StoreName.My;

                var storeSettings = new X509StoreSettings
                {
                    Location = location,
                    Name = storeName,
                    Subject = ConfigurationManager.AppSettings["X509_Subject"]
                };
                if (!string.IsNullOrEmpty(storeSettings.Subject))
                    provider = new X509StoreProvider(storeSettings);
            }

            X509Certificate2 x509Cert = null;
            if (uri.Scheme == "https")
                x509Cert = provider?.GetCert();
            return x509Cert;
        }
    }
}
