using Nancy.Metadata.Modules;
using Nancy.Swagger;
using Swagger.ObjectModel;
using System.Net;

namespace Domain0.Nancy
{
    public class TestMetadataModule : MetadataModule<PathItem>
    {
        public TestMetadataModule()
        {
            Describe[nameof(TestModule.TestMethod)] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("Test")
                        .Tag("Test")
                        .Summary("test method")
                        .Description("this is a test method")
                        .Response(HttpStatusCode.OK, r => r.Description("Simple response"))));
        }
    }
}
