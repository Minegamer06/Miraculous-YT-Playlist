using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Apis.YouTube.v3;
using Minegamer95.YTPlaylistManager.Main.Auth;
using Minegamer95.YTPlaylistManager.Main.Model;
using Minegamer95.YTPlaylistManager.Main.Services;
using Minegamer95.YTPlaylistManager.Main.Services.Extractors;

List<PlaylistTask>? LoadConfig()
{
  var file = new FileInfo("Config/playlist_task.json");
  if (!file.Exists)
  {
    Console.WriteLine($"Fehler: Die Konfigurationsdatei '{file.FullName}' wurde nicht gefunden.");
    return null;
  }
  
  using var stream = file.OpenRead();
  var playlistTasks = JsonSerializer.Deserialize<List<PlaylistTask>>(stream,
    new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true,
      Converters =
      {
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
      }
    });
  return playlistTasks;
}
// Pfad zur client_secret.json Datei
const string ClientSecretsPath =
  "M:\\PC\\Projects\\Miraculous-YT-Playlist\\Minegamer95.YTPlaylistManager.Main\\client_secret.json";
// Benötigte Scopes für die YouTube API
string[] Scopes = { YouTubeService.Scope.Youtube };
const string programName = "My YouTube Playlist Manager";

Console.WriteLine("YouTube Playlist Manager");
Console.WriteLine("======================================================\n");


try
{
  var credential =
    await AuthHelper.AuthenticateBasedOnEnvironmentAsync(ClientSecretsPath, Scopes, CancellationToken.None);

  if (credential != null)
  {
    Console.WriteLine("\nAuthentifizierung erfolgreich abgeschlossen. Starte Playlist-Updater...");

    // --- Updater-Instanz erstellen ---
    IPlaylistInteraction playlistService = new YouTubePlaylistService(credential, programName);
    var updater = new PlaylistUpdater(playlistService); // Verwende deine PlaylistUpdater-Klasse
    var ytChannelProvider = new ChannelVideoProvider(credential, programName);
    var ytPlaylistProvider = new PlaylistVideoProvider(credential, programName);
    var playlists = new Dictionary<string, List<VideoInfo>>();
    var channels = new Dictionary<string, List<VideoInfo>>();
    var playlistTasks = LoadConfig();
    
    if (playlistTasks is null)
    {
      Console.WriteLine("Fehler: Keine Playlist-Tasks gefunden.");
      return;
    }

    foreach (var task in playlistTasks)
    {
      if (string.IsNullOrEmpty(task.TargetPlaylistId))
      {
        Console.WriteLine("Fehler: Keine Ziel-Playlist-ID angegeben.");
        continue;
      }
      else if (playlists.ContainsKey(task.TargetPlaylistId))
      {
        playlists.Remove(task.TargetPlaylistId);
      }

      if (task.SourcePlaylistIds is not null)
      {
        foreach (var list in task.SourcePlaylistIds)
        {
          if (!string.IsNullOrEmpty(list) && list != task.TargetPlaylistId && !playlists.ContainsKey(list))
          {
            playlists.Add(list, await ytPlaylistProvider.GetVideos(list));
          }
        }
      }

      if (task.SourceChannelIds is not null)
      {
        foreach (var list in task.SourceChannelIds)
        {
          if (!string.IsNullOrEmpty(list) && !channels.ContainsKey(list))
          {
            channels.Add(list, await ytChannelProvider.GetVideos(list));
          }
        }
      }

      await RunTask(task);
    }
    
    async Task RunTask(PlaylistTask task)
    {
      List<VideoInfo> videos = [];
      if (task.SourcePlaylistIds is not null)
      {
        foreach (var list in task.SourcePlaylistIds)
        {
          if (!string.IsNullOrEmpty(list) && list != task.TargetPlaylistId &&
              playlists.TryGetValue(list, out var plVideos))
          {
            videos.AddRange(plVideos);
          }
        }
      }

      if (task.SourceChannelIds is not null)
      {
        foreach (var list in task.SourceChannelIds)
        {
          if (!string.IsNullOrEmpty(list) && channels.TryGetValue(list, out var chVideos))
          {
            videos.AddRange(chVideos);
          }
        }
      }

      videos = videos.DistinctBy(v => v.VideoId).ToList();
      if (videos.Count == 0)
        return;
      var episodeExtract = task.RegexPattern is null
        ? new RegexSeasonEpisodeExtractor()
        : new RegexSeasonEpisodeExtractor(task.RegexPattern);
      var episodes = episodeExtract.ExtractSeasonEpisodes(videos);
      episodes = episodes.Where(x => task.Season is null || x.Season == task.Season)
        .OrderBy(x => x.Season)
        .ThenBy(x => x.Episode).ToList();
      await updater.UpdateTargetPlaylistAsync(task.TargetPlaylistId, episodes.Select(x => x.VideoId).ToList());
    }
  }
  else
  {
    Console.Error.WriteLine(
      "\nKonnte keine gültigen Anmeldeinformationen erhalten. Playlist-Update wird nicht gestartet.");
  }
}
catch (Exception ex)
{
  Console.Error.WriteLine($"Ein unerwarteter Fehler ist aufgetreten");
}