namespace Minegamer95.YTPlaylistManager.Main.Model;

public class VideoInfo
{
  public string VideoId { get; set; }
  public string Title { get; set; }
  public string Description { get; set; }
  public uint? Season { get; set; } // Nullable<uint>
  public uint? Episode { get; set; } // Nullable<uint>
  public DateTime? PublishedAt { get; set; } // Nullable<DateTime>

  public override string ToString()
  {
    string seasonEpisode = $"S{Season:D2}E{Episode:D2}";
    if (!Season.HasValue || !Episode.HasValue)
    {
      seasonEpisode = "Unbekannt";
    }
    return $"ID: {VideoId}, Titel: {Title} ({seasonEpisode})";
  }
}