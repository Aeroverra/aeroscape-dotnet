using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Services;

namespace AeroScape.Server.App.Services;

/// <summary>
/// Background service that periodically saves clan channel data to the database.
/// Mimics the Java SaveChats.java periodic save functionality.
/// </summary>
public sealed class ClanChatSaveService : BackgroundService
{
    private static readonly TimeSpan SaveInterval = TimeSpan.FromMinutes(1); // Save every minute like Java version

    private readonly ClanChatService _clanService;
    private readonly IClanChatPersistenceService _persistence;
    private readonly ILogger<ClanChatSaveService> _logger;

    public ClanChatSaveService(ClanChatService clanService, IClanChatPersistenceService persistence, ILogger<ClanChatSaveService> logger)
    {
        _clanService = clanService;
        _persistence = persistence;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SaveAllChannelsAsync();
                await Task.Delay(SaveInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic clan channel save");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Wait 30s before retrying
            }
        }
    }

    private async Task SaveAllChannelsAsync()
    {
        try
        {
            await _clanService.SaveAllChannelsAsync();
            _logger.LogDebug("Periodic clan channel save completed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save clan channels during periodic save");
        }
    }
}