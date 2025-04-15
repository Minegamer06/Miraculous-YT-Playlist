using Google.Apis.YouTube.v3.Data;

namespace Minegamer95.YTPlaylistManager.Main.Services;

public interface IPlaylistInteraction
{
  // Listet alle Items einer Playlist auf
  Task<List<PlaylistItem>> ListItemsAsync(string playlistId);

  // Löscht ein spezifisches Item
  Task DeleteItemAsync(string playlistItemId);

  // Fügt ein neues Item ein (gibt das eingefügte Item zurück, evtl. mit von API generierter ID)
  Task<PlaylistItem?> InsertItemAsync(PlaylistItem? itemToInsert);

  // Aktualisiert ein Item (gibt das aktualisierte Item zurück)
  Task<PlaylistItem?> UpdateItemAsync(PlaylistItem? itemToUpdate);
}