using Google.Apis.Auth.OAuth2;
using Minegamer95.YTPlaylistManager.Main.Model;

namespace Minegamer95.YTPlaylistManager.Main.Services;

public class PlaylistVideoProvider : BaseVideoProvider
{
  public string PlaylistId {get; set;}

  public PlaylistVideoProvider(UserCredential credential, string applicationName, string playlistId = "") : base(credential,
    applicationName)
  {
    PlaylistId = playlistId;
  }

  public override async Task<IEnumerable<VideoInfo>> GetVideos()
  {
    Console.WriteLine($"\nRufe Videos von Playlist ID: {PlaylistId} ab...");
    List<VideoInfo> allVideos = [];
    string? nextPageToken = null;

    try
    {
      do
      {
        if (!QuotaManager.Instance.DeductIfAvailable(1))
        {
          return Enumerable.Empty<VideoInfo>();
        }
        
        var playlistItemsRequest = _youtubeService.PlaylistItems.List("snippet,contentDetails");
        playlistItemsRequest.PlaylistId = PlaylistId;
        playlistItemsRequest.MaxResults = 50; // Maximalwert
        playlistItemsRequest.PageToken = nextPageToken;

        var playlistItemsResponse = await playlistItemsRequest.ExecuteAsync();

        if (playlistItemsResponse.Items != null)
        {
          foreach (var item in playlistItemsResponse.Items)
          {
            // Überspringe Videos, die nicht verfügbar sind oder gelöscht wurden
            if (item.Snippet.Title == "Private video" || item.Snippet.Title == "Deleted video") continue;
            if (item.ContentDetails == null || string.IsNullOrEmpty(item.ContentDetails.VideoId)) continue;


            var videoInfo = new VideoInfo
            {
              VideoId = item.ContentDetails.VideoId,
              Title = item.Snippet.Title,
              Description = item.Snippet.Description,
              PublishedAt = item.ContentDetails.VideoPublishedAtDateTimeOffset?.DateTime
            };
            allVideos.Add(videoInfo);
          }
        }

        nextPageToken = playlistItemsResponse.NextPageToken;
      } while (nextPageToken != null);
    }
    catch (Exception ex)
    {
      await Console.Error.WriteLineAsync($"Fehler beim Abrufen der Videos von Playlist {PlaylistId}: {ex.Message}");
    }

    Console.WriteLine($"Abruf von Playlist {PlaylistId} abgeschlossen. {allVideos.Count} Videos gefunden.");
    return allVideos;
  }
}