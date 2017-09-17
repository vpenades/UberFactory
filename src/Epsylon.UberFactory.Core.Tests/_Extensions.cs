using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Epsylon.UberFactory
{
    static class _Extensions
    {
        #region paths

        public static string GetAbsolutePath(this string relativeFilePath)
        {
            var probeDir = Environment.CurrentDirectory;

            while (probeDir.Length > 3)
            {
                var absPath = System.IO.Path.Combine(probeDir, relativeFilePath);
                if (System.IO.File.Exists(absPath)) return absPath;

                probeDir = System.IO.Path.GetDirectoryName(probeDir);
            }

            return null;
        }

        #endregion

        #region DOM

        public static Guid AddRootNode(this ProjectDOM.Pipeline pipeline, string className)
        {
            var id = pipeline.AddNode(className);

            pipeline.RootIdentifier = id;

            return id;
        }

        public static ProjectDOM.Node CreateNode(this ProjectDOM.Pipeline pipeline,string className)
        {
            var id = pipeline.AddNode(className);

            return pipeline.GetNode(id);
        }

        public static ProjectDOM.Node CreateRootNode(this ProjectDOM.Pipeline pipeline, string className)
        {
            var id = pipeline.AddRootNode(className);

            return pipeline.GetNode(id);
        }

        public static void SetReferences(this ProjectDOM.Node parent, string jointCfg, string propertyName, params ProjectDOM.Node[] children)
        {
            var ids = children
                .Select(item => item.Identifier)
                .ToArray();

            var props = parent.GetPropertiesForConfiguration(jointCfg.Split('.'));
            props.SetReferenceIds("Value1", ids);
        }

        public static IEnumerable<Guid> GetReferenceIds(this ProjectDOM.Node parent, string jointCfg, string propertyName)
        {
            var props = parent.GetPropertiesForConfiguration(jointCfg.Split('.'));

            return props.GetReferenceIds(propertyName);
        }

        public static IEnumerable<ProjectDOM.Node> GetReferences(this ProjectDOM.Pipeline pipeline, ProjectDOM.Node parent, string jointCfg, string propertyName)
        {
            var ids = parent.GetReferenceIds(jointCfg, propertyName);

            return ids.Select(item => pipeline.GetNode(item));
        }

        #endregion

        #region assemblies

        public static Version Version(this Assembly assembly) { return assembly == null ? new Version() : assembly.GetName().Version; }

        private static T _GetCustomAttribute<T>(this Assembly assembly) where T : Attribute { return Attribute.GetCustomAttribute(assembly, typeof(T), false) as T; }

        public static string InfoCompany(this Assembly assembly) { return assembly._GetCustomAttribute<AssemblyCompanyAttribute>()?.Company; }

        public static string InfoProductName(this Assembly assembly) { return assembly._GetCustomAttribute<AssemblyProductAttribute>()?.Product; }

        public static string InformationalVersion(this Assembly assembly) { return assembly._GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion; }

        public static string InfoCopyright(this Assembly assembly) { return assembly._GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright; }

        public static string DisplayTitle(this Assembly assembly) { return assembly == null ? null : assembly.InfoProductName() + " " + assembly.Version().ToString(); }

        public static string GetDisplayTitle(this Assembly assembly, bool displayCompany, bool displayVersion, string currentDocument)
        {
            if (assembly == null) return null;
            var title = assembly.InfoProductName();

            if (displayCompany) title = assembly.InfoCompany() + " " + title;
            if (displayVersion) title = title + " " + assembly.Version().ToString();

            if (!string.IsNullOrWhiteSpace(currentDocument)) title = currentDocument.Trim() + " - " + title;

            return title;
        }

        public static string GetMetadata(this Assembly assembly, string key)
        {
            var attributes = Attribute.GetCustomAttributes(assembly, typeof(AssemblyMetadataAttribute), true);
            if (attributes == null) return null;

            return attributes.OfType<AssemblyMetadataAttribute>().FirstOrDefault(item => item.Key == key)?.Value;
        }

        public static bool IsLoaded(this System.Diagnostics.FileVersionInfo fvinfo)
        {
            return AppDomain
                    .CurrentDomain
                    .GetAssemblies()
                    .Any(a => string.Equals(a.Location, fvinfo.FileName, StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion

    }
}
