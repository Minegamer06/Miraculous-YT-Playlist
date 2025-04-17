using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Minegamer95.YTPlaylistManager.Main.Model;
using Minegamer95.YTPlaylistManager.Main.Services.Actions;

namespace Minegamer95.YTPlaylistManager.Main.Services;

public class PlaylistUpdater
{
  private readonly IPlaylistInteraction _playlistService;

  public PlaylistUpdater(IPlaylistInteraction playlistService)
  {
    _playlistService = playlistService;
  }

  public async Task UpdateTargetPlaylistAsync(string targetPlaylistId, List<string> desiredVideoIds,
    bool dryRun = false)
  {
    Console.WriteLine($"\nAktualisiere Playlist ID: {targetPlaylistId}...");
    if (string.IsNullOrEmpty(targetPlaylistId))
    {
      await Console.Error.WriteLineAsync("Fehler: Keine Ziel-Playlist-ID angegeben.");
      return;
    }

    try
    {
      // --- Schritt 0: For now we assume that the target playlist can not contian duplicates ---
      desiredVideoIds = desiredVideoIds.Distinct().ToList();
      
      // --- Schritt 1: Aktuellen Zustand lesen ---
      Console.WriteLine(" Schritt 1: Lese aktuelle Playlist-Elemente...");
      var currentItems = await _playlistService.ListItemsAsync(targetPlaylistId);
      Console.WriteLine($" {currentItems.Count} Elemente in der Playlist gefunden.");

      // --- Schritt 2: Änderungsplan berechnen ---
      Console.WriteLine(" Schritt 2: Berechne Änderungsplan...");
      var changePlan = CalculateChangePlan(currentItems, desiredVideoIds, targetPlaylistId);
      LogChangePlan(changePlan); // Optional: Plan zur Übersicht ausgeben

      // --- Schritt 3: Optimierte Aktionssequenz generieren ---
      Console.WriteLine(" Schritt 3: Generiere optimierte Aktionssequenz...");
      var orderedActions = GenerateActionSequence(changePlan);
      Console.WriteLine($" {orderedActions.Count} Aktionen geplant.");

      // --- Schritt 4: Aktionen ausführen ---
      Console.WriteLine($" Schritt 4: Führe Aktionen aus (DryRun={dryRun})...");
      if (!QuotaManager.Instance.CanExecute(orderedActions.GetCost(_playlistService)))
      {
        Console.WriteLine($"Quota für {orderedActions.Count} Aktionen nicht ausreichend. Abbruch.");
        return;
      }
      int actionCounter = 0;
      foreach (var action in orderedActions)
      {
        actionCounter++;
        // Console.WriteLine($"\n Aktion {actionCounter}/{orderedActions.Count}: {action.Describe()}");
        await action.ExecuteAsync(_playlistService, dryRun);
      }

      Console.WriteLine($"\nAktualisierung der Playlist {targetPlaylistId} abgeschlossen.");
    }
    catch (Exception ex)
    {
      await Console.Error.WriteLineAsync(
        $"Fehler beim Aktualisieren der Playlist {targetPlaylistId}: {ex.Message}\n{ex.StackTrace}");
      // Gib mehr Details aus für die Fehlersuche
    }
  }

  public PlaylistChangePlan CalculateChangePlan(List<PlaylistItem> currentItems, List<string> desiredVideoIds,
    string playlistId)
  {
    var videosToDelete = new List<PlaylistItem>();
    var videosToUpdate = new List<PlaylistItem>();
    var videosToInsert = new Dictionary<string, long>(); // videoId -> targetPosition
    var groupedItems = currentItems.GroupBy(item => item.Snippet?.ResourceId?.VideoId).ToList();
    var simulator = new PlaylistSimulator(groupedItems.Select(x => x.First()).ToList(), desiredVideoIds, playlistId);


    // --- Schritt 1: Ungewolltes löschen ---
    foreach (var item in groupedItems)
    {
      if (desiredVideoIds.Contains(item.Key!))
        continue;

      foreach (var VARIABLE in COLLECTION)
      {
        
      }
      simulator.RemoveItem()
        
    }
    // Durch aktuelle Items iterieren
    foreach (var item in currentItems.GroupBy(item => item.Snippet?.ResourceId?.VideoId))
    {
      var videoId = item.Key!;
      var desiredPosition = desiredVideoIds.IndexOf(videoId);
      PlaylistItem video = item.FirstOrDefault(i => i.Snippet?.Position == desiredPosition, item.First());
      var currentPosition = video.Snippet.Position ?? -1; // Aktuelle Position

      if (item.Count() > 1)
      {
        foreach (var v in item.Where(i => i != video))
        {
          videosToDelete.Add(v); // Lösche alle Duplikate
        }
      }


      if (currentPosition == -1)
      {
        Console.WriteLine($"WARNUNG: Video {videoId} hat keine Position in der Playlist. Wird ignoriert.");
        continue; // Items ohne Position können wir nicht sinnvoll verarbeiten
      }
      
      if (desiredPosition == -1) // Nicht mehr in der gewünschten Liste? -> Löschen
      {
        videosToDelete.Add(video);
      }
      // In der gewünschten Liste, aber an der falschen Position? -> Update planen
      // Wichtig: Vergleiche long mit int, Konvertierung ist sicher hier.
      else if (currentPosition != desiredPosition)
      {
        // Erstelle eine *Kopie* oder setze nur die Zielposition im Snippet
        // Vorsicht: Ändere nicht das Original-Item direkt, wenn es noch woanders gebraucht wird.
        // Hier setzen wir die Zielposition für den Plan.
        // Der Update-API-Call braucht sowieso ein neues Snippet-Objekt.
        var itemForUpdatePlan = new PlaylistItem
        {
          Id = video.Id,
          Snippet = new PlaylistItemSnippet
          {
            PlaylistId = video.Snippet.PlaylistId, // Wichtig für Update Action
            ResourceId = video.Snippet.ResourceId, // Wichtig für Update Action
            Position = desiredPosition // Zielposition setzen
            // Andere Snippet-Teile wie Titel sind für die Positionsänderung irrelevant
          }
        };
        videosToUpdate.Add(itemForUpdatePlan);
      }
    }

    // Durch gewünschte IDs iterieren, um fehlende zu finden -> Insert planen
    for (int i = 0; i < desiredVideoIds.Count; i++)
    {
      var desiredVideoId = desiredVideoIds[i];
      if (currentItems.All(x => x.Snippet.ResourceId.VideoId != desiredVideoId))
      {
        videosToInsert.Add(desiredVideoId, i); // videoId -> targetPosition
      }
    }

    return new PlaylistChangePlan(playlistId, videosToDelete, videosToUpdate, videosToInsert);
  }

  private List<IPlaylistAction> GenerateActionSequence(PlaylistChangePlan plan)
  {
    var actions = new List<IPlaylistAction>();

    // --- Phase 1: Plan Deletions (Descending Order by Current Position) ---
    var sortedDeletes = plan.ToDelete
      .Where(item =>
        item.Snippet?.Position != null && !string.IsNullOrEmpty(item.Id)) // Nur löschen, wenn Position & ID bekannt
      .OrderByDescending(item => item.Snippet.Position!.Value)
      .ToList();

    Console.WriteLine($"\n--- Planungsphase: Löschungen ({sortedDeletes.Count}) ---");
    foreach (var itemToDelete in sortedDeletes)
    {
      Console.WriteLine(
        $" Planung Löschung für: {itemToDelete.Snippet.ResourceId.VideoId} (Pos: {itemToDelete.Snippet.Position.Value})");
      actions.Add(new DeleteAction(itemToDelete));
    }

    // --- Phase 2: Plan Updates and Inserts (Ascending Order by Target Position) ---
    var placements = new List<(string videoId, long targetPosition, PlaylistItem? originalItemForUpdate)>();

    // Updates hinzufügen (originalItem enthält Zielposition im Snippet)
    foreach (var itemToUpdate in plan.ToUpdate)
    {
      if (itemToUpdate.Snippet?.Position == null || string.IsNullOrEmpty(itemToUpdate.Id))
        continue; // Zielposition & ID muss bekannt sein
      placements.Add((itemToUpdate.Snippet.ResourceId.VideoId, itemToUpdate.Snippet.Position.Value, itemToUpdate));
    }

    // Inserts hinzufügen
    foreach (var kvp in plan.ToInsert)
    {
      placements.Add((kvp.Key, kvp.Value, null)); // Kein originalItem bei Insert
    }

    // Sortieren nach Zielposition (aufsteigend)
    var sortedPlacements = placements.OrderBy(p => p.targetPosition).ToList();

    Console.WriteLine($"\n--- Planungsphase: Platzierungen ({sortedPlacements.Count}) ---");
    foreach (var placement in sortedPlacements)
    {
      string videoId = placement.videoId;
      long targetPosition = placement.targetPosition;
      PlaylistItem? originalItemForUpdate = placement.originalItemForUpdate;

      if (originalItemForUpdate != null) // Es ist ein Update (Move)
      {
        Console.WriteLine($" Planung Update für: {videoId} nach Ziel-Pos {targetPosition}");
        // Die UpdateAction braucht das Item mit ID und die Zielposition
        actions.Add(new UpdateAction(originalItemForUpdate, targetPosition));
      }
      else // Es ist ein Insert
      {
        Console.WriteLine($" Planung Insert für: {videoId} an Ziel-Pos {targetPosition}");
        string playlistId = plan.PlaylistId; // Playlist ID aus dem Plan
        if (playlistId == "FEHLENDE_PLAYLIST_ID")
        {
          Console.Error.WriteLine($"FEHLER: Playlist ID konnte nicht für Insert von {videoId} ermittelt werden!");
          // Aktion überspringen oder Fehler werfen
          continue;
        }

        actions.Add(new InsertAction(videoId, targetPosition, playlistId));
      }
    }

    return actions;
  }

  // Hilfsmethode zum Loggen des Change Plans (Optional)
  private void LogChangePlan(PlaylistChangePlan plan)
  {
    Console.WriteLine("\n--- Berechneter Änderungsplan ---");
    Console.WriteLine($" Zu Löschen ({plan.ToDelete.Count}):");
    foreach (var item in plan.ToDelete)
      Console.WriteLine($"  - {item.Snippet?.ResourceId?.VideoId} (Pos: {item.Snippet?.Position})");
    Console.WriteLine($" Zu Aktualisieren ({plan.ToUpdate.Count}):");
    foreach (var item in plan.ToUpdate)
      Console.WriteLine($"  - {item.Snippet?.ResourceId?.VideoId} (ZielPos: {item.Snippet?.Position})");
    Console.WriteLine($" Zu Einfügen ({plan.ToInsert.Count}):");
    foreach (var kvp in plan.ToInsert) Console.WriteLine($"  - {kvp.Key} (ZielPos: {kvp.Value})");
    Console.WriteLine("---------------------------------");
  }
}