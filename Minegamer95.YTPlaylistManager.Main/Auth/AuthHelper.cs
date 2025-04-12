using Google.Apis.Auth.OAuth2;
using Minegamer95.YTPlaylistManager.Main.Auth.ClientSecretProvider;

namespace Minegamer95.YTPlaylistManager.Main.Auth;

public static class AuthHelper
{
  /// <summary>
  /// Führt die Authentifizierung basierend auf der Umgebung (Environment Variable) durch.
  /// Lädt Client Secrets, wählt den passenden Authenticator und führt AuthorizeAsync aus.
  /// </summary>
  /// <param name="clientSecretsPath">Pfad zur Client Secrets Datei.</param>
  /// <param name="scopes">Die benötigten API-Scopes.</param>
  /// <param name="cancellationToken">Cancellation Token.</param>
  /// <param name="dataStorePath"></param>
  /// <returns>UserCredential bei Erfolg, sonst null.</returns>
  public static async Task<UserCredential?> AuthenticateBasedOnEnvironmentAsync(
    string clientSecretsPath,
    IEnumerable<string> scopes,
    CancellationToken cancellationToken,
    string? dataStorePath = "YouTube.Auth.Store")
  {
    Console.WriteLine("Starte Authentifizierungsprozess...");

    ClientSecrets? clientSecrets;
    IGoogleAuthenticator authenticator;


    string? refreshTokenFromEnv = Environment.GetEnvironmentVariable("GOOGLE_REFRESH_TOKEN");
    if (string.IsNullOrWhiteSpace(refreshTokenFromEnv))
    {
      Console.WriteLine("Umgebungsvariable GOOGLE_REFRESH_TOKEN nicht gefunden. Verwende Client Secrets datei.");
      clientSecrets = new FileClientSecretProvider(clientSecretsPath).GetClientSecrets();
    }
    else
    {
      Console.WriteLine("Umgebungsvariable GOOGLE_REFRESH_TOKEN gefunden. Verwende GitHub Actions Authentifizierung.");
      clientSecrets = new GitHubClientSecretsProvider().GetClientSecrets();
    }
    
    if (clientSecrets == null)
    {
      Console.WriteLine("Authentifizierung abgebrochen, da Client Secrets nicht geladen werden konnten.");
      return null;
    }

    if (!string.IsNullOrWhiteSpace(refreshTokenFromEnv))
    {
      Console.WriteLine("Umgebungsvariable GOOGLE_REFRESH_TOKEN gefunden. Verwende GitHub Actions Authentifizierung.");
      authenticator = new GitHubActionsAuthenticator(clientSecrets, scopes, refreshTokenFromEnv);
    }
    else
    {
      Console.WriteLine(
        "Keine Umgebungsvariable GOOGLE_REFRESH_TOKEN gefunden. Verwende lokale Authentifizierung (manueller Flow oder FileDataStore).");
      // Optional: Pfad zum DataStore anpassen, falls gewünscht
      // authenticator = new LocalAuthenticator(clientSecrets, scopes, "Pfad/zum/Custom/Store");
      if (!string.IsNullOrEmpty(dataStorePath))
      {
        authenticator = new LocalAuthenticator(clientSecrets, scopes, dataStorePath);
      }
      else
        return null;
    }

    // 3. Authentifizierung durchführen
    Console.WriteLine("Führe Authentifizierung aus...");
    try
    {
      return await authenticator.AuthorizeAsync(cancellationToken);
    }
    catch (Exception ex)
    {
      // Fängt unerwartete Fehler während des AuthorizeAsync-Aufrufs ab
      await Console.Error.WriteLineAsync(
        $"Ein schwerwiegender Fehler ist während der Authentifizierung aufgetreten: {ex.Message}");
      await Console.Error.WriteLineAsync(
        "Stelle sicher, dass die Netzwerkverbindung besteht und die Client Secrets korrekt sind.");
      return null;
    }
  }
}