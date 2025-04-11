using Google.Apis.YouTube.v3;
using Minegamer95.YTPlaylistManager.Main.Auth;
using Minegamer95.YTPlaylistManager.Main.Model; 
using Minegamer95.YTPlaylistManager.Main.Services;
using Minegamer95.YTPlaylistManager.Main.Services.Extractors;

// Definiere Konstanten direkt im Top-Level-Bereich
const string ClientSecretsPath =
  "M:\\PC\\Projects\\Miraculous-YT-Playlist\\Minegamer95.YTPlaylistManager.Main\\client_secret.json"; // Dein Pfad
// Stelle sicher, dass die Scopes-Variable korrekt definiert ist
string[] Scopes = { YouTubeService.Scope.Youtube }; // Youtube reicht auch

Console.WriteLine("YouTube Playlist Manager - Einmalige lokale Autorisierung");
Console.WriteLine("======================================================\n");

// --- Konfiguration ---
// ID des Kanals, von dem Videos geholt werden sollen
const string sourceChannelId = "UC2D5z27XfUz5DfU7kQAehiA";
// ID der Playlist, von der Videos geholt werden sollen
const string sourcePlaylistId = "PLDGA85Y1JsmNIo_mKcsWvzKAFRPah6mqT";
string targetPlaylistId_Season1 = "PL_TARGET_SEASON_1"; // ID der Ziel-Playlist für Staffel 1 // <-- ANPASSEN!

try
{
  var credential =
    await AuthHelper.AuthenticateBasedOnEnvironmentAsync(ClientSecretsPath, Scopes, CancellationToken.None);

  if (credential != null)
  {
    Console.WriteLine("\nAuthentifizierung erfolgreich abgeschlossen. Starte Playlist-Updater...");

    // --- Updater-Instanz erstellen ---
    // Stelle sicher, dass PlaylistUpdater einen Konstruktor hat, der UserCredential akzeptiert
    var updater = new PlaylistUpdater(credential); // Verwende deine PlaylistUpdater-Klasse
    var ytChannel = new ChannelVideoProvider(credential, "My YouTube Playlist Manager", sourceChannelId);
    var ytPlaylist = new PlaylistVideoProvider(credential, "My YouTube Playlist Manager", sourcePlaylistId);

    // --- Videos sammeln ---
    List<VideoInfo> allAvailableVideos = new List<VideoInfo>(); // Verwende deine VideoInfo-Klasse

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
    
    var episodes = episodeConverter.ExtractSeasonEpisodes(allAvailableVideos);
    Console.WriteLine($"Videos:\n\t{
      string.Join("\n\t", episodes.Where(x => x.Season is not null)
                                            .OrderBy(x => x.Season)
                                            .ThenBy(x => x.Episode)
                                            .Select(x => $"Staffel {x.Season}, Episode {x.Episode}, NAME: {x.Title} ({x.VideoId})"))
    }");
    Console.WriteLine($"\nInsgesamt {allAvailableVideos.Count} einzigartige Videos gesammelt.");

    // --- Staffeln verarbeiten ---

    // Beispiel für Staffel 1
    uint seasonToUpdate = 1;
    if (!string.IsNullOrEmpty(targetPlaylistId_Season1) &&
        targetPlaylistId_Season1 != "PL_TARGET_SEASON_1") // Prüfe auf Standardwert
    {
      Console.WriteLine($"\nVerarbeite Staffel {seasonToUpdate} für Playlist {targetPlaylistId_Season1}...");
      // Stelle sicher, dass FilterAndSortVideosForSeason existiert und List<string> zurückgibt
      // List<string> season1VideoIds = updater.FilterAndSortVideosForSeason(allAvailableVideos, seasonToUpdate);
      // Stelle sicher, dass UpdateTargetPlaylistAsync existiert
      // await updater.UpdateTargetPlaylistAsync(targetPlaylistId_Season1, season1VideoIds);
    }
    else
    {
      Console.WriteLine(
        $"\nÜberspringe Staffel {seasonToUpdate}, da keine gültige Ziel-Playlist ID ('{targetPlaylistId_Season1}') angegeben wurde.");
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
  Console.Error.WriteLine($"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}");
  Console.Error.WriteLine(ex.StackTrace); // Mehr Details bei Bedarf
}

Console.WriteLine("\nDrücke eine Taste zum Beenden (nur bei lokaler Ausführung relevant).");
Console.ReadKey(); // Behalte dies für die lokale Ausführung bei