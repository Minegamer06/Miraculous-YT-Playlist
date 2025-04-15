namespace Minegamer95.YTPlaylistManager.Main;

public class QuotaManager : IQuotaManager
{
  private int _remainingPoints;
  public int RemainingPoints => _remainingPoints;

  private static readonly Lazy<QuotaManager> _instance =
    new Lazy<QuotaManager>(() => new QuotaManager(8000));

  public static QuotaManager Instance => _instance.Value;

  private QuotaManager(int initialPoints)
  {
    _remainingPoints = initialPoints;
  }

  public bool CanExecute(int cost)
  {
    return _remainingPoints >= cost;
  }

  public void Deduct(int cost)
  {
    if (!CanExecute(cost))
    {
      throw new InvalidOperationException("Nicht genügend API-Kontingent vorhanden.");
    }
    _remainingPoints -= cost;
    Console.WriteLine($"Verbleibende Punkte: {_remainingPoints}");
  }
  
  public bool DeductIfAvailable(int cost)
  {
    if (!CanExecute(cost))
    {
      return false;
    }
    _remainingPoints -= cost;
    return true;
  }
}