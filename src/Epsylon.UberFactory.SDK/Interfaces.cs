using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory
{
    public static partial class SDK
    {
        public static string InformationalVersion
        {
            get
            {
                var assembly = typeof(SDK).GetTypeInfo().Assembly;

                var attribute = assembly
                    .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
                    .FirstOrDefault();

                return attribute?.InformationalVersion;
            }
        }

        public interface IMonitorContext : IProgress<float>
        {
            /// <summary>
            /// True when host has requested operation cancellation
            /// </summary>
            /// <remarks>
            /// When the called checks this property is true, it must throw a <see cref="OperationCanceledException"/> 
            /// </remarks>
            bool IsCancelRequested { get; }
        }

        public interface ILogger
        { 
            /// <summary>
            /// Writes a Trace message to the log event system
            /// </summary>
            /// <param name="categoryName">category name</param>
            /// <param name="message">message</param>
            void LogTrace(string categoryName, string message);

            /// <summary>
            /// Writes a Debug message to the log event system
            /// </summary>
            /// <param name="categoryName">category name</param>
            /// <param name="message">message</param>
            void LogDebug(string categoryName, string message);

            /// <summary>
            /// Writes a Info message to the log event system
            /// </summary>
            /// <param name="categoryName">category name</param>
            /// <param name="message">message</param>
            void LogInfo(string categoryName, string message);

            /// <summary>
            /// Writes a Warning message to the log event system
            /// </summary>
            /// <param name="categoryName">category name</param>
            /// <param name="message">message</param>
            void LogWarning(string categoryName, string message);

            /// <summary>
            /// Writes a Error message to the log event system
            /// </summary>
            /// <param name="categoryName">category name</param>
            /// <param name="message">message</param>
            void LogError(string categoryName, string message);

            /// <summary>
            /// Writes a Critical message to the log event system
            /// </summary>
            /// <param name="categoryName">category name</param>
            /// <param name="message">message</param>
            void LogCritical(string categoryName, string message);
        }

        /// <summary>
        /// Tells the evaluator which child files depend on which parent files
        /// </summary>
        public interface IFileTracker
        {
            void RegisterInputFile(string filePath, string parentFilePath);
            void RegisterOutputFile(string filePath, string parentFilePath);
        }

        /// <summary>
        /// The file manager provides a number of services to the contet filters
        /// </summary>
        /// <remarks>
        /// Content Filters use the build context to:
        /// - convert paths from/to relative/absolute
        /// - Get a context to read source files
        /// - Get a context to write product files
        /// - Write to the log
        /// </remarks>
        public interface IFileManager
        {
            /// <summary>
            /// Converts an absolute path to a path relative to Context's Source path
            /// </summary>
            /// <param name="absolutePath">An absolute path</param>
            /// <returns>A relative path</returns>            
            String GetRelativeToSource(String absolutePath);

            /// <summary>
            /// Converts an absolute path to a path relative to Context's Target path
            /// </summary>
            /// <param name="absolutePath">An absolute path</param>
            /// <returns>A relative path</returns>            
            String GetRelativeToTarget(String absolutePath);

            /// <summary>
            /// Converts a path relative to Context's Source path to an absolute path
            /// </summary>
            /// <param name="relativePath">A path relative to Context's Source</param>
            /// <returns>An absolute path</returns>            
            String GetSourceAbsolutePath(String relativePath);

            /// <summary>
            /// Converts a path relative to Context's Target path to an absolute path
            /// </summary>
            /// <param name="relativePath">A path relative to Context's Target</param>
            /// <returns>An absolute path</returns>            
            String GetTargetAbsolutePath(String relativePath);

            /// <summary>
            /// Creates an inport context to read files from the given location
            /// </summary>
            /// <param name="absolutePath">A path to the location</param>
            /// <returns>An import context</returns>
            ImportContext GetImportContext(String absolutePath);

            /// <summary>
            /// Creates an Export context to write files to the given location
            /// </summary>
            /// <param name="absolutePath">A path to the location</param>
            /// <returns>An export context</returns>
            ExportContext GetExportContext(String absolutePath);

            
            IEnumerable<ImportContext> GetImportContextBatch(String absolutePath, String fileMask, bool allDirectories);

            /// <summary>
            /// Gets a preview context to 
            /// </summary>            
            /// <returns>A preview context</returns>
            PreviewContext GetPreviewContext();
        }       
        
    }

}
