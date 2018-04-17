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
                        .ProduceMimeTypes(new[] {"application/json", "application/x-protobuf"})
                        .ConsumeMimeTypes(new[] {"application/json", "application/x-protobuf"})
                        .Summary("test method")
                        .Description("this is a test method")
                        .BodyParameter(b => b.Schema<Test>().Name("parameter").Description("description").Build())
                        .Response((int) HttpStatusCode.OK, r => r.Schema<Test>().Description("Simple response").Build())));
        }
    }
}
