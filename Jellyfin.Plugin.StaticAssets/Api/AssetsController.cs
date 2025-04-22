using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.StaticAssets.Api
{
    /// <summary>
    /// Controller for asset upload and retrieval
    /// </summary>
    [Route("StaticAssets")]
    [Microsoft.AspNetCore.Authorization.Authorize] // Requires authentication to access these endpoints
    public class AssetsController : ControllerBase
    {
        private readonly ILogger<AssetsController> _logger;

        public AssetsController(ILogger<AssetsController> logger)
        {
            _logger = logger;
            _logger.LogInformation("StaticAssets plugin controller initialized");
        }

        /// <summary>
        /// Get a list of all uploaded assets
        /// </summary>
        [HttpGet("list")]
        public ActionResult<IEnumerable<AssetInfo>> GetAssets()
        {
            var assetsDir = Jellyfin.Plugin.StaticAssets.Plugin.Instance.AssetsDirectory;
            var assets = new List<AssetInfo>();

            try
            {
                var files = Directory.GetFiles(assetsDir);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    assets.Add(new AssetInfo
                    {
                        Filename = Path.GetFileName(file),
                        Size = fileInfo.Length,
                        DateUploaded = fileInfo.CreationTime,
                        Url = $"/StaticAssets/asset/{Path.GetFileName(file)}"
                    });
                }

                return assets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing assets");
                return StatusCode(500, "Error retrieving assets");
            }
        }

        /// <summary>
        /// Upload a new asset file
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadAsset(IFormFile file)
        {
            _logger.LogInformation($"Upload attempt started. Has file: {file != null}");

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Upload failed: No file or empty file");
                return BadRequest("No file uploaded");
            }

            _logger.LogInformation($"Upload file info: Name={file.FileName}, Size={file.Length}, ContentType={file.ContentType}");

            // Check file extension for security
            var allowedExtensions = new[] {
                // Images
                ".jpg", ".jpeg", ".png", ".gif", ".svg", ".webp",
                // Videos
                ".mp4", ".webm", ".ogv", ".mov",
                // Web resources
                ".css", ".js", ".json", ".txt", ".html", ".htm"
            };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return BadRequest("File type not allowed");

            try
            {
                // Sanitize filename to prevent directory traversal attacks
                var fileName = Path.GetFileName(file.FileName);
                var filePath = Path.Combine(Jellyfin.Plugin.StaticAssets.Plugin.Instance.AssetsDirectory, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new {
                    message = "File uploaded successfully",
                    url = $"/StaticAssets/asset/{fileName}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                _logger.LogInformation($"Upload error details: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogInformation($"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, $"Error uploading file: {ex.Message}");
            }
        }

        /// <summary>
        /// Get an asset file by name
        /// </summary>
        [HttpGet("asset/{filename}")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous] // Assets can be accessed without authentication
        public IActionResult GetAsset(string filename)
        {
            // Sanitize the filename
            var sanitizedFilename = Path.GetFileName(filename);
            var filePath = Path.Combine(Jellyfin.Plugin.StaticAssets.Plugin.Instance.AssetsDirectory, sanitizedFilename);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            // Determine content type based on file extension
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = extension switch
            {
                // Images
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".webp" => "image/webp",

                // Videos
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".ogv" => "video/ogg",
                ".mov" => "video/quicktime",

                // Web resources
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".json" => "application/json",
                ".txt" => "text/plain",
                ".html" or ".htm" => "text/html",

                // Default
                _ => "application/octet-stream"
            };

            // Return the file
            return PhysicalFile(filePath, contentType);
        }

        /// <summary>
        /// Delete an asset
        /// </summary>
        [HttpDelete("asset/{filename}")]
        public IActionResult DeleteAsset(string filename)
        {
            // Sanitize the filename
            var sanitizedFilename = Path.GetFileName(filename);
            var filePath = Path.Combine(Jellyfin.Plugin.StaticAssets.Plugin.Instance.AssetsDirectory, sanitizedFilename);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            try
            {
                System.IO.File.Delete(filePath);
                return Ok(new { message = "File deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file");
                return StatusCode(500, "Error deleting file");
            }
        }
    }

    /// <summary>
    /// Model class for asset information
    /// </summary>
    public class AssetInfo
    {
        public string Filename { get; set; }
        public long Size { get; set; }
        public DateTime DateUploaded { get; set; }
        public string Url { get; set; }
    }
}
