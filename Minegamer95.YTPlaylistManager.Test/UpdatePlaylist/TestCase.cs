namespace Minegamer95.YTPlaylistManager.Test.UpdatePlaylist;

public class TestCase
{
  public string Name { get; set; } = string.Empty;
  public List<string> Target { get; set; }
  public List<string> Source { get; set; }
  
  public int AddCount { get; set; } = -1;
  public int RemoveCount { get; set; } = -1;
  public int UpdateCount { get; set; } = -1;

  public SimulatedPlaylistService GetPlaylistService()
  {
    long pos = 0;
    var items = Source.Select(x => (x, pos++)).ToList();
    return new SimulatedPlaylistService(items);
  }
}
