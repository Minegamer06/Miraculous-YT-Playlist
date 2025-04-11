using Google.Apis.Auth.OAuth2;

namespace Minegamer95.YTPlaylistManager.Main.Auth;

public static class AuthHelper
{
  /// <summary>
  /// Lädt die Client Secrets aus der angegebenen Datei.
  /// </summary>
  /// <param name="path">Pfad zur client_secrets.json Datei.</param>
  /// <returns>ClientSecrets Objekt bei Erfolg, sonst null.</returns>
  private static ClientSecrets? LoadClientSecrets(string path)
  {
    if (!File.Exists(path))
    {
      Console.Error.WriteLine($"Fehler: Client Secrets Datei nicht gefunden unter '{path}'.");
      return null;
    }

    try
    {
      using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
      var secrets = GoogleClientSecrets.FromStream(stream).Secrets;
      if (secrets == null || string.IsNullOrEmpty(secrets.ClientId) || string.IsNullOrEmpty(secrets.ClientSecret))
      {
        Console.Error.WriteLine(
          $"Fehler: Client Secrets konnten nicht geladen werden oder sind unvollständig in '{path}'. Prüfe die Datei.");
        return null;
      }

      Console.WriteLine($"Client Secrets erfolgreich aus '{path}' geladen.");
      return secrets;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Fehler beim Lesen oder Parsen der Client Secrets aus '{path}': {ex.Message}");
      return null;
    }
  }

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

    // 1. Client Secrets laden
    var clientSecrets = LoadClientSecrets(clientSecretsPath);
    if (clientSecrets == null)
    {
      Console.WriteLine("Authentifizierung abgebrochen, da Client Secrets nicht geladen werden konnten.");
      return null;
    }

    IGoogleAuthenticator authenticator;

    // 2. Auswahl der Authentifizierungsmethode
    //    Versuche, den Refresh Token aus einer Umgebungsvariable zu lesen.
    string? refreshTokenFromEnv = Environment.GetEnvironmentVariable("GOOGLE_REFRESH_TOKEN");

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