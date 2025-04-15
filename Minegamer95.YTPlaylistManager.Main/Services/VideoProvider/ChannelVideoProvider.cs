using Google.Apis.Auth.OAuth2;
using Minegamer95.YTPlaylistManager.Main.Model;

namespace Minegamer95.YTPlaylistManager.Main.Services;

public class ChannelVideoProvider : BaseVideoProvider
{
  private readonly PlaylistVideoProvider _playlistVideoProvider;
  
  public ChannelVideoProvider(UserCredential credential, string applicationName) : base(credential,
    applicationName)
  {
    _playlistVideoProvider = new PlaylistVideoProvider(credential, applicationName);
  }

  public override async Task<List<VideoInfo>> GetVideos(string channelId)
  {
    if (!QuotaManager.Instance.DeductIfAvailable(1))
    {
      return [];
    }
    
    if (string.IsNullOrEmpty(channelId))
    {
      Console.WriteLine("Kanal ID ist nicht gesetzt. Bitte setzen Sie die Kanal ID.");
      return [];
    }

    
    Console.WriteLine($"\nRufe Videos von Kanal ID: {channelId} ab...");
    List<VideoInfo> allVideos = [];
    try
    {
      // Finde die Uploads-Playlist des Kanals
      var channelsRequest = _youtubeService.Channels.List("contentDetails");
      channelsRequest.Id = channelId;
      var channelsResponse = await channelsRequest.ExecuteAsync();

      if (channelsResponse.Items == null || channelsResponse.Items.Count == 0)
      {
        Console.WriteLine($"Kanal mit ID {channelId} nicht gefunden oder keine Details verfügbar.");
        return allVideos;
      }

      string uploadsPlaylistId = channelsResponse.Items[0].ContentDetails.RelatedPlaylists.Uploads;
      if (string.IsNullOrEmpty(uploadsPlaylistId))
      {
        Console.WriteLine($"Konnte Uploads-Playlist für Kanal {channelId} nicht finden.");
        return allVideos;
      }

      Console.WriteLine($"Uploads Playlist ID gefunden: {uploadsPlaylistId}");

      // Rufe Videos aus der Uploads-Playlist ab
      allVideos = (await _playlistVideoProvider.GetVideos(uploadsPlaylistId)).ToList();

    }
    catch
    {
      await Console.Error.WriteLineAsync($"Fehler beim Abrufen der Videos vom Kanal {channelId}");
    }
    Console.WriteLine($"Abruf von Kanal {channelId} abgeschlossen. {allVideos.Count} Videos gefunden.");
    return allVideos;
  }
}