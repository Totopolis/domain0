namespace Domain0.Service.Tokens
{
    public class TokenClaims
    {
        public const string CLAIM_SUBJECT = "sub";
        public const string CLAIM_PERMISSIONS = "permissions";
        public const string CLAIM_TOKEN_ID = "tid";
        public const string CLAIM_TOKEN_TYPE = "typ";
        public const string CLAIM_TOKEN_TYPE_ACCESS = "access_token";
        public const string CLAIM_TOKEN_TYPE_REFRESH = "refresh_token";

        public const string CLAIM_PERMISSIONS_ADMIN = "domain0.superuser";
    }
}
