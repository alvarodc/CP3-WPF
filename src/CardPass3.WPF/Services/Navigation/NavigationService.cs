using Microsoft.Extensions.DependencyInjection;

namespace CardPass3.WPF.Services.Navigation
{

public interface INavigationService
{
    object? Resolve(string moduleName);
}

public class NavigationService(IServiceProvider sp) : INavigationService
{
    private static readonly Dictionary<string, Type> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        // Add module name → ViewModel type mappings here
        // ["readers"] = typeof(ReadersViewModel),
        // ["events"]  = typeof(EventsViewModel),
        // ["users"]   = typeof(UsersViewModel),
    };

    public object? Resolve(string moduleName)
    {
        if (_map.TryGetValue(moduleName, out var vmType))
            return sp.GetRequiredService(vmType);

        return null;
    }
}
