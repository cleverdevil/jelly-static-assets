using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.StaticAssets.Configuration;

namespace Jellyfin.Plugin.StaticAssets
{
    /// <summary>
    /// Simple logger helper class for when ILogManager isn't available
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Creates a logger for the specified type
        /// </summary>
        public static ILogger CreateLogger<T>()
        {
            return new ConsoleLogger<T>();
        }

        /// <summary>
        /// Simple console logger implementation
        /// </summary>
        private class ConsoleLogger<T> : ILogger
        {
            private readonly string _categoryName;

            public ConsoleLogger()
            {
                _categoryName = typeof(T).Name;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                try
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{logLevel}] [{_categoryName}] {formatter(state, exception)}");
                    if (exception != null)
                    {
                        Console.WriteLine($"Exception: {exception.Message}");
                        Console.WriteLine(exception.StackTrace);
                    }
                }
                catch
                {
                    // Just in case
                }
            }
        }
    }

    /// <summary>
    /// The main plugin class that defines our plugin
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        // Plugin instance for singleton pattern
        public static Plugin Instance { get; private set; }

        // Directory where assets will be stored
        public string AssetsDirectory { get; private set; }

        // Version with debugging info
        public const string VersionDebug = "0.0.1.0";

        // Logger instance
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor registers the plugin and sets up directories
        /// </summary>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;

            // Set up logging - use our simple logger implementation
            _logger = Logger.CreateLogger<Plugin>();

            // Load the resource paths right away for maximum visibility in logs
            Console.WriteLine($"StaticAssets plugin initializing, version {VersionDebug}");
            Console.WriteLine($"Plugin namespace: {GetType().Namespace}");

            // Log available resources
            var resourceNames = GetType().Assembly.GetManifestResourceNames();
            Console.WriteLine($"Available embedded resources: {resourceNames.Length}");
            foreach (var resource in resourceNames)
            {
                Console.WriteLine($"Resource found: {resource}");
            }

            // Create the assets directory within the plugin's data path
            AssetsDirectory = Path.Combine(applicationPaths.DataPath,
                                          "plugins", "StaticAssets", "uploads");

            Console.WriteLine($"Assets directory set to: {AssetsDirectory}");

            // Ensure the directory exists
            Directory.CreateDirectory(AssetsDirectory);
            Console.WriteLine($"Assets directory created: {Directory.Exists(AssetsDirectory)}");

            // Now use structured logging if available
            _logger.LogInformation("StaticAssets plugin initialized, version {Version}", VersionDebug);
            _logger.LogInformation("Plugin namespace: {Namespace}", GetType().Namespace);
            _logger.LogInformation("Assets directory: {Directory}", AssetsDirectory);
        }

        /// <summary>
        /// Plugin name display in the admin dashboard
        /// </summary>
        public override string Name => "Static Assets Manager";

        /// <summary>
        /// Plugin description
        /// </summary>
        public override string Description =>
            "Plugin to upload and host static resources";

        /// <summary>
        /// Get the plugin version
        /// </summary>
        public override Guid Id => Guid.Parse("1f826750-d8f1-4e44-a814-9432d2833dc0");

        /// <summary>
        /// Define web pages/routes for the plugin
        /// </summary>
        public IEnumerable<PluginPageInfo> GetPages()
        {
            // Get the namespace explicitly for logging and consistency
            var ns = GetType().Namespace;
            _logger.LogInformation("Building plugin pages, using namespace: {Namespace}", ns);
            Console.WriteLine($"Building plugin pages, using namespace: {ns}");

            // Verify that the resources exist
            var assembly = GetType().Assembly;
            var resourceNames = assembly.GetManifestResourceNames();

            var htmlPath = $"{ns}.Configuration.configPage.html";

            var hasHtml = resourceNames.Contains(htmlPath);

            Console.WriteLine($"Resource check - HTML: {htmlPath} exists: {hasHtml}");

            // Make route names more explicit and consistent
            var result = new[]
            {
                // The configuration page - make path very explicit with complete path
                new PluginPageInfo
                {
                    Name = "staticassets",
                    DisplayName = "Static Assets Manager",
                    EmbeddedResourcePath = htmlPath,
                    EnableInMainMenu = true,
                    MenuSection = "server",
                    MenuIcon = "photo"
                }
            };

            // Log each configured page for debugging
            foreach (var page in result)
            {
                Console.WriteLine($"Configured plugin page: Name={page.Name}, Path={page.EmbeddedResourcePath}, MenuEnabled={page.EnableInMainMenu}");
                _logger.LogInformation("Configured plugin page: Name={Name}, Path={Path}, MenuEnabled={MenuEnabled}",
                                      page.Name, page.EmbeddedResourcePath, page.EnableInMainMenu);
            }

            return result;
        }
    }
}


