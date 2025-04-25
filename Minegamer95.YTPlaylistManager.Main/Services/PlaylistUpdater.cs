using Google.Apis.YouTube.v3.Data;
using Minegamer95.YTPlaylistManager.Main.Model;
using Minegamer95.YTPlaylistManager.Main.Services.Actions;

namespace Minegamer95.YTPlaylistManager.Main.Services;

public class PlaylistUpdater
{
  private readonly IPlaylistInteraction _playlistService;
  private string _targetPlaylistId = string.Empty;
  private bool _dryRun = false;

  public PlaylistUpdater(IPlaylistInteraction playlistService)
  {
    _playlistService = playlistService;
  }

  public async Task UpdateTargetPlaylistAsync(string targetPlaylistId, List<string> desiredVideoIds,
    bool dryRun = false)
  {
    Console.WriteLine($"\nAktualisiere Playlist ID: {targetPlaylistId}...");
    _targetPlaylistId = targetPlaylistId;
    _dryRun = dryRun;
    if (string.IsNullOrEmpty(_targetPlaylistId))
    {
      await Console.Error.WriteLineAsync("Fehler: Keine Ziel-Playlist-ID angegeben.");
      return;
    }

    try
    {
      // --- Schritt 0: For now we assume that the target playlist can not contian duplicates ---
      desiredVideoIds = desiredVideoIds.Distinct().ToList();

      // --- Schritt 1: Aktuellen Zustand lesen ---
      //Console.WriteLine(" Schritt 1: Lese aktuelle Playlist-Elemente...");
      var currentItems = await _playlistService.ListItemsAsync(_targetPlaylistId);
      Console.WriteLine($" {currentItems.Count} Elemente in der Playlist gefunden.");

      // --- Schritt 2: Änderungsplan berechnen ---
      //Console.WriteLine(" Schritt 2: Berechne Änderungsplan...");
      var changePlan = CalculateChangePlan(currentItems, desiredVideoIds, _targetPlaylistId);
      // LogChangePlan(changePlan); // Optional: Plan zur Übersicht ausgeben

      // --- Schritt 3: Aktionen ausführen ---
      await ApplyChanges(changePlan);
    }
    catch (Exception ex)
    {
      await Console.Error.WriteLineAsync(
        $"Fehler beim Aktualisieren der Playlist {_targetPlaylistId}");
    }
  }

  public PlaylistChangePlan CalculateChangePlan(List<PlaylistItem> currentItems, List<string> desiredVideoIds,
    string playlistId)
  {
    var videosToDelete = new List<PlaylistItem>();
    var videosToUpdate = new List<PlaylistItem>();
    var playlistActions = new List<IPlaylistAction>();
    var videosToInsert = new Dictionary<string, long>(); // videoId -> targetPosition
    var groupedItems = currentItems.GroupBy(item => item.Snippet?.ResourceId?.VideoId).ToList();
    var simulator = new PlaylistSimulator(groupedItems.Select(x => x.First()).ToList(), desiredVideoIds, playlistId);


    // --- Schritt 1: Ungewolltes löschen ---
    foreach (var item in groupedItems)
    {
      if (desiredVideoIds.Contains(item.Key!))
        continue;

      foreach (var vItem in item)
      {
        videosToDelete.Add(vItem);
        playlistActions.Add(new DeleteAction(vItem));
      }

      simulator.RemoveByVideoId(item.Key!);
    }

    // --- Schritt 2: Fehlendes hinzufügen ---
    for (int i = 0; i < desiredVideoIds.Count; i++)
    {
      var desiredVideoId = desiredVideoIds[i];
      if (groupedItems.Any(x => x.Key == desiredVideoId)) continue;
      var realPos = simulator.AddByVideoId(desiredVideoId, i);
      videosToInsert.Add(desiredVideoId, realPos); // videoId -> targetPosition
      playlistActions.Add(new InsertAction(desiredVideoId, realPos, playlistId));
    }

    // --- Schritt 3: Vorhandene aktualisieren, falls sich Position geändert hat ---
    var getCurrentOrder = simulator.GetSimulatorItems();
    var updateOrder = groupedItems.OrderByDescending(x =>
    {
      var video = getCurrentOrder.FirstOrDefault(y =>
        y.Item.Snippet.ResourceId.VideoId == x.Key);
      return Math.Abs(video!.CurrentPositon - video.TargetPosition);
    }).ToList();
    foreach (var item in updateOrder)
    {
      var video = item.First();
      if (!desiredVideoIds.Contains(item.Key!))
        continue;
      if (item.Count() > 1)
      {
        foreach (var vItem in item.Skip(1))
        {
          videosToDelete.Add(vItem);
          playlistActions.Add(new DeleteAction(vItem));
        }
      }

      if (simulator.ItemsIsOnTargetPos(video)) continue;
      var realPos = simulator.UpdatePosByVideoId(item.Key!, desiredVideoIds.IndexOf(item.Key!));
      var itemForUpdatePlan = new PlaylistItem
      {
        Id = video.Id,
        Snippet = new PlaylistItemSnippet
        {
          PlaylistId = video.Snippet.PlaylistId,
          ResourceId = video.Snippet.ResourceId,
          Position = realPos
        }
      };
      videosToUpdate.Add(itemForUpdatePlan);
      playlistActions.Add(new UpdateAction(itemForUpdatePlan, realPos));
    }

    return new PlaylistChangePlan(playlistId, videosToDelete, videosToUpdate, videosToInsert, playlistActions);
  }

  private async Task ApplyChanges(PlaylistChangePlan changePlan)
  {
    if (changePlan.Actions.Count == 0)
    {
      Console.WriteLine($"Keine Änderungen an Playlist {_targetPlaylistId} erforderlich.");
      return;
    }

    Console.WriteLine($" Schritt 4: Führe Aktionen aus (DryRun={_dryRun})...");
    var updateCost = changePlan.Actions.GetCost(_playlistService);
    if (!QuotaManager.Instance.CanExecute(updateCost))
    {
      Console.WriteLine($"Quota für {changePlan.Actions.Count} Aktionen nicht ausreichend. Abbruch.");
      return;
    }

    Console.WriteLine($"Kosten für die Aktionen: {updateCost} Quota-Einheiten");
    Console.WriteLine(
      $"Aktuelle Quota: {QuotaManager.Instance.RemainingPoints} Quota-Einheiten, Verbleibend nach Update: {QuotaManager.Instance.RemainingPoints - updateCost} Quota-Einheiten");
    var actionCounter = 0;
    var cursorPosStart = Console.GetCursorPosition();
    var cursorPosEnd = Console.GetCursorPosition();
    foreach (var action in changePlan.Actions)
    {
      actionCounter++;
      if (cursorPosStart != cursorPosEnd)
      {
        Console.SetCursorPosition(cursorPosStart.Left, cursorPosStart.Top);
        var range = Math.Abs(cursorPosEnd.Top - cursorPosStart.Top);
        for (int i = 0; i < range; i++)
          Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(cursorPosStart.Left, cursorPosStart.Top);
      }

      Console.WriteLine($"\n Aktion {actionCounter}/{changePlan.Actions.Count}: {action.Describe()}");
      cursorPosEnd = Console.GetCursorPosition();
      await action.ExecuteAsync(_playlistService, _dryRun);
    }

    Console.WriteLine($"\nAktualisierung der Playlist {_targetPlaylistId} abgeschlossen.");
  }

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