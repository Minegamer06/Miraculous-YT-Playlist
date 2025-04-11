using Minegamer95.YTPlaylistManager.Main.Model;

namespace Minegamer95.YTPlaylistManager.Main.Services.Extractors;

public interface ISeasonEpisodeExtractor
{
  EpisodeInfo? ExtractSeasonEpisode(VideoInfo videoInfo);
}