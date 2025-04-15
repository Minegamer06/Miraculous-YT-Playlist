using Google.Apis.YouTube.v3;
using Minegamer95.YTPlaylistManager.Main.Auth;
using Minegamer95.YTPlaylistManager.Main.Model;
using Minegamer95.YTPlaylistManager.Main.Services;
using Minegamer95.YTPlaylistManager.Main.Services.Extractors;

// Pfad zur client_secret.json Datei
const string ClientSecretsPath =
  "M:\\PC\\Projects\\Miraculous-YT-Playlist\\Minegamer95.YTPlaylistManager.Main\\client_secret.json";
// Benötigte Scopes für die YouTube API
string[] Scopes = { YouTubeService.Scope.Youtube };
const string programName = "My YouTube Playlist Manager";

Console.WriteLine("YouTube Playlist Manager");
Console.WriteLine("======================================================\n");

// --- Konfiguration ---
// ID des Kanals, von dem Videos geholt werden sollen
const string sourceChannelId = ""; //"UC2D5z27XfUz5DfU7kQAehiA";
// ID der Playlist, von der Videos geholt werden sollen
const string sourcePlaylistId = "PLDGA85Y1JsmNIo_mKcsWvzKAFRPah6mqT";
const string targetPlaylistIdSeason1 = "PLQg5Jd-VCfKAqwgaRkQo4Ngct4TeKi0-O";

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
    var ytChannels = new List<IVideoProvider>();
    var ytChannel = new ChannelVideoProvider(credential, programName, sourceChannelId);
    var ytPlaylist = new PlaylistVideoProvider(credential, programName, sourcePlaylistId);
    var playlists = new Dictionary<string, List<VideoInfo>>();
    var channels = new Dictionary<string, List<VideoInfo>>();
    var playlistTasks = new List<PlaylistTask>
    {
      new(){
        TargetPlaylistId = "PLDGA85Y1JsmNIo_mKcsWvzKAFRPah6mqT",
        SourcePlaylistIds = ["PLQg5Jd-VCfKAqwgaRkQo4Ngct4TeKi0-O"],
        Season = 1,
      }
    };

    foreach (var task in playlistTasks)
    {
      
    }
    
    // --- Videos sammeln ---
    List<VideoInfo> allAvailableVideos = []; // Verwende deine VideoInfo-Klasse

    // Beispiel: Videos vom Kanal holen
    if (!string.IsNullOrEmpty(sourceChannelId)) // Prüfe auf Standardwert
    {
      var channelVideos = await ytChannel.GetVideos();
      allAvailableVideos.AddRange(channelVideos);
    }

    // Beispiel: Videos aus einer Quell-Playlist holen
    if (!string.IsNullOrEmpty(sourcePlaylistId)) // Prüfe auf Standardwert
    {
      var playlistVideos = await ytPlaylist.GetVideos();
      // Optional: Doppelte Einträge vermeiden
      allAvailableVideos.AddRange(playlistVideos);
      allAvailableVideos = allAvailableVideos.GroupBy(v => v.VideoId).Select(g => g.First()).ToList();
    }

    var episodeConverter = new RegexSeasonEpisodeExtractor();

    var episodes = episodeConverter.ExtractSeasonEpisodes(allAvailableVideos)
      .OrderBy(x => x.Season)
      .ThenBy(x => x.Episode).ToList();
    
    Console.WriteLine($"Videos:\n\t{
      string.Join("\n\t", episodes.Where(x => x.Season is not null)
        .Select(x => $"Staffel {x.Season}, Episode {x.Episode}, NAME: {x.Title} ({x.VideoId})"))
    }");
    Console.WriteLine($"\nInsgesamt {allAvailableVideos.Count} einzigartige Videos gesammelt.");

    // --- Staffeln verarbeiten ---

    // Beispiel für Staffel 1
    uint seasonToUpdate = 1;
    if (!string.IsNullOrEmpty(targetPlaylistIdSeason1)) // Prüfe auf Standardwert
    {
      Console.WriteLine($"\nVerarbeite Staffel {seasonToUpdate} für Playlist {targetPlaylistIdSeason1}...");
      List<string> season1VideoIds = episodes.Where(x => x.Season == 1).Select(x => x.VideoId).ToList();
      // Stelle sicher, dass UpdateTargetPlaylistAsync existiert
      await updater.UpdateTargetPlaylistAsync(targetPlaylistIdSeason1, season1VideoIds);
    }
    else
    {
      Console.WriteLine(
        $"\nÜberspringe Staffel {seasonToUpdate}, da keine gültige Ziel-Playlist ID ('{targetPlaylistIdSeason1}') angegeben wurde.");
    }

    // Füge hier Logik für weitere Staffeln hinzu, falls targetPlaylistId_Season2 etc. gesetzt sind...


    Console.WriteLine("\nAlle Aktionen abgeschlossen.");
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