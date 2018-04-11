using Nancy;

namespace Domain0.Nancy
{
    public class TestModule : NancyModule
    {
        public TestModule()
        {
            Get("/testapimethod", ctx => HttpStatusCode.OK);
        }
    }
}