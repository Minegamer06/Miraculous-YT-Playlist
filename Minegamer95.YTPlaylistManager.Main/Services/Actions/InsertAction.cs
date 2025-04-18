using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace Minegamer95.YTPlaylistManager.Main.Services.Actions;

public class InsertAction : IPlaylistAction
{
  // Properties unverändert...
  public string VideoId { get; }
  public long TargetPosition { get; }
  public string PlaylistId { get; }

  public InsertAction(string videoId, long targetPosition, string playlistId)
  {
    VideoId = videoId;
    TargetPosition = targetPosition;
    PlaylistId = playlistId;
  }

  // Akzeptiert IYouTubePlaylistService
  public async Task ExecuteAsync(IPlaylistInteraction service, bool dryRun)
  {
    // Erstellt das Objekt für den API-Call (unverändert)
    var newPlaylistItem = new PlaylistItem
    {
      Snippet = new PlaylistItemSnippet
      {
        PlaylistId = PlaylistId, ResourceId = new ResourceId { Kind = "youtube#video", VideoId = VideoId },
        Position = TargetPosition
      }
    };

    if (dryRun)
    {
      Console.WriteLine(
        $"[DryRun] Würde Video {VideoId} an Position {TargetPosition} in Playlist {PlaylistId} einfügen.");
      return;
    }

    try
    {
      // Ruft die Methode des Interfaces auf
      await service.InsertItemAsync(newPlaylistItem);
    }
    catch (Exception exInsert)
    {
      await Console.Error.WriteLineAsync($" Fehler beim Hinzufügen von Video {VideoId}: {exInsert.Message}");
    }
  }

  public string Describe() => $"Insert: VideoId={VideoId}, TargetPosition={TargetPosition}, PlaylistId={PlaylistId}";
  public QuotaType GetCostType => QuotaType.Insert;
}