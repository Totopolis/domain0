using Nancy;

namespace Domain0.Nancy
{
    public class TestModule : NancyModule
    {
        public TestModule()
        {
            Get("/testapimethod", ctx => TestMethod(), null, nameof(TestMethod));
        }

        public object TestMethod()
        {
            return HttpStatusCode.OK;
        }
    }
}