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

  public bool AddByVideoId(string videoId, int targetPos)
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

  public bool AddItem(PlaylistItem item, int targetPos)
  {
    if (item.Snippet?.Position is null)
      return false;

    // Grenze an Anzahl der Items
    var pos = _items.Where(i => i.TargetPosition < targetPos).MaxBy(i => i.TargetPosition)?.CurrentPositon ?? 0;
    pos = Math.Clamp(pos, 0, _items.Count);
    var plItem = new PlaylistSimulatorItem
    {
      Item = item,
      TargetPosition = targetPos
    };
    _items.Insert(pos, plItem);
    ReindexAll();
    return true;
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

  public bool UpdatePosByVideoId(string videoId, int newPos)
  {
    var item = _items.FirstOrDefault(x => x.Item.Snippet?.ResourceId?.VideoId == videoId)?.Item;
    return item is not null && UpdatePosItem(item, newPos);
  }

  public bool UpdatePosItem(PlaylistItem item, int newPos)
  {
    if (item.Snippet?.Position is null)
      return false;

    // altes Item finden
    var currentIndex = _items.FindIndex(x => x.Item == item);
    if (currentIndex == -1)
      return false;

    // aus Liste rausnehmen
    _items.RemoveAt(currentIndex);
    
    ReindexAll();
    
    return AddItem(item, newPos);
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
}

public class PlaylistSimulatorItem
{
  public PlaylistItem Item { get; set; } = null!;
  public int TargetPosition { get; set; }
  public int CurrentPositon => (int)Math.Clamp(Item.Snippet.Position ?? 0, 0 , int.MaxValue);
}