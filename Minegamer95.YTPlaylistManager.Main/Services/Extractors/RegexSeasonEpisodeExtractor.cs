using System.Text.RegularExpressions;
using Minegamer95.YTPlaylistManager.Main.Model;

namespace Minegamer95.YTPlaylistManager.Main.Services.Extractors;

public class RegexSeasonEpisodeExtractor : ISeasonEpisodeExtractor
{
  private readonly Regex _regex;
  private readonly string _episodeGroupName;
  private readonly string _seasonGroupName;

  /// <summary>
  /// Erstellt einen neuen RegexSeasonEpisodeExtractor mit einem angegebenen Regex-Muster und optionalem Timeout.
  /// </summary>
  /// <param name="pattern">Das Regex-Muster, das verwendet wird, um Staffel- und Episodeninformationen zu extrahieren.</param>
  /// <param name="seasonGroupName"></param>
  /// <param name="timeout">Die maximale Zeitspanne, die für die Regex-Operation zulässig ist (default 2 Sec.).</param>
  /// <param name="episodeGroupName"></param>
  public RegexSeasonEpisodeExtractor(string pattern = RegexPatterns.SeasonEpisodePattern, string episodeGroupName = "episode",
    string seasonGroupName = "season", TimeSpan? timeout = null)
  {
    _episodeGroupName = episodeGroupName;
    _seasonGroupName = seasonGroupName;
    _regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, timeout ?? TimeSpan.FromSeconds(2));
  }

  public EpisodeInfo ExtractSeasonEpisode(VideoInfo videoInfo)
  {
    var episodeInfo = new EpisodeInfo(videoInfo);
    if (string.IsNullOrEmpty(videoInfo.Title))
      return episodeInfo;

    var match = _regex.Match(videoInfo.Title);
    if (!match.Success) return episodeInfo;
    if (int.TryParse(match.Groups[_seasonGroupName].Value, out var season))
    {
      episodeInfo.Season = season;
    }

    if (int.TryParse(match.Groups[_episodeGroupName].Value, out var episode))
    {
      episodeInfo.Episode = episode;
    }

    return episodeInfo;
  }

  public List<EpisodeInfo> ExtractSeasonEpisodes(IEnumerable<VideoInfo> videoInfos)
  {
    return videoInfos.Select(ExtractSeasonEpisode).ToList();
  }
}
