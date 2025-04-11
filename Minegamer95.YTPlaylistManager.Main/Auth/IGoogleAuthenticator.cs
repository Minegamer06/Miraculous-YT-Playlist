using Google.Apis.Auth.OAuth2;

namespace Minegamer95.YTPlaylistManager.Main.Auth;

// --- Interface für verschiedene Authentifizierungsstrategien ---
public interface IGoogleAuthenticator
{
  /// <summary>
  /// Führt die Authentifizierung durch und gibt die UserCredentials zurück.
  /// </summary>
  /// <param name="cancellationToken">Cancellation Token.</param>
  /// <returns>UserCredential bei Erfolg, sonst null.</returns>
  Task<UserCredential?> AuthorizeAsync(CancellationToken cancellationToken);
}