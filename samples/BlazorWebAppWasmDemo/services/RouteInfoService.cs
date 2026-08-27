using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace BlazorWebAppWasmDemo.services
{
    /// <summary>
    /// Service to extract all @page route templates from all loaded assemblies.
    /// </summary>
    public class RouteInfoService
    {
        public List<string> RoutesTemplates { get; }
        public List<RouteAttribute> Routes { get; }
        public IReadOnlyList<Type> PageTypes { get; }

        public RouteInfoService()
        {
            RoutesTemplates = GetAllPageRoutesTemplates();
            Routes = GetAllPageRoutes();
            PageTypes = GetAllPageTypes();
        }

        private List<string> GetAllPageRoutesTemplates()
        {

            // Get all assemblies that might contain Blazor components
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.FullName))
                .ToList();

            //assemblies = assemblies
            //.Where(a => a.GetName().Name.StartsWith("MyApp") || a.GetName().Name.StartsWith("MyLib"))
            //.ToList();

            var routeTemplates = assemblies
                .SelectMany(a =>
                {
                    try
                    {
                        return a.ExportedTypes;
                    }
                    catch
                    {
                        return Array.Empty<Type>(); // Skip problematic assemblies
                    }
                })
                .Where(t => typeof(IComponent).IsAssignableFrom(t))
                .SelectMany(t => t.GetCustomAttributes<RouteAttribute>(inherit: false))
                .Select(attr => attr.Template)
                .Distinct()
                .OrderBy(r => r)
                .ToList();

            var routes = assemblies
                .SelectMany(a =>
                {
                    try
                    {
                        return a.ExportedTypes;
                    }
                    catch
                    {
                        return Array.Empty<Type>(); // Skip problematic assemblies
                    }
                })
                .Where(t => typeof(IComponent).IsAssignableFrom(t))
                .SelectMany(t => t.GetCustomAttributes<RouteAttribute>(inherit: false))
                //.Select(attr => attr.Template)
                //.Distinct()
                //.OrderBy(r => r)
                .ToList();

            return routeTemplates;
        }

        private List<RouteAttribute> GetAllPageRoutes()
        {

            // Get all assemblies that might contain Blazor components
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.FullName))
                .ToList();

            //assemblies = assemblies
            //.Where(a => a.GetName().Name.StartsWith("MyApp") || a.GetName().Name.StartsWith("MyLib"))
            //.ToList();


            var routes = assemblies
                .SelectMany(a =>
                {
                    try
                    {
                        return a.ExportedTypes;
                    }
                    catch
                    {
                        return Array.Empty<Type>(); // Skip problematic assemblies
                    }
                })
                .Where(t => typeof(IComponent).IsAssignableFrom(t))
                .SelectMany(t => t.GetCustomAttributes<RouteAttribute>(inherit: false))
                //.Select(attr => attr.Template)
                //.Distinct()
                //.OrderBy(r => r)
                .ToList();

            return routes;
        }

        /// <summary>
        /// Gets all routable Blazor page component types from the given assembly.
        /// </summary>
        public IReadOnlyList<Type> GetAllPageTypes()
        {            // Get all assemblies that might contain Blazor components
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.FullName))
                .ToList();

            var pageTypes = assemblies
                .SelectMany(a => a.ExportedTypes)
                .Where(t =>
                    typeof(ComponentBase).IsAssignableFrom(t) && // Must be a Blazor component
                    t.GetCustomAttributes<RouteAttribute>(inherit: true).Any() // Must have @page
                )
                .ToList();

            return pageTypes;
        }

    }
}
