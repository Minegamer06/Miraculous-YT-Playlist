using Google.Apis.Auth.OAuth2;

namespace Minegamer95.YTPlaylistManager.Main.Auth.ClientSecretProvider;

public class GitHubClientSecretsProvider : IClientSecretProvider
{
  private readonly string _clientIdEnvVar;
  private readonly string _clientSecretEnvVar;

  public GitHubClientSecretsProvider(string clientIdEnvVar = "YT_CLIENT_ID", string clientSecretEnvVar =
    "YT_CLIENT_SECRET")
  {
    _clientIdEnvVar = clientIdEnvVar;
    _clientSecretEnvVar = clientSecretEnvVar;
  }

  /// <summary>
  /// Lädt die Client Secrets aus Environment Variablen.
  /// Erwartet, dass die Variablen YT_CLIENT_ID und YT_CLIENT_SECRET gesetzt sind.
  /// </summary>
  /// <returns>ClientSecrets Objekt bei Erfolg, sonst null.</returns>
  public ClientSecrets? GetClientSecrets()
  {
    string? clientId = Environment.GetEnvironmentVariable(_clientIdEnvVar);
    string? clientSecret = Environment.GetEnvironmentVariable(_clientSecretEnvVar);

    if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
    {
      Console.Error.WriteLine(
        $"Fehler: Umgebungsvariablen {_clientIdEnvVar} oder {_clientSecretEnvVar} sind nicht gesetzt.");
      return null;
    }

    Console.WriteLine("GitHub Client Secrets erfolgreich aus Environment Variablen geladen.");
    return new ClientSecrets
    {
      ClientId = clientId,
      ClientSecret = clientSecret
    };
  }
}