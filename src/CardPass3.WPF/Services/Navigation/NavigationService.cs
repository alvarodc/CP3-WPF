using CardPass3.WPF.Modules.Readers.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CardPass3.WPF.Services.Navigation;

public interface INavigationService
{
    object? Resolve(string moduleName);
}

public class NavigationService(IServiceProvider sp) : INavigationService
{
    private static readonly Dictionary<string, Type> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["readers"] = typeof(ReadersViewModel),
        // ["events"]  = typeof(EventsViewModel),   // Iter-2
        // ["users"]   = typeof(UsersViewModel),    // Iter-3
        // ["areas"]   = typeof(AreasViewModel),    // Iter-5
        // ["config"]  = typeof(ConfigViewModel),   // Iter-5
    };

    public object? Resolve(string moduleName)
    {
        if (_map.TryGetValue(moduleName, out var vmType))
            return sp.GetRequiredService(vmType);
        return null;
    }
}
