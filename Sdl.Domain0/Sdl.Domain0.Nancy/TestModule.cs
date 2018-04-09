using Nancy;

namespace Sdl.Domain0.Nancy
{
    public class TestModule : NancyModule
    {
        public TestModule()
        {
            Get("/testapimethod", ctx => HttpStatusCode.OK);
        }
    }
}
