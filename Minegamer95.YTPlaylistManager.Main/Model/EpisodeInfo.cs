namespace Minegamer95.YTPlaylistManager.Main.Model;

public class EpisodeInfo : VideoInfo
{
  public uint? Season { get; set; }
  public uint? Episode { get; set; }

  public EpisodeInfo()
  {
    
  }

  public EpisodeInfo(VideoInfo videoInfo)
  {
    Description = videoInfo.Description;
    PublishedAt = videoInfo.PublishedAt;
    Title = videoInfo.Title;
    VideoId = videoInfo.VideoId;
  }
  
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