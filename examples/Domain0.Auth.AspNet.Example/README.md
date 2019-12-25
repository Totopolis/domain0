# Domain0.Auth.AspNet.Example

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // ...
    // var tokenValidationSettings = ...
    services.AddDomain0Auth(tokenValidationSettings);
}
```

```csharp
// ***************************************************
// Add Authorization annotation with Domain0 policy
// to a controller or to an action within a controller
[Authorize(Domain0Auth.Policy, Roles = "api.basic")]
// ***************************************************
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
// ...
```