using System.Collections.ObjectModel;
using Google.Apis.YouTube.v3.Data;

namespace Minegamer95.YTPlaylistManager.Main;

public class PlaylistSimulator
{
  private readonly List<PlaylistSimulatorItem> _items;

  public PlaylistSimulator(List<PlaylistItem> items, List<string> target, string playlistId)
  {
    _items = items.OrderBy(x => x.Snippet?.Position).Select(x => new PlaylistSimulatorItem()
      { Item = x, TargetPosition = target.IndexOf(x.Snippet.ResourceId.VideoId) }).ToList();
    foreach (var item in _items)
    {
      if (item.Item.Snippet != null && string.IsNullOrEmpty(item.Item.Snippet.PlaylistId))
      {
        item.Item.Snippet.PlaylistId = playlistId;
      }
    }
  }

  public int AddByVideoId(string videoId, int targetPos)
  {
    var item = new PlaylistItem
    {
      Id = Guid.NewGuid().ToString(),
      Snippet = new PlaylistItemSnippet
      {
        Position = targetPos,
        ResourceId = new ResourceId
        {
          VideoId = videoId
        }
      }
    };

    return AddItem(item, targetPos);
  }

  public int AddItem(PlaylistItem? item, int targetPos)
  {
    if (item?.Snippet?.Position is null)
      return -1;

    var pos = _items.FindIndex(i => i.TargetPosition >= targetPos);
    // Falls kein Element gefunden wird, dann setze pos auf das Ende der Liste.
    if (pos == -1)
      pos = _items.Count;
    
    var plItem = new PlaylistSimulatorItem
    {
      Item = item,
      TargetPosition = targetPos
    };
    _items.Insert(pos, plItem);
    ReindexAll();
    return pos;
  }

  public bool RemoveByVideoId(string videoId)
  {
    var item = _items.FirstOrDefault(x => x.Item.Snippet?.ResourceId?.VideoId == videoId)?.Item;
    return item is not null && RemoveItem(item);
  }

  public bool RemoveItem(PlaylistItem item)
  {
    var index = _items.FindIndex(x => x.Item == item);
    if (index == -1)
      return false;

    _items.RemoveAt(index);
    ReindexAll();
    return true;
  }

  /// <summary>
  /// Aktualisiert die Position eines Videos in der Playlist.
  /// </summary>
  /// <param name="videoId">The VideoId to move.</param>
  /// <param name="newPos">The target Position for the Video.</param>
  /// <returns>The new Real Positon to use for the update Request.</returns>
  public int UpdatePosByVideoId(string videoId, int newPos)
  {
    var item = _items.FirstOrDefault(x => x.Item.Snippet?.ResourceId?.VideoId == videoId)?.Item;
    return UpdatePosItem(item, newPos);
  }

  /// <summary>
  /// Aktualisiert die Position eines Videos in der Playlist.
  /// </summary>
  /// <param name="item">The Playlist Item to move.</param>
  /// <param name="newPos">The target Position for the Video.</param>
  /// <returns>The new Real Positon to use for the update Request.</returns>
  public int UpdatePosItem(PlaylistItem? item, int newPos)
  {
    if (item?.Snippet?.Position is null)
      return -1;

    // altes Item finden
    var currentIndex = _items.FindIndex(x => x.Item == item);
    if (currentIndex == -1)
      return -1;

    // aus Liste rausnehmen
    _items.RemoveAt(currentIndex);
    
    ReindexAll();
    
    return AddItem(item, newPos);
  }

  public bool ItemsIsOnTargetPos(PlaylistItem? item)
  {
    var simItem = _items.FirstOrDefault(x => x.Item == item);
    return simItem is not null && simItem.CurrentPositon == simItem.TargetPosition;
  }
  public int GetItemPosByVideoId(string videoId)
  {
    var item = _items.FirstOrDefault(x => x.Item.Snippet?.ResourceId?.VideoId == videoId)?.Item;
    return item is not null ? GetItemPos(item) : -1;
  }

  public int GetItemPos(PlaylistItem item)
  {
    return _items.FindIndex(x => x.Item == item);
  }

  /// <summary>
  /// Setzt für jedes Item die Snippet.Position auf den aktuellen Index zurück.
  /// </summary>
  private void ReindexAll()
  {
    for (int i = 0; i < _items.Count; i++)
    {
      _items[i].Item.Snippet.Position = i;
    }
  }

  public ReadOnlyCollection<PlaylistItem> GetItems()
  {
    return _items.Select(x => x.Item).ToList().AsReadOnly();
  }
  
  public ReadOnlyCollection<PlaylistSimulatorItem> GetSimulatorItems()
  {
    return _items.AsReadOnly();
  }
}

public class PlaylistSimulatorItem
{
  public PlaylistItem Item { get; set; } = null!;
  public int TargetPosition { get; set; }
  public int CurrentPositon => (int)Math.Clamp(Item.Snippet.Position ?? 0, 0 , int.MaxValue);
}