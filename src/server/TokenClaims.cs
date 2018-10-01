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
        public const string CLAIM_PERMISSIONS_BASIC = "domain0.basic";
        public const string CLAIM_PERMISSIONS_FORCE_CHANGE_PHONE = "domain0.forceChangePhone";
        public const string CLAIM_PERMISSIONS_FORCE_CHANGE_EMAIL = "domain0.forceChangeEmail";
        public const string CLAIM_PERMISSIONS_FORCE_PASSWORD_RESET = "domain0.forcePasswordReset";
        public const string CLAIM_PERMISSIONS_FORCE_CREATE_USER = "domain0.forceCreateUser";
        public const string CLAIM_PERMISSIONS_VIEW_USERS = "domain0.viewUsers";
        public const string CLAIM_PERMISSIONS_EDIT_USERS = "domain0.editUsers";
        public const string CLAIM_PERMISSIONS_VIEW_PROFILE = "domain0.viewProfile";
    }
}
