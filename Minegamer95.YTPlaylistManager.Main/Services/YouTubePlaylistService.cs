using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace Minegamer95.YTPlaylistManager.Main.Services;

public class YouTubePlaylistService : IPlaylistInteraction, IQuotaCost
{
  private readonly YouTubeService _youtubeService;
  private readonly string _applicationName;

  public YouTubePlaylistService(UserCredential credential, string applicationName = "My YouTube Playlist Manager")
  {
    _applicationName = applicationName;
    _youtubeService = new YouTubeService(new BaseClientService.Initializer()
    {
      HttpClientInitializer = credential,
      ApplicationName = _applicationName
    });
  }

  // Implementierung der Interface-Methoden mit echten API-Aufrufen
  public async Task<List<PlaylistItem>> ListItemsAsync(string playlistId)
  {
    var currentItems = new List<PlaylistItem>();
    string? nextPageToken = null;
    do
    {
      if (!QuotaManager.Instance.DeductIfAvailable(1))
      {
        return [];
      }
      
      var listRequest = _youtubeService.PlaylistItems.List("id,snippet");
      listRequest.PlaylistId = playlistId;
      listRequest.MaxResults = 50;
      listRequest.PageToken = nextPageToken;
      var listResponse = await listRequest.ExecuteAsync();
      if (listResponse.Items != null)
      {
        currentItems.AddRange(listResponse.Items.Where(item => item.Snippet?.ResourceId?.VideoId != null));
      }

      nextPageToken = listResponse.NextPageToken;
    } while (nextPageToken != null);

    return currentItems;
  }

  public async Task DeleteItemAsync(string playlistItemId)
  {
    if (!QuotaManager.Instance.DeductIfAvailable(50))
    {
      return;
    }

    await _youtubeService.PlaylistItems.Delete(playlistItemId).ExecuteAsync();
    await Task.Delay(50);
  }

  public async Task<PlaylistItem?> InsertItemAsync(PlaylistItem? itemToInsert)
  {
    if (!QuotaManager.Instance.DeductIfAvailable(50)) return null;
    var insertedItem = await _youtubeService.PlaylistItems.Insert(itemToInsert, "snippet").ExecuteAsync();
    await Task.Delay(50);
    return insertedItem; // Gibt das von der API zurückgegebene Item zurück
  }

  public async Task<PlaylistItem?> UpdateItemAsync(PlaylistItem? itemToUpdate)
  {
    if (!QuotaManager.Instance.DeductIfAvailable(50)) return null;
    var updatedItem = await _youtubeService.PlaylistItems.Update(itemToUpdate, "snippet").ExecuteAsync();
    await Task.Delay(50);
    return updatedItem; // Gibt das von der API zurückgegebene Item zurück
  }

  public int GetCost(QuotaType type)
  {
    return type switch
    {
      QuotaType.Get => 1,
      QuotaType.Insert => 50,
      QuotaType.Delete => 50,
      QuotaType.Update => 50,
      _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
  }
}