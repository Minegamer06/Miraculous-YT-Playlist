using Google.Apis.YouTube.v3.Data;

namespace Minegamer95.YTPlaylistManager.Main.Model;

public record PlaylistChangePlan(
  List<string> ToDelete,
  List<PlaylistItem> ToUpdate,
  List<(string VideoId, uint Position)> ToInsert
);