using Google.Apis.Auth.OAuth2;

namespace Minegamer95.YTPlaylistManager.Main.Auth.ClientSecretProvider;

public class FileClientSecretProvider : IClientSecretProvider
{
  private readonly string _path;

  public FileClientSecretProvider(string path)
  {
    _path = path;
  }


  public ClientSecrets? GetClientSecrets()
  {
    if (!File.Exists(_path))
    {
      Console.Error.WriteLine($"Fehler: Client Secrets Datei nicht gefunden unter '{_path}'.");
      return null;
    }

    try
    {
      using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read);
      var secrets = GoogleClientSecrets.FromStream(stream).Secrets;
      if (secrets == null || string.IsNullOrEmpty(secrets.ClientId) || string.IsNullOrEmpty(secrets.ClientSecret))
      {
        Console.Error.WriteLine(
          $"Fehler: Client Secrets konnten nicht geladen werden oder sind unvollständig in '{_path}'. Prüfe die Datei.");
        return null;
      }

      Console.WriteLine($"Client Secrets erfolgreich aus '{_path}' geladen.");
      return secrets;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Fehler beim Lesen oder Parsen der Client Secrets aus '{_path}'");
      return null;
    }
  }
}