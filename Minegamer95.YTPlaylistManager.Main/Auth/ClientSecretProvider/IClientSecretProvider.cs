using Google.Apis.Auth.OAuth2;

namespace Minegamer95.YTPlaylistManager.Main.Auth.ClientSecretProvider;

public interface IClientSecretProvider
{
  ClientSecrets? GetClientSecrets();
}