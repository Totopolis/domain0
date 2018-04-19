using Autofac;
using Domain0.Infrastructure;
using Domain0.Nancy;
using Domain0.Test.Infrastructure;
using Nancy.Testing;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using HttpStatusCode = Nancy.HttpStatusCode;

namespace Domain0.Test
{
    public class TestModuleTests
    {
        [Fact]
        public async Task Validation_Name_Required()
        {
            var container = GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var result = await browser.Post("/testapimethod", with =>
            {
                with.Accept("application/json");
                with.JsonBody(new Nancy.Test {Id = 1});
            });

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task Test_Protobuf()
        {
            var container = GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var request = new Nancy.Test {Id = 1, Name = "test name"};
            var requestBytes = Nancy.Test.DefaultDescriptor.Write(request);
            var result = await browser.Post("/testapimethod", with =>
            {
                with.Accept("application/x-protobuf");
                with.Body(new MemoryStream(requestBytes), "application/x-protobuf");
                with.Header("Content-Length", requestBytes.Length.ToString());
                with.HttpsRequest();
            });

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var responseBytes = result.Body.ToArray();
            Assert.Equal(requestBytes.Length, responseBytes.Length);
            var response = Nancy.Test.DefaultDescriptor.Read(responseBytes);
            Assert.Equal(request.Id, response.Id);
            Assert.Equal(request.Name, response.Name);
        }

        [Fact]
        public async Task Test_Json()
        {
            var container = GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var request = new Nancy.Test { Id = 1, Name = "test name" };
            var requestString = JsonConvert.SerializeObject(request);
            var result = await browser.Post("/testapimethod", with =>
            {
                with.Accept("application/json");
                with.Body(requestString, "application/json");
                with.Header("Content-Length", requestString.Length.ToString());
                with.HttpsRequest();
            });

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var responseString = result.Body.AsString();
            Assert.Equal(requestString.Length, responseString.Length);
            var response = JsonConvert.DeserializeObject<Nancy.Test>(responseString);
            Assert.Equal(request.Id, response.Id);
            Assert.Equal(request.Name, response.Name);
        }

        public static IContainer GetContainer(Action<ContainerBuilder> upgrade = null)
        {
            var builder = new ContainerBuilder();

            var config = new LoggingConfiguration();
            config.AddTarget("console", new ColoredConsoleTarget());
            config.AddRule(LogLevel.Error, LogLevel.Fatal, "console");
            LogManager.Configuration = config;

            builder.Register(c => LogManager.GetCurrentClassLogger()).As<ILogger>().InstancePerDependency();
            builder.RegisterSource(new MoqRegistrationSource());
            builder.RegisterModule<ApplicationModule>();

            upgrade?.Invoke(builder);
            return builder.Build();
        }
    }
}
