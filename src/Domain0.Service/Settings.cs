using System;
using System.Configuration;

namespace Domain0.Service
{
    public class Settings
    {
        private const string DefaultHttpUri = "http://localhost";

        private const string DefaultConnectionString =
            "Data Source=.;Initial Catalog=Domain0;Persist Security Info=True;Integrated Security=True";

        public static Uri Uri =>
            string.IsNullOrEmpty(ConfigurationManager.AppSettings["Url"])
                ? new Uri(DefaultHttpUri)
                : new Uri(ConfigurationManager.AppSettings["Url"]);

        public static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["Database"]?.ConnectionString
            ?? DefaultConnectionString;
    }
}