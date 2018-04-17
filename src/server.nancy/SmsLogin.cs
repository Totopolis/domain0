namespace Domain0.Nancy.Model
{
    public class SmsLoginRequest
    {
        public string Phone { get; set; }

        public string Password { get; set; }
    }

    public class SmsLoginProfile
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class SmsLoginResponse
    {
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public SmsLoginProfile Profile { get; set; }
    }
}
