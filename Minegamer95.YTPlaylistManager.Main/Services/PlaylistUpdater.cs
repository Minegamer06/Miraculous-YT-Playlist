using System.Text.RegularExpressions;
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

        // Konstruktor: Initialisiert den YouTubeService mit den Anmeldeinformationen
        public PlaylistUpdater(UserCredential? credential)
        {
            _youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }

        // --- 2. Filterung und Sortierung ---


        // --- 3. Playlist-Aktualisierung ---

        /// <summary>
        /// Aktualisiert eine Ziel-Playlist, sodass sie exakt der übergebenen Liste von Video-IDs entspricht (inkl. Reihenfolge).
        /// ACHTUNG: Diese Methode löscht zuerst ALLE vorhandenen Videos aus der Ziel-Playlist!
        /// </summary>
        /// <param name="targetPlaylistId">Die ID der zu aktualisierenden Playlist.</param>
        /// <param name="desiredVideoIds">Die sortierte Liste der Video-IDs, die in der Playlist sein sollen.</param>
        /// <returns>Task.</returns>
        public async Task UpdateTargetPlaylistAsync(string targetPlaylistId, List<string> desiredVideoIds)
        {
            Console.WriteLine($"\nAktualisiere Playlist ID: {targetPlaylistId}...");
            if (string.IsNullOrEmpty(targetPlaylistId))
            {
                Console.Error.WriteLine("Fehler: Keine Ziel-Playlist-ID angegeben.");
                return;
            }

            try
            {
                // Schritt 1: Alle vorhandenen Elemente aus der Ziel-Playlist holen (um ihre IDs zum Löschen zu bekommen)
                Console.WriteLine(" Schritt 1: Lese aktuelle Playlist-Elemente...");
                List<string> currentItemIds = new List<string>();
                string nextPageToken = null;
                do
                {
                    var listRequest = _youtubeService.PlaylistItems.List("id"); // Nur ID wird benötigt
                    listRequest.PlaylistId = targetPlaylistId;
                    listRequest.MaxResults = 50;
                    listRequest.PageToken = nextPageToken;
                    var listResponse = await listRequest.ExecuteAsync();

                    if (listResponse.Items != null)
                    {
                        currentItemIds.AddRange(listResponse.Items.Select(item => item.Id));
                    }
                    nextPageToken = listResponse.NextPageToken;
                } while (nextPageToken != null);
                Console.WriteLine($" {currentItemIds.Count} Elemente in der Playlist gefunden.");


                // Schritt 2: Alle vorhandenen Elemente löschen
                 if (currentItemIds.Count > 0)
                 {
                     Console.WriteLine(" Schritt 2: Lösche alle vorhandenen Elemente...");
                     // Wichtig: Löschvorgänge einzeln oder in kleinen Batches durchführen, um API-Limits nicht zu überschreiten.
                     // Hier der einfache Ansatz: einzeln löschen.
                     int deleteCount = 0;
                     foreach (var itemId in currentItemIds)
                     {
                         try
                         {
                             await _youtubeService.PlaylistItems.Delete(itemId).ExecuteAsync();
                             deleteCount++;
                             // Optional: Kurze Pause einlegen, um Ratenlimits zu vermeiden
                             // await Task.Delay(100); // 100ms Pause
                         }
                         catch (Exception exDelete)
                         {
                              Console.Error.WriteLine($" Fehler beim Löschen von Item {itemId}: {exDelete.Message}");
                         }
                     }
                      Console.WriteLine($" {deleteCount} Elemente gelöscht.");
                 }
                 else
                 {
                      Console.WriteLine(" Schritt 2: Playlist ist bereits leer.");
                 }


                // Schritt 3: Die gewünschten Videos in der richtigen Reihenfolge hinzufügen
                Console.WriteLine($" Schritt 3: Füge {desiredVideoIds.Count} gewünschte Videos hinzu...");
                uint position = 0; // Position in der Playlist (0-basiert)
                int addCount = 0;
                foreach (var videoId in desiredVideoIds)
                {
                    var newPlaylistItem = new PlaylistItem
                    {
                        Snippet = new PlaylistItemSnippet
                        {
                            PlaylistId = targetPlaylistId,
                            ResourceId = new ResourceId
                            {
                                Kind = "youtube#video",
                                VideoId = videoId
                            },
                            Position = position // Setzt die Position in der Playlist
                        }
                    };

                    try
                    {
                        await _youtubeService.PlaylistItems.Insert(newPlaylistItem, "snippet").ExecuteAsync();
                        position++; // Nächste Position
                        addCount++;
                         // Optional: Kurze Pause
                         // await Task.Delay(100);
                    }
                    catch (Exception exInsert)
                    {
                         Console.Error.WriteLine($" Fehler beim Hinzufügen von Video {videoId}: {exInsert.Message}");
                         // Mögliche Gründe: Video nicht (mehr) verfügbar, Berechtigungsproblem, etc.
                    }
                }
                 Console.WriteLine($" {addCount} Videos erfolgreich hinzugefügt.");
                 Console.WriteLine($"\nAktualisierung der Playlist {targetPlaylistId} abgeschlossen.");

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fehler beim Aktualisieren der Playlist {targetPlaylistId}: {ex.Message}");
            }
        }
}