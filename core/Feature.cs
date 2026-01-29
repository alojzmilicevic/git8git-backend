using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace core;

public abstract class Feature
{
    private readonly Dictionary<Type, Type> _dependencies = new();
    private readonly Dictionary<Type, Func<IServiceProvider, object>> _settings = new();
    private readonly List<Type> _settingsTypes = [];

    protected void AddDependency<TInterface, TImplementation>() where TImplementation : class, TInterface
    {
        _dependencies[typeof(TInterface)] = typeof(TImplementation);
    }

    protected void AddSettings<T>() where T : class, new()
    {
        _settingsTypes.Add(typeof(T));
        _settings[typeof(T)] = provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var settings = new T();
            configuration.GetSection(typeof(T).Name).Bind(settings);
            return settings;
        };
    }

    public void Register(IServiceCollection services)
    {
        foreach (var (interfaceType, implementationType) in _dependencies)
        {
            services.AddScoped(interfaceType, implementationType);
        }

        foreach (var (settingsType, factory) in _settings)
        {
            services.AddSingleton(settingsType, factory);
        }
    }

    public virtual Task InitAsync(IServiceProvider provider) => Task.CompletedTask;
}
