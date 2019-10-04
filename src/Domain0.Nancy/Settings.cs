using System;
using System.Configuration;

namespace Domain0.Nancy
{
    public class Settings
    {
        public const string DefaultConnectionString =
            "Data Source=.;Initial Catalog=Domain0;Persist Security Info=True;Integrated Security=True";

        public static string ConnectionString => 
            ConfigurationManager.ConnectionStrings["Database"]?.ConnectionString 
            ?? DefaultConnectionString;

        public static Uri Uri =>
            string.IsNullOrEmpty(ConfigurationManager.AppSettings["Url"])
                ? new Uri(DefaultHttpUri)
                : new Uri(ConfigurationManager.AppSettings["Url"]);


#if DEBUG
        public const string ServiceName = "domain0Debug";
#else
        public const string ServiceName = "domain0";
#endif

#if DEBUG
        public const string DefaultHttpUri = "http://localhost:8880";
#else
        public const string DefaultHttpUri = "http://localhost";
#endif
    }
}