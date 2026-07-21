using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace BlazorWebAppWasmDemo.services
{
    /// <summary>
    /// Service to extract all @page route templates from all loaded assemblies.
    /// </summary>
    public class RouteInfoService
    {
        public List<string> Routes { get; }

        public RouteInfoService()
        {
            Routes = GetAllPageRoutes();
        }

        private List<string> GetAllPageRoutes()
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

            return routeTemplates;
        }
    }
}
