using Google.Apis.YouTube.v3.Data;
using Minegamer95.YTPlaylistManager.Main.Services.Actions;

namespace Minegamer95.YTPlaylistManager.Main.Model;

public record PlaylistChangePlan(
  string PlaylistId,
  List<PlaylistItem> ToDelete,
  List<PlaylistItem> ToUpdate,
  Dictionary<string, long> ToInsert,
  List<IPlaylistAction> Actions
)
{

}

