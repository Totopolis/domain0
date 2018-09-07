using Nancy.Authentication.Stateless;

namespace Domain0.Nancy.Infrastructure
{
    internal interface IAuthenticationConfigurationBuilder
    {
        StatelessAuthenticationConfiguration Build();
    }
}