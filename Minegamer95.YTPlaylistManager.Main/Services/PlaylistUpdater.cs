using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Minegamer95.YTPlaylistManager.Main.Model;

namespace Minegamer95.YTPlaylistManager.Main.Services;

public class PlaylistUpdater
{
  private const string ApplicationName = "My YouTube Playlist Manager";
  private readonly YouTubeService _youtubeService;

  public PlaylistUpdater(UserCredential? credential)
  {
    _youtubeService = new YouTubeService(new BaseClientService.Initializer()
    {
      HttpClientInitializer = credential,
      ApplicationName = ApplicationName
    });
  }

  public async Task UpdateTargetPlaylistAsync(string targetPlaylistId, List<string> desiredVideoIds,
    bool dryRun = false)
  {
    Console.WriteLine($"\nAktualisiere Playlist ID: {targetPlaylistId}...");
    if (string.IsNullOrEmpty(targetPlaylistId))
    {
      await Console.Error.WriteLineAsync("Fehler: Keine Ziel-Playlist-ID angegeben.");
      return;
    }

    try
    {
      Console.WriteLine(" Schritt 1: Lese aktuelle Playlist-Elemente...");
      var currentItems = new List<PlaylistItem>();
      string? nextPageToken = null;
      do
      {
        var listRequest = _youtubeService.PlaylistItems.List("id,snippet");
        listRequest.PlaylistId = targetPlaylistId;
        listRequest.MaxResults = 50;
        listRequest.PageToken = nextPageToken;
        var listResponse = await listRequest.ExecuteAsync();

        if (listResponse.Items != null)
        {
          currentItems.AddRange(listResponse.Items);
        }

        nextPageToken = listResponse.NextPageToken;
      } while (nextPageToken != null);

      Console.WriteLine($" {currentItems.Count} Elemente in der Playlist gefunden.");

      var changePlan = CalculateChangePlan(currentItems, desiredVideoIds);

      Console.WriteLine(" Schritt 2: Lösche nicht benötigte Elemente...");
      foreach (var itemId in changePlan.ToDelete)
      {
        await DeletePlaylistItemAsync(itemId, dryRun);
      }

      Console.WriteLine(" Schritt 3: Aktualisiere Positionen...");
      foreach (var item in changePlan.ToUpdate)
      {
        await UpdatePlaylistItemPositionAsync(item, dryRun);
      }

      Console.WriteLine(" Schritt 4: Füge neue Videos hinzu...");
      foreach (var (id, pos) in changePlan.ToInsert)
      {
        await InsertVideoIntoPlaylistAsync(targetPlaylistId, id, pos, dryRun);
      }

      Console.WriteLine($"\nAktualisierung der Playlist {targetPlaylistId} abgeschlossen.");
    }
    catch (Exception ex)
    {
      await Console.Error.WriteLineAsync($"Fehler beim Aktualisieren der Playlist {targetPlaylistId}: {ex.Message}");
    }
  }

  private PlaylistChangePlan CalculateChangePlan(List<PlaylistItem> currentItems, List<string> desiredVideoIds)
  {
    var currentDict = currentItems.ToDictionary(item => item.Snippet.ResourceId.VideoId);

    var videosToDelete = new List<string>();
    var videosToUpdate = new List<PlaylistItem>();
    var videosToAdd = new List<(string videoId, uint Position)>();

    foreach (var item in currentItems)
    {
      var videoId = item.Snippet.ResourceId.VideoId;
      var position = item.Snippet.Position;
      
      if (!desiredVideoIds.Contains(videoId))
      {
        videosToDelete.Add(item.Id);
      }
      else if (desiredVideoIds.IndexOf(videoId) != position)
      {
        item.Snippet.Position = (uint)desiredVideoIds.IndexOf(videoId);
        videosToUpdate.Add(item);
      }
    }
    
    foreach (var id in desiredVideoIds)
    {
      if (!currentDict.ContainsKey(id))
      {
        videosToAdd.Add((id, (uint)desiredVideoIds.IndexOf(id)));
      }
    }
    return new PlaylistChangePlan(videosToDelete, videosToUpdate, videosToAdd);
  }

  private async Task UpdatePlaylistItemPositionAsync(PlaylistItem updateRequest, bool dryRun)
  {
    if (dryRun)
    {
      Console.WriteLine($"[DryRun] Würde Position von Item {updateRequest.Snippet?.ResourceId} - {updateRequest.Snippet?.Title} auf {updateRequest.Snippet?.Position} setzen.");
      return;
    }

    try
    {
      await _youtubeService.PlaylistItems.Update(updateRequest, "snippet").ExecuteAsync();
    }
    catch
    {
      await Console.Error.WriteLineAsync($" Fehler beim Aktualisieren der Position von Item {updateRequest.Snippet?.ResourceId} - {updateRequest.Snippet?.Title}");
    }
  }

  private async Task DeletePlaylistItemAsync(string itemId, bool dryRun)
  {
    if (dryRun)
    {
      Console.WriteLine($"[DryRun] Würde Item {itemId} löschen.");
      return;
    }

    try
    {
      await _youtubeService.PlaylistItems.Delete(itemId).ExecuteAsync();
    }
    catch (Exception ex)
    {
      await Console.Error.WriteLineAsync($" Fehler beim Löschen von Item {itemId}: {ex.Message}");
    }
  }

  private async Task InsertVideoIntoPlaylistAsync(string playlistId, string videoId, uint position, bool dryRun)
  {
    if (dryRun)
    {
      Console.WriteLine($"[DryRun] Würde Video {videoId} an Position {position} einfügen.");
      return;
    }

    var newPlaylistItem = new PlaylistItem
    {
      Snippet = new PlaylistItemSnippet
      {
        PlaylistId = playlistId,
        ResourceId = new ResourceId
        {
          Kind = "youtube#video",
          VideoId = videoId
        },
        Position = position
      }
    };

    try
    {
      await _youtubeService.PlaylistItems.Insert(newPlaylistItem, "snippet").ExecuteAsync();
    }
    catch (Exception exInsert)
    {
      await Console.Error.WriteLineAsync($" Fehler beim Hinzufügen von Video {videoId}: {exInsert.Message}");
    }
  }
}