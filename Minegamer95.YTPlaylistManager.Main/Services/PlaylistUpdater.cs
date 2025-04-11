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
        public PlaylistUpdater(UserCredential credential)
        {
            _youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }

        // --- 1. Videoabruf ---

        /// <summary>
        /// Ruft alle Videos aus dem Uploads-Ordner eines bestimmten Kanals ab.
        /// </summary>
        /// <param name="channelId">Die ID des YouTube-Kanals.</param>
        /// <returns>Eine Liste von VideoInfo-Objekten.</returns>
        public async Task<List<VideoInfo>> GetVideosFromChannelAsync(string channelId)
        {
            Console.WriteLine($"\nRufe Videos von Kanal ID: {channelId} ab...");
            List<VideoInfo> allVideos = new List<VideoInfo>();
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
                allVideos = await GetVideosFromPlaylistAsync(uploadsPlaylistId);

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fehler beim Abrufen der Videos vom Kanal {channelId}: {ex.Message}");
            }
            Console.WriteLine($"Abruf von Kanal {channelId} abgeschlossen. {allVideos.Count} Videos gefunden.");
            return allVideos;
        }


        /// <summary>
        /// Ruft alle Videos aus einer bestimmten Playlist ab.
        /// </summary>
        /// <param name="playlistId">Die ID der Quell-Playlist.</param>
        /// <returns>Eine Liste von VideoInfo-Objekten.</returns>
        public async Task<List<VideoInfo>> GetVideosFromPlaylistAsync(string playlistId)
        {
            Console.WriteLine($"\nRufe Videos von Playlist ID: {playlistId} ab...");
            List<VideoInfo> allVideos = new List<VideoInfo>();
            string nextPageToken = null;

            try
            {
                do
                {
                    var playlistItemsRequest = _youtubeService.PlaylistItems.List("snippet,contentDetails");
                    playlistItemsRequest.PlaylistId = playlistId;
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
                            // Versuche Staffel/Episode zu extrahieren (Beispielimplementierung)
                            ExtractSeasonEpisode(videoInfo);
                            allVideos.Add(videoInfo);
                        }
                    }

                    nextPageToken = playlistItemsResponse.NextPageToken;

                } while (nextPageToken != null);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fehler beim Abrufen der Videos von Playlist {playlistId}: {ex.Message}");
            }

            Console.WriteLine($"Abruf von Playlist {playlistId} abgeschlossen. {allVideos.Count} Videos gefunden.");
            return allVideos;
        }

        // --- 2. Filterung und Sortierung ---

        /// <summary>
        /// Extrahiert Staffel- und Episodennummer aus dem Videotitel oder der Beschreibung.
        /// PASSE DIESE LOGIK AN DEINE NAMENSKONVENTION AN!
        /// </summary>
        private void ExtractSeasonEpisode(VideoInfo videoInfo)
        {
            if (string.IsNullOrEmpty(videoInfo.Title)) return;

            // Beispiel: Sucht nach "SxxEyy" oder "Staffel xx Folge yy" (Groß/Kleinschreibung ignorieren)
            // PASSE DIESES REGEX-MUSTER AN!
            var regex = new Regex(@"(?:S|Staffel)\s?(\d{1,2})\s?(?:E|Folge)\s?(\d{1,3})", RegexOptions.IgnoreCase);
            var match = regex.Match(videoInfo.Title);

            if (match.Success && match.Groups.Count == 3)
            {
                if (uint.TryParse(match.Groups[1].Value, out uint season))
                {
                    videoInfo.Season = season;
                }
                if (uint.TryParse(match.Groups[2].Value, out uint episode))
                {
                    videoInfo.Episode = episode;
                }
            }
            else
            {
                // Fallback oder alternative Logik hier einfügen, falls das Muster nicht passt
                // z.B. Suche in der Beschreibung, oder anhand des Veröffentlichungsdatums gruppieren?
            }
        }

        /// <summary>
        /// Filtert eine Liste von Videos nach einer bestimmten Staffel und sortiert sie nach Episodennummer.
        /// </summary>
        /// <param name="allVideos">Die vollständige Liste der abgerufenen Videos.</param>
        /// <param name="seasonNumber">Die gewünschte Staffelnummer.</param>
        /// <returns>Eine sortierte Liste von Video-IDs für die angegebene Staffel.</returns>
        public List<string> FilterAndSortVideosForSeason(List<VideoInfo> allVideos, uint seasonNumber)
        {
            Console.WriteLine($"\nFiltere und sortiere Videos für Staffel {seasonNumber}...");

            var filteredVideos = allVideos
                .Where(v => v.Season.HasValue && v.Season.Value == seasonNumber && v.Episode.HasValue) // Nur Videos mit erkannter Staffel/Episode
                .OrderBy(v => v.Episode.Value) // Sortiere nach Episode
                // Optional: Zusätzliche Sortierung, z.B. nach Titel, falls Episodennummer fehlt oder gleich ist
                // .ThenBy(v => v.Title)
                .ToList(); // Wichtig: ToList() ausführen, um die Abfrage auszuführen

             Console.WriteLine($"{filteredVideos.Count} Videos für Staffel {seasonNumber} gefunden und sortiert.");
             foreach(var v in filteredVideos)
             {
                 Console.WriteLine($"  - {v}"); // Gibt die VideoInfos aus (mit ToString())
             }

            return filteredVideos.Select(v => v.VideoId).ToList(); // Gibt nur die Video-IDs zurück
        }

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