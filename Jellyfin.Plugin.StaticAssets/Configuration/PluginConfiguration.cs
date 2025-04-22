using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.StaticAssets.Configuration
{
    /// <summary>
    /// Plugin configuration options
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// The maximum size in bytes for uploaded files (default: 10MB)
        /// </summary>
        public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

        /// <summary>
        /// Whether to allow non-admin users to upload assets
        /// </summary>
        public bool AllowNonAdminUploads { get; set; } = false;

        /// <summary>
        /// Constructor with default settings
        /// </summary>
        public PluginConfiguration()
        {
            // Default configuration options
        }
    }
}


