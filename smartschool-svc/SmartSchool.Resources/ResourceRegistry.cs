using System.Collections.Concurrent;
using System.Reflection;
using SmartSchool.Resources.Descriptors;
using SmartSchool.Resources.Scanning;

namespace SmartSchool.Resources;

/// <summary>
/// Application-wide cache of resource descriptors built once at startup.
/// Thread-safe, immutable after Initialize().
/// </summary>
public sealed class ResourceRegistry
{
    private readonly ConcurrentDictionary<string, ResourceDescriptor> _byKey = new();
    private IReadOnlyList<ResourceDescriptor> _all = [];

    public IReadOnlyList<ResourceDescriptor> All => _all;

    public ResourceDescriptor? Get(string key) =>
        _byKey.TryGetValue(key, out var d) ? d : null;

    public void Initialize(params Assembly[] assemblies)
    {
        var scanner = new ResourceScanner();
        var descriptors = scanner.Scan(assemblies);
        _all = descriptors;
        foreach (var d in descriptors) _byKey[d.Key] = d;
    }
}
