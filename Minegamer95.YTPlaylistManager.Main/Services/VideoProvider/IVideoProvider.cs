using Minegamer95.YTPlaylistManager.Main.Model;

namespace Minegamer95.YTPlaylistManager.Main.Services;

public interface IVideoProvider
{
 /// <summary>
 /// Holt Videos von einem bestimmten Anbieter.
 /// </summary>
 /// <returns></returns>
  Task<IEnumerable<VideoInfo>> GetVideos();
}