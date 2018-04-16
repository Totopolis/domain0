using Nancy;

namespace Domain0.Nancy
{
    public class SmsModule : NancyModule
    {
        public SmsModule()
        {
            Put("/api/sms/Register", ctx => null);
            Put("/api/sms/ForceCreateUser", ctx => null);
            Post("/api/sms/Login", ctx => null);
            Post("/api/sms/ChangePassword", ctx => null);
            Post("/api/sms/RequestResetPassword", ctx => null);
            Post("/api/sms/ForceChangePhone", ctx => null);
            Get("/api/sms/DoesUserExist", ctx => null);
            Get("/api/sms/PhoneByUserId", ctx => null);
            Get("/api/Refresh/{refreshToken}", ctx => null);
            Get("/api/profile", ctx => null);
            Get("/api/users/{type}/{profileId}", ctx => null);
            Post("/api/profile/filter", ctx => null);
            Post("/api/users/{id}" , ctx => null);
        }
    }
}
