using Nancy;
using NLog;

namespace Domain0.Nancy
{
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
            return HttpStatusCode.OK;
        }
    }
}