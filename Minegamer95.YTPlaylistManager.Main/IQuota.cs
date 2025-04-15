namespace Minegamer95.YTPlaylistManager.Main;

public interface IQuotaManager
{
  bool CanExecute(int cost);
  void Deduct(int cost);
  bool DeductIfAvailable(int cost);
  int RemainingPoints { get; }
}

public interface IQuotaCost
{
  int GetCost(QuotaType type);
}

public interface ICostType
{
  QuotaType GetCostType { get; }
}

public enum QuotaType
{
  Get,
  Insert,
  Delete,
  Update,
}

public static class QuotaTypeExtensions
{
  public static int GetCost<T>(this IEnumerable<T> types) where T : IQuotaCost
  {
    int totalCost = 0;
    foreach (var type in types)
    {
      //totalCost += type.GetType
    }
    return totalCost;
  }

}