using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ReadyPerfectly.Utilities;

public static class TypeUtility
{
    public static IEnumerable<TAttribute> GetTypesWithAttribute<TAttribute>() where TAttribute : Attribute
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var type in types)
            {
                if (type is not { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }) continue;
                
                var attribute = type.GetCustomAttribute<TAttribute>(inherit: true);
                    
                if (attribute is not null) yield return attribute;
            }
        }
    }

    public static IEnumerable<Type> GetTypesImplementing<TInterface>(ILogger? logger = null) where TInterface : class
    {
        var targetType = typeof(TInterface);

        if (!targetType.IsInterface) throw new ArgumentException($"The type '{targetType.FullName}' must be an interface.");

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                logger?.LogWarning($"Could not load types from assembly '{assembly.FullName}'. Details: {ex.Message}");
                types = [.. ex.Types.Where(t => t != null).Cast<Type>()];
            }
            catch (Exception ex)
            {
                logger?.LogError($"An unexpected error occurred while loading types from assembly '{assembly.FullName}'. Details: {ex.Message}");
                continue;
            }

            foreach (var type in types)
                if (IsConcreteImplementation(type)) yield return type;
        }

        // Filter for concrete, non-generic, instantiable classes that implements the target interface
        bool IsConcreteImplementation(Type type) =>
            type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } &&
            targetType.IsAssignableFrom(type);

    }

    public static IEnumerable<TInterface> Instances<TInterface>(this IEnumerable<Type> types)
    {
        var targetType = typeof(TInterface);

        foreach (var type in types)
        {
            if (IsConcreteImplementation(type))
                yield return (TInterface)Activator.CreateInstance(type)!;
        }

        // Filter for concrete, non-generic, instantiable classes that implements the target interface
        bool IsConcreteImplementation(Type type) =>
            type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } &&
            targetType.IsAssignableFrom(type);
    }

    public static IEnumerable<TInterface> GetInstancesOfTypesImplementing<TInterface>(ILogger? logger = null) where TInterface : class
    {
        var targetType = typeof(TInterface);

        if (!targetType.IsInterface)
            throw new ArgumentException($"The type '{targetType.FullName}' must be an interface.", nameof(TInterface));

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                logger?.LogWarning(ex, "Could not load all types from assembly '{AssemblyName}'.", assembly.FullName);
                types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to load types from assembly '{AssemblyName}'.", assembly.FullName);
                continue;
            }

            foreach (var type in types)
            {
                if (type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } &&
                    targetType.IsAssignableFrom(type))
                {
                    yield return (TInterface)Activator.CreateInstance(type)!;
                }
            }
        }
    }

    public static IEnumerable<TInterface> GetInstancesOfTypesImplementing<TInterface>(IServiceProvider services, ILogger? logger = null)
    {
        var targetType = typeof(TInterface);
        if (!targetType.IsInterface)
            throw new ArgumentException($"The type '{targetType.FullName}' must be an interface.", nameof(TInterface));

        using var scope = services.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                logger?.LogWarning(ex, "Could not load all types from assembly '{AssemblyName}'.", assembly.FullName);
                types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to load types from assembly '{AssemblyName}'.", assembly.FullName);
                continue;
            }

            foreach (var type in types)
            {
                if (type.IsClass 
                    && !type.IsAbstract 
                    && !type.IsGenericTypeDefinition 
                    && targetType.IsAssignableFrom(type) 
                    && scopedProvider.GetService(type) is TInterface instance)
                    yield return instance;
            }
        }
    }


} // END OF CLASS TypeUtility 
