using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace Demos.Shared.Services
{
    /// <summary>
    /// Extracts every @page route template, RouteAttribute and routable page type from the loaded
    /// assemblies.
    /// <para>
    /// This exists to support <c>BlazorAtoms.Breadcrumbs</c>. That library builds its parent-route
    /// graph by scanning <c>AppDomain.CurrentDomain.GetAssemblies()</c> for components carrying
    /// <c>[Route]</c> and <c>[AtomBreadcrumb]</c> (see <c>AtomBreadcrumbGraph</c>), so a trail is
    /// only ever as correct as what that scan can see. This service is the read-out for the same
    /// view of the world: if a route is missing from the /RouteInfo page, Breadcrumbs cannot resolve
    /// it either.
    /// </para>
    /// <para>
    /// That makes it the practical check for the trimming risk recorded in
    /// <c>src/BlazorAtoms.Breadcrumbs/breadcrumbplan.md</c> — assembly scanning has known
    /// AOT/trimming caveats, and a published, trimmed WebAssembly build is where they would first
    /// bite. Hence a page in each demo host, including Auto, whose two runtimes each scan their own
    /// AppDomain.
    /// </para>
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
                .Where(t =>
                    typeof(ComponentBase).IsAssignableFrom(t) && // Must be a Blazor component
                    t.GetCustomAttributes<RouteAttribute>(inherit: true).Any() // Must have @page
                )
                .ToList();

            return pageTypes;
        }

    }
}
