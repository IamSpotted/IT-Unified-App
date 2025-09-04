using System.Reflection;
using MauiApp1.Services;

namespace MauiApp1.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAutoDiscoveredServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // Register singleton services
        RegisterServicesByInterface(services, assembly, typeof(ISingletonService), ServiceLifetime.Singleton);
        
        // Register transient services
        RegisterServicesByInterface(services, assembly, typeof(ITransientService), ServiceLifetime.Transient);
        
        // Register scoped services
        RegisterServicesByInterface(services, assembly, typeof(IScopedService), ServiceLifetime.Scoped);
        
        // Register view models as transients
        RegisterServicesByInterface(services, assembly, typeof(IViewModel), ServiceLifetime.Transient);
        
        // Register views/pages as transients
        RegisterServicesByInterface(services, assembly, typeof(Interfaces.IView), ServiceLifetime.Transient);
        
        // Register generic filter service
        services.AddTransient(typeof(IFilterService<>), typeof(FilterService<>));
        
        // Explicitly register additional services
        // Duplicate detection services temporarily disabled
        // services.AddTransient<IDuplicateResolutionDialogService, MauiApp1.Services.DuplicateResolutionDialogService>();
        
        return services;
    }
    
    /// <summary>
    /// Register view-viewmodel pairs automatically
    /// </summary>
    public static IServiceCollection AddViewViewModelMappings(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var viewTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && 
                       (t.IsSubclassOf(typeof(ContentPage)) || 
                        t.IsSubclassOf(typeof(ContentView)) ||
                        t.GetInterfaces().Contains(typeof(Interfaces.IView))))
            .ToList();

        foreach (var viewType in viewTypes)
        {
            // Try to find corresponding ViewModel
            var viewModelName = viewType.Name + "ViewModel";
            var viewModelType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == viewModelName && t.GetInterfaces().Contains(typeof(IViewModel)));
            
            if (viewModelType != null)
            {
                services.AddTransient(viewType);
                services.AddTransient(viewModelType);
            }
        }
        
        return services;
    }
    
    private static void RegisterServicesByInterface(IServiceCollection services, Assembly assembly, 
        Type markerInterface, ServiceLifetime lifetime)
    {
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(markerInterface))
            .ToList();

        foreach (var implementationType in types)
        {
            // Register the concrete type
            services.Add(new ServiceDescriptor(implementationType, implementationType, lifetime));
            
            // Register all interfaces (except marker interfaces)
            var interfaces = implementationType.GetInterfaces()
                .Where(i => i != markerInterface && 
                           i != typeof(ISingletonService) && 
                           i != typeof(ITransientService) && 
                           i != typeof(IScopedService) &&
                           i != typeof(IViewModel) &&
                           i != typeof(Interfaces.IView))
                .ToList();

            foreach (var interfaceType in interfaces)
            {
                services.Add(new ServiceDescriptor(interfaceType, implementationType, lifetime));
            }
        }
    }
}
