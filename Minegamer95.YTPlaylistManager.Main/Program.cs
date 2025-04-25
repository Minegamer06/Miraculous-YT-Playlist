using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Google.Apis.YouTube.v3;
using Minegamer95.YTPlaylistManager.Main.Auth;
using Minegamer95.YTPlaylistManager.Main.Model;
using Minegamer95.YTPlaylistManager.Main.Services;
using Minegamer95.YTPlaylistManager.Main.Services.Extractors;

List<PlaylistTask>? LoadConfig()
{
  string configPath = Environment.GetEnvironmentVariable("YTPLAYLIST_CONFIG_PATH") ?? "Config/playlist_tasks.json";
  var file = new FileInfo(configPath);
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
  if (playlistTasks != null)
  {
    foreach (var task in playlistTasks)
    {
      if (task.PredefinedEpisodes is not null)
      {
        foreach (var vid in task.PredefinedEpisodes)
        {
          vid.VideoId = ParseYtUrl(vid.VideoId, true);
        }
      }
    }

    return playlistTasks;
  }

  return null;
}

string ParseYtUrl(string url, bool isSingleVideo = false)
{
  url = url.Trim();

  if (!url.Contains("youtube.com") && !url.Contains("youtu.be"))
    return url;

  try
  {
    var uri = new Uri(url);
    var query = HttpUtility.ParseQueryString(uri.Query);

    if (uri.Host.Contains("youtu.be"))
    {
      // Shortlink: Video-ID ist im Pfad
      return uri.AbsolutePath.Trim('/');
    }

    if (isSingleVideo)
    {
      return query.Get("v") ?? url; // Nicht "watch", sondern "v"
    }
    else
    {
      return query.Get("list") ?? url;
    }
  }
  catch (UriFormatException)
  {
    return url;
  }
  catch
  {
    return url;
  }
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
    Console.WriteLine($"Es wurden {playlistTasks.Count} Playlist-Tasks gefunden, {playlistTasks.Where(x => x.IsFinished)?.Count()} sind bereits als abgeschlossen makiert.");

    foreach (var task in playlistTasks)
    {
      Console.WriteLine($"Starte Playlist-Task: {task.Name}");
      if (await PrepareTask(task) is not null)
        await RunTask(task);
      Console.WriteLine("Task abgeschlossen.");
    }

    async Task<PlaylistTask?> PrepareTask(PlaylistTask task)
    {
      // Wir brauchen nichts zu tun, wenn die Playlist bereits fertig ist
      if (task.IsFinished)
        return null;
      // Die Ids der Playlist Extrahieren, falls sie als URL angegeben sind, leere Strings entfernen
      task.SourcePlaylistIds = task.SourcePlaylistIds?.Select(x => ParseYtUrl(x))
        .Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
      task.SourceChannelIds = task.SourceChannelIds?.Select(x => ParseYtUrl(x))
        .Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
      task.TargetPlaylistId = ParseYtUrl(task.TargetPlaylistId);

      if (string.IsNullOrEmpty(task.TargetPlaylistId))
      {
        Console.WriteLine("Fehler: Keine Ziel-Playlist-ID angegeben.");
        return null;
      }

      // Es wird sich möglicherweise etwas an der Playlist ändern, daher den Cache löschen
      if (playlists.ContainsKey(task.TargetPlaylistId))
      {
        playlists.Remove(task.TargetPlaylistId);
      }

      // Wir dürfen die Ziel-Playlist nicht als Quelle verwenden
      if (task.SourcePlaylistIds?.Contains(task.TargetPlaylistId) ?? false)
      {
        task.SourcePlaylistIds.Remove(task.TargetPlaylistId);
      }

      if (task.SourcePlaylistIds is not null)
      {
        foreach (var list in task.SourcePlaylistIds.Where(list => !playlists.ContainsKey(list)))
        {
          playlists.Add(list, await ytPlaylistProvider.GetVideos(list));
        }
      }

      if (task.SourceChannelIds is not null)
      {
        foreach (var list in task.SourceChannelIds.Where(list => !channels.ContainsKey(list)))
        {
          channels.Add(list, await ytChannelProvider.GetVideos(list));
        }
      }

      return task;
    }

    async Task RunTask(PlaylistTask task)
    {
      List<VideoInfo> videos = [];
      if (task.SourcePlaylistIds is not null)
      {
        foreach (var list in task.SourcePlaylistIds)
        {
          videos.AddRange(playlists[list]);
        }
      }

      if (task.SourceChannelIds is not null)
      {
        foreach (var list in task.SourceChannelIds)
        {
          videos.AddRange(channels[list]);
        }
      }

      videos = videos.DistinctBy(v => v.VideoId).ToList();
      if (videos.Count == 0 && task.PredefinedEpisodes is null)
        return;

      var episodeExtract = task.RegexPattern is null
        ? new RegexSeasonEpisodeExtractor()
        : new RegexSeasonEpisodeExtractor(task.RegexPattern);

      var episodes =
        episodeExtract.ExtractSeasonEpisodes(videos.Where(x =>
          task?.PredefinedEpisodes?.All(y => y.VideoId != x.VideoId) ?? true));

      if (task.PredefinedEpisodes is not null)
        episodes.AddRange(task.PredefinedEpisodes);

      episodes = episodes.Where(x => (task.Season is null || x.Season == task.Season) && x.Season is not null)
        .OrderBy(x => x.Season)
        .ThenBy(x => x.Episode).ToList();

      var target = episodes.Select(x => x.VideoId).ToList();
      await updater.UpdateTargetPlaylistAsync(task.TargetPlaylistId, target, false);
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
  Console.Error.WriteLine($"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}");
  Console.Error.WriteLine(ex);
}