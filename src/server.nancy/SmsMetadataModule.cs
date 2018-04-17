using Domain0.Nancy.Model;
using Nancy.Metadata.Modules;
using Nancy.Swagger;
using Swagger.ObjectModel;
using System.Collections.Generic;
using Nancy.Swagger.Services;
using System.Net;

namespace Domain0.Nancy
{
    public class SmsMetadataModule : MetadataModule<PathItem>
    {
        public SmsMetadataModule(ISwaggerModelCatalog modelCatalog, IEnumerable<ISwaggerModelDataProvider> providers)
        {
            Describe[nameof(SmsModule.DoesUserExist)] = description => 
                description.AsSwagger(with => with.Operation(op => op
                    .OperationId(nameof(SmsModule.PhoneByUserId))
                    .ProduceMimeTypes(new [] { "application/json" })
                    .Parameter(new Parameter { In = ParameterIn.Header, Name="Authorization", Type="string", Default="Bearer", Required=true, Description="Authorization token from login method"})
                    .Parameter(new Parameter { In = ParameterIn.Query, Name = "Phone", Default = "", MinLength=11, Required = true, Description = "user's phone with single number, started from 7 for Russia, 79162233224 for example" })
                    .Tag("Sms")
                    .Response((int)HttpStatusCode.OK, r => r.Schema(new Schema {Type="boolean"}).Description("True if user exists else false").Build())
                ));

            Describe[nameof(SmsModule.PhoneByUserId)] = description => description.AsSwagger(
                with => with.Operation(op => op
                    .OperationId(nameof(SmsModule.PhoneByUserId))
                    .Tag("Sms")
                    .ProduceMimeTypes(new[] { "application/json" })
                    .ConsumeMimeTypes(new[] { "application/json" })
                    .Parameter(new Parameter { In = ParameterIn.Header, Name = "Authorization", Type = "string", Default = "Bearer", Required = true, Description = "Authorization token from login method" })
                    .Parameter(new Parameter { In = ParameterIn.Query, Name = "Id", Type = "integer", Required = true, Description = "User Id" })
                    .Response((int)HttpStatusCode.OK, r => r.Schema(new Schema {Type = "integer" }).Description("Return phone by user Id").Build())));

            modelCatalog.AddModel<SmsLoginRequest>();
            modelCatalog.AddModel<SmsLoginResponse>();
            modelCatalog.AddModel<SmsLoginProfile>();
            Describe[nameof(SmsModule.Login)] = description => description.AsSwagger(
                with => with.Operation(op => op
                    .OperationId(nameof(SmsModule.Login))
                    .Tag("Sms")
                    .ProduceMimeTypes(new[] { "application/json" })
                    .ConsumeMimeTypes(new[] { "application/json" })
                    .BodyParameter(p => p.Schema<SmsLoginRequest>().Name("login request parameter").Build())
                    .Response((int) HttpStatusCode.OK, r => r.Schema<SmsLoginResponse>().Description("Ok").Build())));

            Describe[nameof(SmsModule.Register)] = description => description.AsSwagger(
                with => with.Operation(op => op
                    .OperationId(nameof(SmsModule.Register))
                    .Tag("Sms")
                    .ProduceMimeTypes(new[] { "application/json" })
                    .ConsumeMimeTypes(new[] { "application/json" })
                    .BodyParameter(b => b.Name("phone").Schema(new Schema()).Description("user's phone with single number, started from 7 for Russia, 79162233224 for example").Build())
                    .Response(r => r.Description("Ok").Build())));

            Describe[nameof(SmsModule.ChangePassword)] = description => description.AsSwagger(
                with => with.Operation(op => op.OperationId(nameof(SmsModule.ChangePassword)).Tag("Sms").Response(r => r.Description("Ok").Build())));

            modelCatalog.AddModel<ForceCreateUserRequest>();
            Describe[nameof(SmsModule.ForceCreateUser)] = description => description.AsSwagger(
                with => with.Operation(op => op
                    .OperationId(nameof(SmsModule.ForceCreateUser))
                    .Tag("Sms")
                    .ProduceMimeTypes(new[] { "application/json" })
                    .ConsumeMimeTypes(new[] { "application/json" })
                    .Parameter(new Parameter { In = ParameterIn.Header, Name = "Authorization", Type = "string", Default = "Bearer", Required = true, Description = "Authorization token from login method" })
                    .BodyParameter(b => b.Name("parameters").Schema<ForceCreateUserRequest>().Description("parameters for force create").Build())
                    .Response((int) HttpStatusCode.OK, r => r.Description("Ok").Build())));

            Describe[nameof(SmsModule.RequestResetPassword)] = description => description.AsSwagger(
                with => with.Operation(op => op
                    .OperationId(nameof(SmsModule.RequestResetPassword))
                    .Tag("Sms")
                    .ProduceMimeTypes(new[] { "application/json" })
                    .ConsumeMimeTypes(new[] { "application/json" })
                    .BodyParameter(b => b.Name("phone").Schema(new Schema()).Description("user's phone with single number, started from 7 for Russia, 79162233224 for example").Build())
                    .Response(r => r.Description("Ok").Build())));

            modelCatalog.AddModel<ForceChangePhone>();
            Describe[nameof(SmsModule.ForceChangePhone)] = description => description.AsSwagger(
                with => with.Operation(op => op
                    .OperationId(nameof(SmsModule.ForceChangePhone))
                    .Tag("Sms")
                    .ProduceMimeTypes(new[] { "application/json" })
                    .ConsumeMimeTypes(new[] { "application/json" })
                    .BodyParameter(b => b.Name("parameters").Schema<ForceChangePhone>().Description("parameters for force change phone").Build())
                    .Response(r => r.Description("Ok").Build())));
        }
    }
}
