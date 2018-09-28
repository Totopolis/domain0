using System;
using System.Configuration;

namespace Domain0.WinService
{
    class Settings
    {
        public const string DefaultConnectionString =
            "Data Source=.;Initial Catalog=Domain0;Persist Security Info=True;Integrated Security=True";

        public static string ConnectionString => 
            ConfigurationManager.ConnectionStrings["Database"]?.ConnectionString 
            ?? DefaultConnectionString;

        public static Uri Uri =>
            string.IsNullOrEmpty(ConfigurationManager.AppSettings["Url"])
                ? new Uri(HasX509CertificateSettings()? DefaultHttpsUri : DefaultHttpUri)
                : new Uri(ConfigurationManager.AppSettings["Url"]);

        public static bool HasX509CertificateSettings()
        {
            return !string.IsNullOrEmpty(ConfigurationManager.AppSettings["X509_Filepath"])
                   || !string.IsNullOrEmpty(ConfigurationManager.AppSettings["X509_Subject"]);
        }


#if DEBUG
        public const string ServiceName = "domain0Debug";
#else
        public const string ServiceName = "domain0";
#endif

#if DEBUG
        public const string DefaultHttpsUri = "https://localhost:4443";
#else
        public const string DefaultHttpsUri = "https://localhost";
#endif

#if DEBUG
        public const string DefaultHttpUri = "http://localhost:8880";
#else
        public const string DefaultHttpUri = "http://localhost";
#endif
    }
}