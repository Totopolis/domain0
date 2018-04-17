namespace Domain0.Nancy.Model
{
    public class AccessTokenResponse
    {
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public UserProfile Profile { get; set; }
    }
}
