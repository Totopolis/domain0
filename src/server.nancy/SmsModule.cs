using Domain0.Nancy.Model;
using Nancy;
using Nancy.ModelBinding;
using System.Linq;

namespace Domain0.Nancy
{
    public class SmsModule : NancyModule
    {
        public SmsModule()
        {
            Put("/api/sms/Register", ctx => Register(), name: nameof(Register));
            Put("/api/sms/ForceCreateUser", ctx => ForceCreateUser(), name: nameof(ForceCreateUser));
            Post("/api/sms/Login", ctx => Login(), name: nameof(Login));
            Post("/api/sms/ChangePassword", ctx => ChangePassword(), name: nameof(ChangePassword));
            Post("/api/sms/RequestResetPassword", ctx => RequestResetPassword(), name: nameof(RequestResetPassword));
            Post("/api/sms/ForceChangePhone", ctx => ForceChangePhone(), name: nameof(ForceChangePhone));
            Get("/api/sms/DoesUserExist", ctx => DoesUserExist(), name: nameof(DoesUserExist));
            Get("/api/sms/PhoneByUserId", ctx => PhoneByUserId(), name: nameof(PhoneByUserId));
            Get("/api/Refresh/{refreshToken}", ctx => Refresh(), name: nameof(Refresh));
            Get("/api/profile", ctx => GetMyProfile(), name: nameof(GetMyProfile));
            Get("/api/users/sms/{phone}", ctx => GetUserByPhone(), name: nameof(GetUserByPhone));
            Post("/api/profile/filter", ctx => UserFilter(), name: nameof(UserFilter));
            Post("/api/users/{id}", ctx => GetUserById(), name: nameof(GetUserById));
        }

        public object Register()
        {
            var phone = this.Bind<long>();
            return HttpStatusCode.OK;
        }

        public object ForceCreateUser()
        {
            var request = this.Bind<ForceCreateUserRequest>();
            return HttpStatusCode.OK;
        }

        public object Login()
        {
            var request = this.Bind<SmsLoginRequest>();
            return new AccessTokenResponse
            {
                AccessToken = "access_token",
                RefreshToken = "refresh_token",
                Profile = new UserProfile
                {
                    Id = 1,
                    Name = "name"
                }
            };
        }

        public object ChangePassword()
        {
            return HttpStatusCode.OK;
        }

        public object RequestResetPassword()
        {
            var phone = this.Bind<long>();
            return HttpStatusCode.OK;
        }

        public object ForceChangePhone()
        {
            var request = this.Bind<ForceChangePhone>();
            return HttpStatusCode.OK;
        }

        public object DoesUserExist()
        {
            var phone = this.Bind<long>();
            return true;
        }

        public object PhoneByUserId()
        {
            var userId = this.Bind<long>();
            return 79160000000;
        }

        public object Refresh()
        {
            var refreshToken = Context.Parameters.refreshToken;
            return new AccessTokenResponse
            {
                AccessToken = "access_token",
                RefreshToken = refreshToken,
                Profile = new UserProfile
                {
                    Id = 1,
                    Name = "name"
                }
            };
        }

        public object GetMyProfile()
        {
            return new UserProfile
            {
                Id = 1,
                Name = "test"
            };
        }

        public object GetUserByPhone()
        {
            var phone = Context.Parameters.phone;
            return new UserProfile
            {
                Id = 1,
                Name = "test",
                Phone = phone
            };
        }

        public object UserFilter()
        {
            var filter = this.Bind<UserProfileFilter>();
            return filter.UserIds.Select(id => new UserProfile
            {
                Id = id,
                Name = "test " + id,
                Phone = 79000000000 + id
            }).ToList();
        }

        public object GetUserById()
        {
            var id = Context.Parameters.id;
            return new UserProfile
            {
                Id = id,
                Name = "test " + id
            };
        }
    }
}
