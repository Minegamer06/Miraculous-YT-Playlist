using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace Minegamer95.YTPlaylistManager.Main.Services.Actions;

public class UpdateAction : IPlaylistAction
{
  // Properties unverändert...
  public string PlaylistItemId { get; }
  public string VideoId { get; }
  public long TargetPosition { get; }
  public string PlaylistId { get; }

  public UpdateAction(PlaylistItem item, long targetPosition)
  {
    PlaylistItemId = item.Id ?? throw new ArgumentNullException(nameof(item.Id));
    VideoId = item.Snippet?.ResourceId?.VideoId ??
              throw new ArgumentNullException(nameof(item.Snippet.ResourceId.VideoId));
    PlaylistId = item.Snippet?.PlaylistId ?? throw new ArgumentNullException(nameof(item.Snippet.PlaylistId));
    TargetPosition = targetPosition;
  }

  // Akzeptiert IYouTubePlaylistService
  public async Task ExecuteAsync(IPlaylistInteraction service, bool dryRun)
  {
    // Erstellt das Objekt für den API-Call (unverändert)
    var playlistItemForUpdate = new PlaylistItem
    {
      Id = PlaylistItemId,
      Snippet = new PlaylistItemSnippet
      {
        PlaylistId = PlaylistId, Position = TargetPosition,
        ResourceId = new ResourceId { Kind = "youtube#video", VideoId = VideoId }
      }
    };

    if (dryRun)
    {
      Console.WriteLine(
        $"[DryRun] Würde Position von Item {PlaylistItemId} (Video: {VideoId}) auf {TargetPosition} setzen.");
      return;
    }

    try
    {
      // Ruft die Methode des Interfaces auf
      await service.UpdateItemAsync(playlistItemForUpdate);
    }
    catch (Exception ex)
    {
      await Console.Error.WriteLineAsync(
        $" Fehler beim Aktualisieren der Position von Item {PlaylistItemId} (Video: {VideoId}): {ex.Message}");
    }
  }

  public string Describe() =>
    $"Update: VideoId={VideoId}, TargetPosition={TargetPosition}, PlaylistItemId={PlaylistItemId}";

  public QuotaType GetCostType => QuotaType.Update;
}