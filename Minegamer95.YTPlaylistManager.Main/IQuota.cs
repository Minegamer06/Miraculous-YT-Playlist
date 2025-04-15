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
  public static int GetCost<T>(this IEnumerable<T> types, IQuotaCost cost) where T : ICostType
  {
    return types.Sum(type => cost.GetCost(type.GetCostType));
  }

}