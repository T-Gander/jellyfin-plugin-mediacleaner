using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
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

    string IScheduledTask.Name => "Scan Stale Media";

    string IScheduledTask.Key => "Stale Media";

    string IScheduledTask.Description => "Scan Stale Media";

    string IScheduledTask.Category => "Media";

    Task IScheduledTask.ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var query = new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
            Recursive = true
        };
        var allItems = _libraryManager.GetItemsResult(query).Items;

        _logger.LogInformation("Total items found: {AllItems}", allItems);

        foreach (BaseItem item in allItems)
        {
            var userData = item.UserData.ToList();
            var mostRecentUserData = userData.OrderByDescending(data => data.LastPlayedDate).First();
            if (mostRecentUserData.LastPlayedDate < DateTime.Now.AddDays(1))
            {
                // Stale data
            }
        }

        Debugger.Break();

        return Task.CompletedTask;
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
