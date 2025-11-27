using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Libraries;
using Jellyfin.Plugin.MediaCleaner.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MediaCleaner.ScheduledTasks;

/// <summary>
/// A task to scan media for stale files.
/// </summary>
public sealed class StaleMediaTask : IScheduledTask
{
    private readonly ILogger _logger;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaleMediaTask"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="libraryManager">.</param>
    public StaleMediaTask(ILogger<StaleMediaTask> logger, IUserManager userManager, ILibraryManager libraryManager)
    {
        _logger = logger;
        _userManager = userManager;
        _libraryManager = libraryManager;
    }

    private static PluginConfiguration Configuration =>
            Plugin.Instance!.Configuration;

    string IScheduledTask.Name => "Scan Stale Media";

    string IScheduledTask.Key => "Stale Media";

    string IScheduledTask.Description => "Scan Stale Media";

    string IScheduledTask.Category => "Media";

    Task IScheduledTask.ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var query = new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
            Recursive = true
        };
        List<BaseItem> allItems = [.. _libraryManager.GetItemsResult(query).Items];

        _logger.LogInformation("Total items found: {AllItems}", allItems);

        List<BaseItem> shows = [.. allItems.Where(item => item.GetBaseItemKind() == BaseItemKind.Series)];
        List<BaseItem> movies = [.. allItems.Where(item => item.GetBaseItemKind() == BaseItemKind.Movie && item.UserData.Count > 0)];

        List<BaseItem> staleEpisodes = [.. shows.SelectMany(GetStaleEpisodes)];
        List<BaseItem> staleMovies = [.. GetStaleMovies(movies)];

        _logger.LogInformation("Stale Movies found: {StaleMovies}", staleMovies.Count);
        _logger.LogInformation("Stale Series found: {StaleShows}", staleEpisodes.Count);

        return Task.CompletedTask;
    }

    private List<BaseItem> GetStaleMovies(List<BaseItem> movies)
    {
        List<BaseItem> staleMovies = [];
        foreach (var movie in movies)
        {
            var mostRecentUserData = movie.UserData.OrderByDescending(data => data.LastPlayedDate).First();
            if (mostRecentUserData.LastPlayedDate < DateTime.Now.AddDays(-Configuration.StaleMediaCutoff))
            {
                staleMovies.Add(movie);
            }
        }

        return staleMovies;
    }

    private List<BaseItem> GetStaleEpisodes(BaseItem item)
    {
        List<BaseItem> staleEpisodes = [];

        // Gets each season in a show
        var seasons = _libraryManager.GetItemList(new InternalItemsQuery
        {
            ParentId = item.Id,
            Recursive = false
        });

        foreach (var season in seasons)
        {
            // Gets each episode, to access user data.
            var episodes = _libraryManager.GetItemList(new InternalItemsQuery
            {
                ParentId = season.Id,
                Recursive = false
            });

            bool seasonHasUserData = episodes.Any(episode => episode.UserData.Count > 0);

            if (seasonHasUserData)
            {
                var episodesWithUserData = episodes.Where(episode => episode.UserData.Count > 0).ToList();
                foreach (var episode in episodesWithUserData)
                {
                    bool stale = false;
                    var mostRecentUserData = episode.UserData.OrderByDescending(data => data.LastPlayedDate).First();
                    if (mostRecentUserData.LastPlayedDate < DateTime.Now.AddDays(-Configuration.StaleMediaCutoff))
                    {
                        staleEpisodes.AddRange(episodes);
                        stale = true;
                        break;
                    }

                    if (stale)
                    {
                        break;
                    }
                }
            }
        }

        return staleEpisodes;
    }

    IEnumerable<TaskTriggerInfo> IScheduledTask.GetDefaultTriggers()
    {
        // Run this task every 24 hours
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromHours(24).Ticks
        };
    }
}
