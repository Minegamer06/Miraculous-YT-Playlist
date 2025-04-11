using Google.Apis.Auth.OAuth2;
using Minegamer95.YTPlaylistManager.Main.Model;

namespace Minegamer95.YTPlaylistManager.Main.Services;

public class ChannelVideoProvider : BaseVideoProvider
{
  private readonly PlaylistVideoProvider _playlistVideoProvider;
  public string ChannelId { get; set; }
  
  public ChannelVideoProvider(UserCredential credential, string applicationName, string channelId = "") : base(credential,
    applicationName)
  {
    ChannelId = channelId;
    _playlistVideoProvider = new PlaylistVideoProvider(credential, applicationName);
  }

  public override async Task<IEnumerable<VideoInfo>> GetVideos()
  {
    if (string.IsNullOrEmpty(ChannelId))
    {
      Console.WriteLine("Kanal ID ist nicht gesetzt. Bitte setzen Sie die Kanal ID.");
      return [];
    }
    
    Console.WriteLine($"\nRufe Videos von Kanal ID: {ChannelId} ab...");
    List<VideoInfo> allVideos = [];
    try
    {
      // Finde die Uploads-Playlist des Kanals
      var channelsRequest = _youtubeService.Channels.List("contentDetails");
      channelsRequest.Id = ChannelId;
      var channelsResponse = await channelsRequest.ExecuteAsync();

      if (channelsResponse.Items == null || channelsResponse.Items.Count == 0)
      {
        Console.WriteLine($"Kanal mit ID {ChannelId} nicht gefunden oder keine Details verfügbar.");
        return allVideos;
      }

      string uploadsPlaylistId = channelsResponse.Items[0].ContentDetails.RelatedPlaylists.Uploads;
      if (string.IsNullOrEmpty(uploadsPlaylistId))
      {
        Console.WriteLine($"Konnte Uploads-Playlist für Kanal {ChannelId} nicht finden.");
        return allVideos;
      }

      Console.WriteLine($"Uploads Playlist ID gefunden: {uploadsPlaylistId}");

      // Rufe Videos aus der Uploads-Playlist ab
      _playlistVideoProvider.PlaylistId = uploadsPlaylistId;
      allVideos = (await _playlistVideoProvider.GetVideos()).ToList();

    }
    catch (Exception ex)
    {
      await Console.Error.WriteLineAsync($"Fehler beim Abrufen der Videos vom Kanal {ChannelId}: {ex.Message}");
    }
    Console.WriteLine($"Abruf von Kanal {ChannelId} abgeschlossen. {allVideos.Count} Videos gefunden.");
    return allVideos;
  }
}