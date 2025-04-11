namespace Minegamer95.YTPlaylistManager.Main.Model;

public class VideoInfo
{
  public string VideoId { get; set; }
  public string Title { get; set; }
  public string Description { get; set; }
  public DateTimeOffset? PublishedAt { get; set; } // Nullable<DateTime>

  public override string ToString()
  {
    return $"ID: {VideoId}, Titel: {Title}";
  }
}