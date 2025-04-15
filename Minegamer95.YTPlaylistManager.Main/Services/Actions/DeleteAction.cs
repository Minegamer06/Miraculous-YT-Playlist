using Google.Apis.YouTube.v3.Data;

namespace Minegamer95.YTPlaylistManager.Main.Services.Actions;

public class DeleteAction : IPlaylistAction
{
  public string PlaylistItemId { get; }
  public string VideoId { get; }
  public long OriginalPosition { get; }

  public DeleteAction(PlaylistItem item)
  {
    PlaylistItemId = item.Id ?? throw new ArgumentNullException(nameof(item.Id));
    VideoId = item.Snippet?.ResourceId?.VideoId ?? "Unbekannt";
    OriginalPosition = item.Snippet?.Position ?? -1;
  }

  // Akzeptiert IYouTubePlaylistService
  public async Task ExecuteAsync(IPlaylistInteraction service, bool dryRun)
  {
    if (dryRun)
    {
      Console.WriteLine(
        $"[DryRun] Würde Item {PlaylistItemId} (Video: {VideoId}, Urspr. Pos: {OriginalPosition}) löschen.");
      return;
    }

    try
    {
      Console.WriteLine($"SERVICE CALL: Delete PlaylistItemId={PlaylistItemId} (Video: {VideoId})");
      // Ruft die Methode des Interfaces auf
      await service.DeleteItemAsync(PlaylistItemId);
    }
    catch (Exception ex)
    {
      await Console.Error.WriteLineAsync($" Fehler beim Löschen von Item {PlaylistItemId}: {ex.Message}");
    }
  }

  public string Describe() =>
    $"Delete: VideoId={VideoId}, OriginalPosition={OriginalPosition}, PlaylistItemId={PlaylistItemId}";

  public QuotaType GetCostType => QuotaType.Delete;
}