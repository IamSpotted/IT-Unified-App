namespace MauiApp1.Interfaces;

/// <summary>
/// Marker interface for services that should be registered as singletons
/// </summary>
public interface ISingletonService
{
}

/// <summary>
/// Marker interface for services that should be registered as transients
/// </summary>
public interface ITransientService
{
}

/// <summary>
/// Marker interface for services that should be registered as scoped
/// </summary>
public interface IScopedService
{
}

/// <summary>
/// Marker interface for view models
/// </summary>
public interface IViewModel
{
}

/// <summary>
/// Marker interface for pages/views
/// </summary>
public interface IView
{
}
