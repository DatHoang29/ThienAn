using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Services.Shared.Devices;
using Services.Shared.Logging;

namespace Services.Sample.Features.Devices;

/// <summary>
/// Author: TAC Team
/// Description: Nạp một lần các IDeviceCodec public từ thư mục plugin khi service khởi động.
/// Created date: 16/07/2026
/// </summary>
public sealed class DevicePluginLoader
{
    private static readonly string ContractAssemblyName =
        typeof(IDeviceCodec).Assembly.GetName().Name
        ?? throw new InvalidOperationException("Could not resolve the device contract assembly name.");

    public IReadOnlyCollection<IDeviceCodec> Load(string pluginDirectory)
    {
        if (string.IsNullOrWhiteSpace(pluginDirectory))
            throw new ArgumentException("Plugin directory is required.", nameof(pluginDirectory));

        var absoluteDirectory = Path.GetFullPath(
            Path.IsPathRooted(pluginDirectory)
                ? pluginDirectory
                : Path.Combine(AppContext.BaseDirectory, pluginDirectory));

        if (!Directory.Exists(absoluteDirectory))
        {
            LogManager.Info(
                $"Device plugin directory {absoluteDirectory} does not exist; only built-in codecs are used",
                kind: LogKind.Device);
            return Array.Empty<IDeviceCodec>();
        }

        var codecs = new List<IDeviceCodec>();
        foreach (var assemblyPath in Directory
                     .EnumerateFiles(absoluteDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            LoadAssemblyCodecs(assemblyPath, codecs);
        }

        LogManager.Info(
            $"Loaded {codecs.Count} device codec(s) from {absoluteDirectory}",
            kind: LogKind.Device);
        return codecs.AsReadOnly();
    }

    private void LoadAssemblyCodecs(string assemblyPath, ICollection<IDeviceCodec> codecs)
    {
        AssemblyName assemblyName;
        try
        {
            assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
        }
        catch (BadImageFormatException)
        {
            LogManager.Log(LogLevel.Debug, $"Skip native or invalid assembly {assemblyPath}", kind: LogKind.Device);
            return;
        }

        if (string.Equals(
                assemblyName.Name,
                ContractAssemblyName,
                StringComparison.OrdinalIgnoreCase))
        {
            LogManager.Log(LogLevel.Debug, $"Skip device contract assembly {assemblyPath}", kind: LogKind.Device);
            return;
        }

        var loadContext = new DevicePluginLoadContext(assemblyPath, ContractAssemblyName);
        Assembly assembly;
        try
        {
            assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Could not load device plugin assembly '{assemblyPath}'.",
                exception);
        }

        foreach (var type in GetLoadableTypes(assembly, assemblyPath))
        {
            if (!typeof(IDeviceCodec).IsAssignableFrom(type)
                || !type.IsClass
                || type.IsAbstract
                || type.ContainsGenericParameters)
            {
                continue;
            }

            if (type.GetConstructor(Type.EmptyTypes) is null)
            {
                throw new InvalidOperationException(
                    $"Device codec '{type.FullName}' must have a public parameterless constructor.");
            }

            try
            {
                codecs.Add((IDeviceCodec)Activator.CreateInstance(type)!);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Could not create device codec '{type.FullName}'.",
                    exception);
            }
        }
    }

    private IEnumerable<Type> GetLoadableTypes(
        Assembly assembly,
        string assemblyPath)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            var details = string.Join(
                Environment.NewLine,
                exception.LoaderExceptions
                    .Where(item => item is not null)
                    .Select(item => item!.Message));
            LogManager.Log(
                LogLevel.Warning,
                $"Some types in device plugin assembly {assemblyPath} could not be inspected. "
                + $"Loadable codec types will still be considered. Details: {details}",
                kind: LogKind.Device,
                exception: exception);

            return exception.Types
                .Where(type => type is not null)
                .Cast<Type>();
        }
    }

    private sealed class DevicePluginLoadContext(
        string pluginPath,
        string contractAssemblyName) : AssemblyLoadContext(isCollectible: false)
    {
        private readonly AssemblyDependencyResolver _resolver = new(pluginPath);
        private readonly string _pluginDirectory =
            Path.GetDirectoryName(pluginPath)
            ?? throw new InvalidOperationException("Plugin path has no directory.");

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (string.Equals(
                    assemblyName.Name,
                    contractAssemblyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (resolvedPath is null && !string.IsNullOrWhiteSpace(assemblyName.Name))
            {
                var adjacentPath = Path.Combine(
                    _pluginDirectory,
                    assemblyName.Name + ".dll");
                if (File.Exists(adjacentPath))
                    resolvedPath = adjacentPath;
            }

            return resolvedPath is null ? null : LoadFromAssemblyPath(resolvedPath);
        }

        protected override nint LoadUnmanagedDll(string unmanagedDllName)
        {
            var resolvedPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return resolvedPath is null
                ? nint.Zero
                : LoadUnmanagedDllFromPath(resolvedPath);
        }
    }
}
