using Google.Apis.YouTube.v3.Data;
using Minegamer95.YTPlaylistManager.Main.Services.Actions;

namespace Minegamer95.YTPlaylistManager.Test.UpdatePlaylist;


public class PlaylistSimulator
{
  // ... (Kompletter Code für PlaylistSimulator wie zuvor) ...
  private readonly Dictionary<string, long> _positions;
  private readonly Dictionary<string, PlaylistItem> _items; // Hinzufügen, um Item-IDs zu simulieren/zuordnen

  // Konstruktor nimmt den initialen Zustand (Liste von PlaylistItems)
  public PlaylistSimulator(IEnumerable<PlaylistItem> initialItems)
  {
    _items = new Dictionary<string, PlaylistItem>();
    _positions = new Dictionary<string, long>();
    long currentPos = 0; // Falls Positionen nicht gesetzt sind, simulieren wir sie
    foreach (var item in initialItems.OrderBy(i => i.Snippet?.Position ?? long.MaxValue)) // Nach Position sortieren
    {
      var videoId = item.Snippet?.ResourceId?.VideoId;
      if (videoId == null) continue; // Ungültiges Item überspringen

      // Stelle sicher, dass jedes Item eine (simulierte) ID hat
      if (string.IsNullOrEmpty(item.Id))
      {
        item.Id = $"simulated_{videoId}_{Guid.NewGuid()}"; // Eindeutige simulierte ID
      }

      long position = item.Snippet?.Position ?? currentPos; // Nutze vorhandene Pos oder zähle hoch
      _positions[videoId] = position;
      _items[item.Id] = item; // Item anhand seiner ID speichern
      currentPos = position + 1;
    }

    // Korrigiere die Positionen, falls sie nicht sequentiell waren
    RenumberPositions();
  }

  // Gibt den aktuellen simulierten Zustand zurück (als Kopie der Items)
  public List<PlaylistItem> GetCurrentSimulatedItems()
  {
    // Gibt die Items in der aktuellen Reihenfolge zurück
    return _positions
      .OrderBy(kvp => kvp.Value)
      .Select(kvp => _items.Values.FirstOrDefault(item => item.Snippet?.ResourceId?.VideoId == kvp.Key))
      .Where(item => item != null)
      .Select(item =>
      {
        // Stelle sicher, dass die Position im zurückgegebenen Item aktuell ist
        if (item != null && item.Snippet != null)
        {
          item.Snippet.Position = _positions[item.Snippet.ResourceId.VideoId];
        }

        return item!; // item ist hier nicht null wegen Where-Filter
      })
      .ToList();
  }

  // Gibt nur die Positionen zurück (nützlich für Assertions)
  public Dictionary<string, long> GetCurrentSimulatedPositions()
  {
    return new Dictionary<string, long>(_positions);
  }

  // Wendet eine Aktion auf den simulierten Zustand an (unverändert)
  public void ApplyAction(IPlaylistAction action)
  {
    if (action is DeleteAction da) RemoveItemById(da.PlaylistItemId); // Löschen über ID
    else if (action is UpdateAction ua) UpdateItem(ua.VideoId, ua.TargetPosition);
    else if (action is InsertAction ia) AddItem(ia.VideoId, ia.TargetPosition, ia.PlaylistId); // PlaylistId hinzugefügt
    else Console.WriteLine($"Simulator Warnung: Unbekannter Aktionstyp {action.GetType().Name}");
  }


  // --- Interne Simulationsmethoden ---

  // Hilfsmethode zum Neu-Nummerieren der Positionen
  private void RenumberPositions()
  {
    long currentPos = 0;
    var sortedVideoIds = _positions.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
    _positions.Clear(); // Leeren und neu befüllen
    foreach (var videoId in sortedVideoIds)
    {
      _positions.Add(videoId, currentPos++);
    }
  }

  // Angepasst, um über PlaylistItemId zu löschen
  public void RemoveItemById(string playlistItemId)
  {
    if (!_items.TryGetValue(playlistItemId, out var itemToRemove))
    {
      Console.WriteLine($"Simulator Warnung: Item mit ID {playlistItemId} nicht gefunden zum Löschen.");
      return;
    }

    var videoId = itemToRemove.Snippet?.ResourceId?.VideoId;
    if (videoId == null) return; // Sollte nicht passieren

    if (!_positions.TryGetValue(videoId, out var position)) return; // Position nicht gefunden

    _positions.Remove(videoId);
    _items.Remove(playlistItemId); // Auch aus der Item-Liste entfernen

    // Positionen der nachfolgenden Elemente anpassen (unverändert)
    var keysToUpdate = _positions.Where(kvp => kvp.Value > position).Select(kvp => kvp.Key).ToList();
    foreach (var key in keysToUpdate) _positions[key]--;
  }

  // Behält die alte RemoveItem bei Bedarf (obwohl Löschen per ID besser ist)
  public void RemoveItem(string videoId)
  {
    // Finde das Item, das zu dieser VideoId gehört
    var itemEntry = _items.FirstOrDefault(kvp => kvp.Value.Snippet?.ResourceId?.VideoId == videoId);
    if (itemEntry.Key != null) // Wenn ein Item gefunden wurde
    {
      RemoveItemById(itemEntry.Key);
    }
    else
    {
      Console.WriteLine($"Simulator Warnung: Konnte kein Item für VideoId {videoId} zum Löschen finden.");
    }
  }

  // Fügt PlaylistId hinzu und erstellt ein simuliertes Item
  public void AddItem(string videoId, long position, string playlistId)
  {
    if (_positions.ContainsKey(videoId))
    {
      Console.WriteLine($"Simulator Warnung: Video {videoId} ist bereits vorhanden...");
      return;
    }

    // Erstelle ein simuliertes PlaylistItem
    var newItem = new PlaylistItem
    {
      Id = $"simulated_{videoId}_{Guid.NewGuid()}", // Eindeutige simulierte ID
      Snippet = new PlaylistItemSnippet
      {
        PlaylistId = playlistId,
        ResourceId = new ResourceId { Kind = "youtube#video", VideoId = videoId },
        Position = position,
        Title = $"Simulated Title for {videoId}" // Dummy-Titel
      }
    };

    // Positionen anpassen (unverändert)
    var keysToUpdate = _positions.Where(kvp => kvp.Value >= position).Select(kvp => kvp.Key).ToList();
    foreach (var key in keysToUpdate) _positions[key]++;

    _positions.Add(videoId, position);
    _items.Add(newItem.Id, newItem); // Füge das neue Item hinzu
  }

  public void UpdateItem(string videoId, long newPosition)
  {
    if (!_positions.TryGetValue(videoId, out var currentPosition))
    {
      Console.WriteLine($"Simulator Warnung: Video {videoId} nicht gefunden...");
      return;
    }

    if (currentPosition == newPosition) return;

    // Finde das zugehörige Item, um dessen Position im Snippet zu aktualisieren
    var itemEntry = _items.FirstOrDefault(kvp => kvp.Value.Snippet?.ResourceId?.VideoId == videoId);


    _positions.Remove(videoId); // Temporär aus Positionen entfernen
    if (newPosition < currentPosition)
    {
      var keysToShift = _positions.Where(kvp => kvp.Value >= newPosition && kvp.Value < currentPosition)
        .Select(kvp => kvp.Key).ToList();
      foreach (var key in keysToShift) _positions[key]++;
    }
    else
    {
      var keysToShift = _positions.Where(kvp => kvp.Value > currentPosition && kvp.Value <= newPosition)
        .Select(kvp => kvp.Key).ToList();
      foreach (var key in keysToShift) _positions[key]--;
    }

    // Füge zur Positionsliste hinzu
    if (!_positions.TryAdd(videoId, newPosition))
    {
      Console.WriteLine($"Simulator Fehler: Konnte Position für Video {videoId} nicht hinzufügen...");
      _positions[videoId] = newPosition; // Überschreiben versuchen
    }

    // Aktualisiere die Position im Snippet des Items
    if (itemEntry.Key != null && _items.TryGetValue(itemEntry.Key, out var itemToUpdate) &&
        itemToUpdate.Snippet != null)
    {
      itemToUpdate.Snippet.Position = newPosition;
    }
  }
}