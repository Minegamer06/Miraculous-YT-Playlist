using Google.Apis.YouTube.v3;

namespace Minegamer95.YTPlaylistManager.Main.Services.Actions;

public interface IPlaylistAction : ICostType
{
  // Führt die eigentliche API-Aktion aus
  Task ExecuteAsync(IPlaylistInteraction service, bool dryRun);

  // Beschreibt die Aktion für Logging/Debugging
  string Describe();
}