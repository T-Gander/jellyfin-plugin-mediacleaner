using System.Collections.Generic;
using System.Collections.ObjectModel;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MediaCleaner.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
    }

    /// <summary>
    /// Gets or sets the api for your Radarr instance.
    /// </summary>
    public string RadarrAPIKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the api for your Sonarr instance.
    /// </summary>
    public string SonarrAPIKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cut off days before deleting unwatched files.
    /// </summary>
    public int StaleMediaCutoff { get; set; } = 90;
}
