using System.Threading.Tasks;
using Domain0.Nancy;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Domain0.Test
{
    public class SmsModuleTests
    {
        [Fact]
        public async Task Registration_Validation_Phone()
        {
            var container = TestModuleTests.GetContainer();
            var bootstrapper = new Domain0Bootstrapper(container);
            var browser = new Browser(bootstrapper);

            var phone = 79000000000;
            var result = await browser.Put(SmsModule.RegisterUrl, with =>
            {
                with.Accept("application/json");
                with.JsonBody(phone);
            });

            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        }
    }
}
