using System;
using System.Collections.Generic;
using Autofac.Core;
using System.Linq;
using Autofac.Core.Registration;
using Autofac.Core.Activators.Delegate;
using Moq;
using Autofac.Core.Lifetime;

namespace Domain0.Test.Infrastructure
{
    public class MoqRegistrationSource : IRegistrationSource
    {
        public bool IsAdapterForIndividualComponents => false;

        public IEnumerable<IComponentRegistration> RegistrationsFor(Autofac.Core.Service service, Func<Autofac.Core.Service, IEnumerable<IComponentRegistration>> registrationAccessor)
        {
            if (!service.Description.Contains("Domain0"))
                return Enumerable.Empty<IComponentRegistration>();

            var accessors = registrationAccessor(service);
            if (accessors.Any())
                return Enumerable.Empty<IComponentRegistration>();

            var type = service as IServiceWithType;
            var mock = CreateFake(type.ServiceType);
            var mockType = mock.GetType();
            var serviceRegistration = new ComponentRegistration(
                Guid.NewGuid(),
                new DelegateActivator(type.ServiceType, (c, p) => mock),
                new CurrentScopeLifetime(),
                InstanceSharing.None,
                InstanceOwnership.OwnedByLifetimeScope,
                new[] {service},
                new Dictionary<string, object>());

            return new[] {serviceRegistration};
        }

        public static object CreateFake(Type type)
        {
            var mockType = typeof(Mock<>).MakeGenericType(type);
            var property = mockType.GetProperty("Object", mockType);
            var instance = Activator.CreateInstance(mockType);
            return property.GetValue(instance, null);
        }

    }
}
