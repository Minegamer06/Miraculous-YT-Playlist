using Google.Apis.YouTube.v3.Data;
using Minegamer95.YTPlaylistManager.Main;
using Minegamer95.YTPlaylistManager.Main.Services;

namespace Minegamer95.YTPlaylistManager.Test.UpdatePlaylist;

public class SimulatedPlaylistService : IPlaylistInteraction, IQuotaCost
{
  private readonly PlaylistSimulator _simulator;
  private readonly List<PlaylistItem> _initialState; // Die "eigene Liste als Quelle"

  // Konstruktor akzeptiert die Testdaten (Anfangszustand)
  public SimulatedPlaylistService(List<PlaylistItem> initialState)
  {
    _initialState = initialState;
    _simulator = new PlaylistSimulator(_initialState);
    Console.WriteLine("SimulatedYouTubePlaylistService initialisiert.");
    Console.WriteLine("Anfangszustand (simuliert):");
    LogSimulatedState();
  }

  public SimulatedPlaylistService(List<(string videoId, long position)> initialState)
  {
    _initialState = initialState.Select(item => new PlaylistItem
    {
      Id = item.videoId,
      Snippet = new PlaylistItemSnippet
      {
        ResourceId = new ResourceId { VideoId = item.videoId },
        Position = item.position,
      }
    }).ToList();
    _simulator = new PlaylistSimulator(_initialState);
    Console.WriteLine("SimulatedYouTubePlaylistService initialisiert.");
    Console.WriteLine("Anfangszustand (simuliert):");
    LogSimulatedState();
  }

  // Gibt den definierten Anfangszustand zurück
  public Task<List<PlaylistItem>> ListItemsAsync(string playlistId)
  {
    Console.WriteLine(
      $"Simulated Service: ListItemsAsync für Playlist {playlistId} aufgerufen. Gebe initialen Zustand zurück.");
    // Wichtig: Gibt eine Kopie zurück, damit der Test den Zustand nicht versehentlich ändert
    var copy = _initialState.Select(item => new PlaylistItem
    {
      Id = item.Id,
      Snippet = new PlaylistItemSnippet
      {
        PlaylistId = item.Snippet?.PlaylistId ?? playlistId,
        Position = item.Snippet?.Position,
        ResourceId = item.Snippet?.ResourceId == null
          ? null
          : new ResourceId { Kind = item.Snippet.ResourceId.Kind, VideoId = item.Snippet.ResourceId.VideoId },
        Title = item.Snippet?.Title
        // Kopiere weitere benötigte Felder
      }
    }).ToList();
    return Task.FromResult(copy);
  }

  // Führt Löschen auf dem Simulator aus
  public Task DeleteItemAsync(string playlistItemId)
  {
    Console.WriteLine($"Simulated Service: DeleteItemAsync für Item {playlistItemId} aufgerufen.");
    _simulator.RemoveItemById(playlistItemId); // Nutze die ID-basierte Löschung
    LogSimulatedState();
    return Task.CompletedTask;
  }

  // Führt Einfügen auf dem Simulator aus
  public Task<PlaylistItem?> InsertItemAsync(PlaylistItem? itemToInsert)
  {
    var videoId = itemToInsert.Snippet?.ResourceId?.VideoId ?? "unknown_insert_id";
    var position = itemToInsert.Snippet?.Position ?? -1;
    var playlistId = itemToInsert.Snippet?.PlaylistId ?? "unknown_playlist";
    Console.WriteLine($"Simulated Service: InsertItemAsync für Video {videoId} an Pos {position} aufgerufen.");
    _simulator.AddItem(videoId, position, playlistId);
    LogSimulatedState();
    // Gib das (möglicherweise modifizierte) Item zurück
    // Finde das gerade hinzugefügte Item im Simulator
    var insertedItem = _simulator.GetCurrentSimulatedItems()
      .FirstOrDefault(i => i.Snippet?.ResourceId?.VideoId == videoId);
    return Task.FromResult(insertedItem ?? itemToInsert); // Fallback auf Original, falls nicht gefunden
  }

  // Führt Update auf dem Simulator aus
  public Task<PlaylistItem?> UpdateItemAsync(PlaylistItem? itemToUpdate)
  {
    var videoId = itemToUpdate.Snippet?.ResourceId?.VideoId ?? "unknown_update_id";
    var position = itemToUpdate.Snippet?.Position ?? -1;
    Console.WriteLine($"Simulated Service: UpdateItemAsync für Video {videoId} nach Pos {position} aufgerufen.");
    _simulator.UpdateItem(videoId, position);
    LogSimulatedState();
    // Gib das (möglicherweise modifizierte) Item zurück
    var updatedItem = _simulator.GetCurrentSimulatedItems()
      .FirstOrDefault(i => i.Snippet?.ResourceId?.VideoId == videoId);
    return Task.FromResult(updatedItem ?? itemToUpdate); // Fallback auf Original, falls nicht gefunden
  }

  // Hilfsmethode zum Ausgeben des Simulatorzustands (für Debugging im Test)
  private void LogSimulatedState()
  {
    Console.WriteLine("--- Simulator State ---");
    var items = _simulator.GetCurrentSimulatedItems();
    if (!items.Any())
    {
      Console.WriteLine("(Leer)");
    }
    else
    {
      foreach (var item in items)
      {
        Console.WriteLine($"- Pos {item.Snippet?.Position}: {item.Snippet?.ResourceId?.VideoId} (ID: {item.Id})");
      }
    }

    Console.WriteLine("-----------------------");
  }

  // Methode, um den Endzustand des Simulators für Assertions im Test abzurufen
  public List<PlaylistItem> GetFinalSimulatedState()
  {
    return _simulator.GetCurrentSimulatedItems();
  }

  public Dictionary<string, long> GetFinalSimulatedPositions()
  {
    return _simulator.GetCurrentSimulatedPositions();
  }

  public int GetCost(QuotaType type)
  {
    return 0;
  }
}