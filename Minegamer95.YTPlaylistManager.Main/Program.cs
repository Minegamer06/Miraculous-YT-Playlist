using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows; // Hinzugefügt für den manuellen Flow
using Google.Apis.Auth.OAuth2.Responses; // Hinzugefügt für TokenResponseException
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Minegamer95.YTPlaylistManager.Main.Model; // Dein Model-Namespace
using Minegamer95.YTPlaylistManager.Main.Services; // Dein Service-Namespace (für PlaylistUpdater)

// Definiere Konstanten direkt im Top-Level-Bereich
const string ClientSecretsPath = "M:\\PC\\Projects\\Miraculous-YT-Playlist\\Minegamer95.YTPlaylistManager.Main\\client_secret.json"; // Dein Pfad
// Stelle sicher, dass die Scopes-Variable korrekt definiert ist
string[] Scopes = { YouTubeService.Scope.Youtube }; // Youtube reicht auch

Console.WriteLine("YouTube Playlist Manager - Einmalige lokale Autorisierung");
Console.WriteLine("======================================================\n");

 // --- Konfiguration ---
 // Passe diese IDs an deine Bedürfnisse an
string sourceChannelId = "UC2D5z27XfUz5DfU7kQAehiA";               // ID des Kanals, von dem Videos geholt werden sollen (wenn gewünscht)
string sourcePlaylistId = "PLDGA85Y1JsmNIo_mKcsWvzKAFRPah6mqT";     // ID der Playlist, von der Videos geholt werden sollen (wenn gewünscht)
string targetPlaylistId_Season1 = "PL_TARGET_SEASON_1"; // ID der Ziel-Playlist für Staffel 1 // <-- ANPASSEN!
// string targetPlaylistId_Season2 = "PL_TARGET_SEASON_2"; // ID der Ziel-Playlist für Staffel 2
// ... weitere Staffeln ...

UserCredential credential = null;
ClientSecrets clientSecrets;

try
{
    // --- Authentifizierung (Manuelle Copy/Paste Methode) ---

    // Lade die Client Secrets aus der Datei
    using (var stream = new FileStream(ClientSecretsPath, FileMode.Open, FileAccess.Read))
    {
        clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;
    }

    if (clientSecrets == null || string.IsNullOrEmpty(clientSecrets.ClientId) || string.IsNullOrEmpty(clientSecrets.ClientSecret))
    {
         Console.Error.WriteLine($"Fehler: Konnte Client Secrets nicht laden oder sie sind unvollständig in '{ClientSecretsPath}'.");
         return; // Beenden
    }

    // Erstelle den Authorization Code Flow
    var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
    {
        ClientSecrets = clientSecrets,
        Scopes = Scopes,
        // Wichtig: Der DataStore speichert den Refresh Token nach erfolgreicher Autorisierung
        DataStore = new FileDataStore("YouTube.Auth.Store", true)
    });

    // Versuche zuerst, gespeicherte Credentials zu laden
    var fileToken = await flow.LoadTokenAsync("user", CancellationToken.None);
    credential = new UserCredential(flow, "user", fileToken);
    
    // Wenn keine gültigen Credentials gefunden wurden ODER der Refresh Token fehlt
    if (credential == null || credential.Token == null || string.IsNullOrEmpty(credential.Token.RefreshToken))
    {
        Console.WriteLine("Keine gültigen gespeicherten Anmeldeinformationen gefunden oder Refresh Token fehlt. Starte manuellen Autorisierungs-Flow...");

        // Erstelle die Autorisierungs-URL für den manuellen Copy/Paste Flow
        var authUrl = flow.CreateAuthorizationCodeRequest("urn:ietf:wg:oauth:2.0:oob").Build();

        Console.WriteLine("\nBitte öffne die folgende URL in deinem Browser:");
        Console.WriteLine(authUrl);
        Console.WriteLine("\nNachdem du die Berechtigung erteilt hast, kopiere den angezeigten Code und füge ihn hier ein:");
        Console.Write("Autorisierungscode: ");
        string authCode = Console.ReadLine(); // Lese den Code von der Konsole

        if (string.IsNullOrWhiteSpace(authCode))
        {
             Console.Error.WriteLine("Fehler: Kein Autorisierungscode eingegeben.");
             return;
        }

        try
        {
            // Tausche den Code gegen Tokens (Access & Refresh Token)
            TokenResponse token = await flow.ExchangeCodeForTokenAsync(
                "user", // User Identifier
                authCode,
                "urn:ietf:wg:oauth:2.0:oob", // Redirect-URI für Copy/Paste
                CancellationToken.None);

             // Erstelle das Credential-Objekt
             credential = new UserCredential(flow, "user", token);
             Console.WriteLine("\nAutorisierung erfolgreich! Tokens wurden empfangen und gespeichert.");

             // Gib den wichtigen Refresh Token aus
             if (!string.IsNullOrEmpty(token.RefreshToken))
             {
                 Console.WriteLine($"\n-----------------------------------------------------------------------");
                 Console.WriteLine($"WICHTIG: Dein Refresh Token wurde in '{((FileDataStore)flow.DataStore).FolderPath}' gespeichert.");
                 Console.WriteLine($"Für GitHub Actions benötigst du diesen Refresh Token. Er lautet:");
                 Console.WriteLine(token.RefreshToken);
                 Console.WriteLine($"-----------------------------------------------------------------------");
             } else {
                  Console.WriteLine("\nHinweis: Es wurde kein neuer Refresh Token vom Server zurückgegeben.");
             }
        }
        catch (TokenResponseException ex)
        {
             Console.Error.WriteLine($"\nFehler beim Austauschen des Autorisierungscodes: {ex.Error.Error} - {ex.Error.ErrorDescription}");
             return;
        }
        catch (Exception ex)
        {
             Console.Error.WriteLine($"\nEin unerwarteter Fehler ist beim Token-Austausch aufgetreten: {ex.Message}");
             return;
        }
    }
    else
    {
        Console.WriteLine("Gültige Anmeldeinformationen aus dem Speicher geladen.");
        // Optional: Prüfen, ob der Access Token noch gültig ist oder erneuert werden muss
         if (credential.Token.IsExpired(Google.Apis.Util.SystemClock.Default))
         {
             Console.WriteLine("Access Token ist abgelaufen, versuche Erneuerung...");
             if (await credential.RefreshTokenAsync(CancellationToken.None))
             {
                 Console.WriteLine("Access Token erfolgreich erneuert.");
             }
             else
             {
                 // Wenn die Erneuerung fehlschlägt, könnte der Refresh Token ungültig sein.
                 // Man könnte hier den manuellen Flow erneut starten oder abbrechen.
                 Console.Error.WriteLine("Fehler: Access Token konnte nicht erneuert werden. Refresh Token möglicherweise ungültig.");
                 // Hier evtl. credential auf null setzen, um den Rest zu überspringen?
                 credential = null;
             }
         }
    }

    // --- Ab hier geht dein normaler Code weiter, wenn die Authentifizierung erfolgreich war ---
    if (credential != null)
    {
        Console.WriteLine("\nAuthentifizierung erfolgreich abgeschlossen. Starte Playlist-Updater...");

        // --- Updater-Instanz erstellen ---
        // Stelle sicher, dass PlaylistUpdater einen Konstruktor hat, der UserCredential akzeptiert
        var updater = new PlaylistUpdater(credential); // Verwende deine PlaylistUpdater-Klasse

        // --- Videos sammeln ---
        List<VideoInfo> allAvailableVideos = new List<VideoInfo>(); // Verwende deine VideoInfo-Klasse

        // Beispiel: Videos vom Kanal holen
        if (!string.IsNullOrEmpty(sourceChannelId) && sourceChannelId != "UC_CHANNEL_ID") // Prüfe auf Standardwert
        {
             var channelVideos = await updater.GetVideosFromChannelAsync(sourceChannelId);
             allAvailableVideos.AddRange(channelVideos);
        }

         // Beispiel: Videos aus einer Quell-Playlist holen
         if (!string.IsNullOrEmpty(sourcePlaylistId) && sourcePlaylistId != "PL_SOURCE_PLAYLIST_ID") // Prüfe auf Standardwert
         {
             var playlistVideos = await updater.GetVideosFromPlaylistAsync(sourcePlaylistId);
             // Optional: Doppelte Einträge vermeiden
             allAvailableVideos.AddRange(playlistVideos);
             allAvailableVideos = allAvailableVideos.GroupBy(v => v.VideoId).Select(g => g.First()).ToList();
         }

        Console.WriteLine($"\nInsgesamt {allAvailableVideos.Count} einzigartige Videos gesammelt.");

        // --- Staffeln verarbeiten ---

        // Beispiel für Staffel 1
        uint seasonToUpdate = 1;
        if (!string.IsNullOrEmpty(targetPlaylistId_Season1) && targetPlaylistId_Season1 != "PL_TARGET_SEASON_1") // Prüfe auf Standardwert
        {
            Console.WriteLine($"\nVerarbeite Staffel {seasonToUpdate} für Playlist {targetPlaylistId_Season1}...");
            // Stelle sicher, dass FilterAndSortVideosForSeason existiert und List<string> zurückgibt
            List<string> season1VideoIds = updater.FilterAndSortVideosForSeason(allAvailableVideos, seasonToUpdate);
            // Stelle sicher, dass UpdateTargetPlaylistAsync existiert
            await updater.UpdateTargetPlaylistAsync(targetPlaylistId_Season1, season1VideoIds);
        } else {
             Console.WriteLine($"\nÜberspringe Staffel {seasonToUpdate}, da keine gültige Ziel-Playlist ID ('{targetPlaylistId_Season1}') angegeben wurde.");
        }

        // Füge hier Logik für weitere Staffeln hinzu, falls targetPlaylistId_Season2 etc. gesetzt sind...


        Console.WriteLine("\nAlle Aktionen abgeschlossen.");
    } else {
         Console.Error.WriteLine("\nKonnte keine gültigen Anmeldeinformationen erhalten. Playlist-Update wird nicht gestartet.");
    }

}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine($"Fehler: Die Client-Secret-Datei '{ClientSecretsPath}' wurde nicht gefunden.");
    Console.Error.WriteLine(ex.Message);
}
catch (AggregateException ex) // Fängt Fehler aus asynchronen Aufrufen
{
     foreach (var innerEx in ex.InnerExceptions)
     {
          Console.Error.WriteLine($"Ein Fehler ist aufgetreten: {innerEx.Message}");
          Console.Error.WriteLine(innerEx.StackTrace); // Mehr Details bei Bedarf
     }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace); // Mehr Details bei Bedarf
}

Console.WriteLine("\nDrücke eine Taste zum Beenden (nur bei lokaler Ausführung relevant).");
Console.ReadKey(); // Behalte dies für die lokale Ausführung bei