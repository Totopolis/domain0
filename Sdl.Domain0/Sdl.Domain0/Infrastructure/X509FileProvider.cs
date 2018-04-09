using System;
using System.Security.Cryptography.X509Certificates;

namespace Sdl.Domain0.Infrastructure
{
    public class X509FileSettings
    {
        public string FilePath { get; set; }

        public string Password { get; set; }
    }

    public class X509FileProvider : IX509CertificateProvider
    {
        private readonly X509FileSettings _settings;

        public X509FileProvider(X509FileSettings settings)
        {
            _settings = settings;
        }

        public X509Certificate2 GetCert()
        {
            try
            {
                return new X509Certificate2(_settings.FilePath, _settings.Password, X509KeyStorageFlags.DefaultKeySet;
            }
            catch (Exception)
            {
                return null;
            }
            
        }
    }
}