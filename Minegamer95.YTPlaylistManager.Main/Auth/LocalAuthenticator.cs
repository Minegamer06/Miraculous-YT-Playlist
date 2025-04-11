using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;

namespace Minegamer95.YTPlaylistManager.Main.Auth;

// --- Implementierung für lokale Entwicklung (mit FileDataStore) ---
public class LocalAuthenticator : IGoogleAuthenticator
{
    private readonly GoogleAuthorizationCodeFlow _flow;
    private readonly string _userId = "user"; // Kann bei Bedarf konfigurierbar gemacht werden

    /// <summary>
    /// Initialisiert den LocalAuthenticator.
    /// </summary>
    /// <param name="clientSecrets">Die geladenen Client Secrets.</param>
    /// <param name="scopes">Die benötigten API-Scopes.</param>
    /// <param name="dataStorePath">Pfad für den FileDataStore (Standard: "YouTube.Auth.Store").</param>
    /// <param name="dataStorePathIsFull"></param>
    public LocalAuthenticator(ClientSecrets clientSecrets, IEnumerable<string> scopes, string dataStorePath = "YouTube.Auth.Store", bool dataStorePathIsFull = true)
    {
        _flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = clientSecrets,
            Scopes = scopes,
            // Wichtig: FileDataStore speichert den Refresh Token lokal
            DataStore = new FileDataStore(dataStorePath, dataStorePathIsFull)
        });
    }

    /// <summary>
    /// Führt die lokale Authentifizierung durch (lädt oder startet manuellen Flow).
    /// </summary>
    public async Task<UserCredential?> AuthorizeAsync(CancellationToken cancellationToken)
    {
        UserCredential? credential = null;
        try
        {
            // Versuche, gespeicherte Credentials zu laden
            var token = await _flow.LoadTokenAsync(_userId, cancellationToken);

            if (token != null && !string.IsNullOrEmpty(token.RefreshToken))
            {
                credential = new UserCredential(_flow, _userId, token);
                Console.WriteLine("Local: Gespeicherte Anmeldeinformationen werden geprüft...");

                // Prüfen, ob der Access Token noch gültig ist oder erneuert werden muss
                if (credential.Token.IsStale)
                {
                    Console.WriteLine("Local: Access Token ist abgelaufen, versuche Erneuerung...");
                    if (await credential.RefreshTokenAsync(cancellationToken))
                    {
                        Console.WriteLine("Local: Access Token erfolgreich erneuert.");
                    }
                    else
                    {
                        // Wenn die Erneuerung fehlschlägt, könnte der Refresh Token ungültig sein.
                        await Console.Error.WriteLineAsync("Local: Fehler: Access Token konnte nicht erneuert werden. Refresh Token möglicherweise ungültig. Starte manuellen Flow.");
                        credential = null; // Erzwinge neuen manuellen Flow
                    }
                }
                else
                {
                    Console.WriteLine("Local: Gespeicherte Anmeldeinformationen sind gültig.");
                }
            }
            else
            {
                 Console.WriteLine("Local: Keine gültigen gespeicherten Anmeldeinformationen gefunden oder Refresh Token fehlt.");
            }
        }
        catch (Exception ex)
        {
             await Console.Error.WriteLineAsync($"Local: Fehler beim Laden oder Prüfen des Tokens: {ex.Message}. Starte manuellen Flow.");
             credential = null; // Sicherstellen, dass der manuelle Flow gestartet wird
        }


        // Wenn keine gültigen Credentials gefunden oder geladen werden konnten
        if (credential != null) return credential; // Kann null sein, wenn die manuelle Autorisierung fehlschlägt
        Console.WriteLine("Local: Starte manuellen Autorisierungs-Flow...");
        credential = await AuthorizeManuallyAsync(cancellationToken);

        return credential; // Kann null sein, wenn die manuelle Autorisierung fehlschlägt
    }

    /// <summary>
    /// Führt den manuellen Copy/Paste-Flow durch.
    /// </summary>
    private async Task<UserCredential?> AuthorizeManuallyAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Erstelle die Autorisierungs-URL
            var authUrl = _flow.CreateAuthorizationCodeRequest("urn:ietf:wg:oauth:2.0:oob").Build();

            Console.WriteLine("\nBitte öffne die folgende URL in deinem Browser:");
            Console.WriteLine(authUrl);
            Console.WriteLine("\nNachdem du die Berechtigung erteilt hast, kopiere den angezeigten Code und füge ihn hier ein:");
            Console.Write("Autorisierungscode: ");
            string? authCode = Console.ReadLine()?.Trim(); // Lese den Code von der Konsole

            if (string.IsNullOrWhiteSpace(authCode))
            {
                await Console.Error.WriteLineAsync("Local: Fehler: Kein Autorisierungscode eingegeben.");
                return null;
            }

            // Tausche den Code gegen Tokens (Access & Refresh Token)
            TokenResponse token = await _flow.ExchangeCodeForTokenAsync(
                _userId,
                authCode,
                "urn:ietf:wg:oauth:2.0:oob", // Redirect-URI für Copy/Paste
                cancellationToken);

            Console.WriteLine("\nLocal: Autorisierung erfolgreich! Tokens wurden empfangen und gespeichert.");

            // Gib den wichtigen Refresh Token aus (falls ein neuer generiert wurde)
            if (!string.IsNullOrEmpty(token.RefreshToken))
            {
                Console.WriteLine($"\n-----------------------------------------------------------------------");
                Console.WriteLine($"WICHTIG: Dein Refresh Token wurde in '{((FileDataStore)_flow.DataStore).FolderPath}' gespeichert.");
                Console.WriteLine($"Für GitHub Actions benötigst du diesen Refresh Token. Er lautet:");
                Console.WriteLine(token.RefreshToken);
                Console.WriteLine($"-----------------------------------------------------------------------");
            } else {
                 Console.WriteLine("\nLocal: Hinweis: Es wurde kein neuer Refresh Token vom Server zurückgegeben (wahrscheinlich weil bereits einer existiert).");
            }

            // Erstelle das Credential-Objekt
            return new UserCredential(_flow, _userId, token);
        }
        catch (TokenResponseException ex)
        {
            await Console.Error.WriteLineAsync($"\nLocal: Fehler beim Austauschen des Autorisierungscodes: {ex.Error.Error} - {ex.Error.ErrorDescription}");
            return null;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"\nLocal: Ein unerwarteter Fehler ist beim Token-Austausch aufgetreten: {ex.Message}");
            return null;
        }
    }
}