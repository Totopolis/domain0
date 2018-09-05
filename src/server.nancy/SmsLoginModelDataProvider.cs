using Nancy.Swagger.Services;
using Nancy.Swagger;

namespace Domain0.Model
{
    public class SmsLoginModelDataProvider : ISwaggerModelDataProvider
    {
        public SwaggerModelData GetModelData()
            => SwaggerModelData.ForType<SmsLoginRequest>(with =>
            {
                with.Description("Login request");
                with.Property(x => x.Phone)
                    .Description("user phone like 71231234567")
                    .Required(true);
                with.Property(x => x.Password)
                    .Description("user password")
                    .Required(true);
            });
    }
}
