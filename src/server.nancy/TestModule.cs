using Nancy;
using NLog;

namespace Domain0.Nancy
{
    public class Test
    {
        public int Id { get; set; }
    }

    public class TestModule : NancyModule
    {
        private readonly ILogger _logger;

        public TestModule(ILogger logger)
        {
            _logger = logger;

            Get("/testapimethod", ctx => TestMethod(), null, nameof(TestMethod));
        }

        public object TestMethod()
        {
            _logger.Debug("TestMethod invoked");
            return Negotiate
                .WithMediaRangeModel("application/x-protobuf", new Test { Id = 1 })
                .WithMediaRangeModel("application/json", new Test { Id = 2 });
        }
    }
}