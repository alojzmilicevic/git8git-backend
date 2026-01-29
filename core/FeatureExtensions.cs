using Microsoft.Extensions.DependencyInjection;

namespace core;

public static class FeatureExtensions
{
    private static readonly List<Feature> RegisteredFeatures = new();

    public static IServiceCollection AddFeature<TFeature>(this IServiceCollection services) where TFeature : Feature, new()
    {
        var feature = new TFeature();
        feature.Register(services);
        RegisteredFeatures.Add(feature);
        return services;
    }

    public static async Task WarmUpFeatures(this IServiceProvider provider)
    {
        foreach (var feature in RegisteredFeatures)
        {
            await feature.InitAsync(provider);
        }
    }
}
