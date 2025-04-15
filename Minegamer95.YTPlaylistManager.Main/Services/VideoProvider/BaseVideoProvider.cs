using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Minegamer95.YTPlaylistManager.Main.Model;

namespace Minegamer95.YTPlaylistManager.Main.Services;

public abstract class BaseVideoProvider : IVideoProvider
{
  protected readonly YouTubeService _youtubeService;
  private readonly string _applicationName;

  protected BaseVideoProvider(UserCredential credential, string applicationName)
  {
    _applicationName = applicationName;
    _youtubeService = new YouTubeService(new BaseClientService.Initializer()
    {
      HttpClientInitializer = credential,
      ApplicationName = _applicationName
    });
  }
  
  public abstract Task<List<VideoInfo>> GetVideos(string id);

}