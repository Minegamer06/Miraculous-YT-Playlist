namespace Minegamer95.YTPlaylistManager.Main.Model;

public class PlaylistTask
{
  public string? Name { get; set; }
  public required string TargetPlaylistId { get; set; }
  public List<string>? SourcePlaylistIds { get; set; }
  public List<string>? SourceChannelIds { get; set; }
  public List<EpisodeInfo>? PredefinedEpisodes { get; set; }
  public int? Season { get; set; }
  public string? RegexPattern { get; set; }
}