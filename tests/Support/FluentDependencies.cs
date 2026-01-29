using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;

namespace tests.Support;

public class With
{
    private readonly Dictionary<Type, object> _services = new();
    private readonly Dictionary<Type, Type> _implementations = new();

    public void Fake<TInterface>() where TInterface : class
    {
        _services[typeof(TInterface)] = A.Fake<TInterface>();
    }

    public TInterface GetFake<TInterface>() where TInterface : class
    {
        if (_services.TryGetValue(typeof(TInterface), out var service))
        {
            return (TInterface)service;
        }
        throw new InvalidOperationException($"No fake registered for {typeof(TInterface).Name}");
    }

    public void Real<TInterface, TImplementation>() where TImplementation : class, TInterface
    {
        _implementations[typeof(TInterface)] = typeof(TImplementation);
    }

    public void Real<TInterface>(TInterface instance) where TInterface : class
    {
        _services[typeof(TInterface)] = instance;
    }

    public void Configure(IServiceCollection services)
    {
        foreach (var (interfaceType, instance) in _services)
        {
            var descriptor = services.FirstOrDefault(d => d.ServiceType == interfaceType);
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            services.AddSingleton(interfaceType, instance);
        }

        foreach (var (interfaceType, implementationType) in _implementations)
        {
            var descriptor = services.FirstOrDefault(d => d.ServiceType == interfaceType);
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            services.AddScoped(interfaceType, implementationType);
        }
    }
}
