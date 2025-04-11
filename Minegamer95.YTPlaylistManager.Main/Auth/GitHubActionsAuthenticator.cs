using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;

namespace Minegamer95.YTPlaylistManager.Main.Auth;

// --- Implementierung für GitHub Actions (mit Refresh Token aus Umgebungsvariable/Secret) ---
public class GitHubActionsAuthenticator : IGoogleAuthenticator
{
    private readonly ClientSecrets _clientSecrets;
    private readonly IEnumerable<string> _scopes;
    private readonly string? _refreshToken;
    private readonly string _userId = "user"; // Kann bei Bedarf konfigurierbar gemacht werden

    /// <summary>
    /// Initialisiert den GitHubActionsAuthenticator.
    /// </summary>
    /// <param name="clientSecrets">Die geladenen Client Secrets.</param>
    /// <param name="scopes">Die benötigten API-Scopes.</param>
    /// <param name="refreshToken">Der Refresh Token (z.B. aus GitHub Secrets).</param>
    public GitHubActionsAuthenticator(ClientSecrets clientSecrets, IEnumerable<string> scopes, string? refreshToken)
    {
        if (clientSecrets == null) throw new ArgumentNullException(nameof(clientSecrets));
        if (scopes == null) throw new ArgumentNullException(nameof(scopes));
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            // Dieser Fall sollte idealerweise durch die Logik in Program.Main abgefangen werden,
            // aber eine zusätzliche Prüfung hier schadet nicht.
            throw new ArgumentException("Refresh token darf nicht leer sein für GitHub Actions Authentifizierung.", nameof(refreshToken));
        }
        _clientSecrets = clientSecrets;
        _scopes = scopes;
        _refreshToken = refreshToken;
    }

    /// <summary>
    /// Führt die Authentifizierung mittels des bereitgestellten Refresh Tokens durch.
    /// </summary>
    public async Task<UserCredential?> AuthorizeAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("GitHub Actions: Versuche Autorisierung mit bereitgestelltem Refresh Token...");

        // Erstelle den Flow OHNE DataStore, da wir den Token manuell bereitstellen
        // und nichts speichern wollen/können in der CI/CD Umgebung.
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = _clientSecrets,
            Scopes = _scopes,
            // NullDataStore verwenden oder DataStore-Property ganz weglassen.
            // Verhindert Versuche, Tokens im Dateisystem zu speichern.
            DataStore = new NullDataStore()
        });

        // Erstelle ein TokenResponse-Objekt nur mit dem Refresh Token
        var token = new TokenResponse { RefreshToken = _refreshToken };

        // Erstelle das UserCredential-Objekt direkt
        var credential = new UserCredential(flow, _userId, token);

        // Versuche SOFORT, den Access Token zu erneuern.
        // Dies validiert den Refresh Token und holt einen gültigen Access Token.
        Console.WriteLine("GitHub Actions: Versuche Access Token zu erneuern...");
        bool refreshed = false;
        try
        {
             refreshed = await credential.RefreshTokenAsync(cancellationToken);
        }
        catch (TokenResponseException ex)
        {
             Console.Error.WriteLine($"GitHub Actions: Fehler beim Erneuern des Tokens (TokenResponseException): {ex.Error.Error} - {ex.Error.ErrorDescription}");
             Console.Error.WriteLine("Mögliche Ursachen: Refresh Token ist ungültig, abgelaufen oder wurde widerrufen.");
             return null; // Fehler anzeigen
        }
        catch (Exception ex)
        {
             Console.Error.WriteLine($"GitHub Actions: Unerwarteter Fehler beim Erneuern des Tokens: {ex.Message}");
             return null; // Fehler anzeigen
        }


        if (refreshed)
        {
            Console.WriteLine("GitHub Actions: Access Token erfolgreich erneuert.");
            return credential;
        }
        else
        {
            // Sollte durch die Exception-Behandlung oben abgedeckt sein, aber zur Sicherheit:
            Console.Error.WriteLine("GitHub Actions: Fehler: Access Token konnte nicht erneuert werden (RefreshTokenAsync gab false zurück).");
            return null; // Fehler anzeigen
        }
    }
}
