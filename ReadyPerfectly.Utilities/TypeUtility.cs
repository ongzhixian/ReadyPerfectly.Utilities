using System.Reflection;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace ReadyPerfectly.Extensions;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class TypeUtility
{
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


} // END OF CLASS TypeUtility 
