using Gerakul.ProtoBufSerializer;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Swagger.Annotations.Attributes;
using NLog;
using Swagger.ObjectModel;

namespace Domain0.Nancy
{
    [Model("Test model")]
    public class Test
    {
        [ModelProperty(Ignore = true)]
        public static MessageDescriptor<Test> DefaultDescriptor
            => MessageDescriptor<Test>.Create(new[]
            {
                FieldSetting<Test>.CreateInt32(1, c => c.Id, (c, v) => c.Id = v),
                FieldSetting<Test>.CreateString(2, c => c.Name, (c, v) => c.Name = v, c => c.Name?.Length > 0),
            });

        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class TestModule : NancyModule
    {
        private readonly ILogger _logger;

        public TestModule(ILogger logger)
        {
            _logger = logger;

            Post("/testapimethod", ctx => TestMethod(), null, nameof(TestMethod));
        }

        [Route(nameof(TestMethod))]
        [Route(HttpMethod.Post, "/testapimethod")]
        [Route(Produces = new[] {"application/json", "application/x-protobuf"})]
        [Route(Consumes = new[] {"application/json", "application/x-protobuf"})]
        [Route(Tags = new[] {"Test"}, Summary = "Test method implements basic functions")]
        [RouteParam(ParamIn = ParameterIn.Body, Name = "test parameter", ParamType = typeof(Test), Required = true)]
        [SwaggerResponse(HttpStatusCode.OK, Message = "sample test response", Model = typeof(Test))]
        public object TestMethod()
        {
            var result = this.Bind<Test>();
            _logger.Debug("TestMethod invoked");
            return result;
        }
    }
}