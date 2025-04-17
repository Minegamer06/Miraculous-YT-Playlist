using Google.Apis.YouTube.v3.Data;
using Minegamer95.YTPlaylistManager.Main.Services;

namespace Minegamer95.YTPlaylistManager.Test.UpdatePlaylist;

public class UpdatePlaylistTest
{
  public static IEnumerable<object[]> TestDataWrapper => TestData.Select(testCase => new object[] { testCase });

  private static readonly List<TestCase> TestData = [
    new()
    {
        Name = "Lösche ersten Playlist-Eintrag aus Target"
      , Source = ["_bAX0Cyco1Q","p5sFGwcMVGE","ioYM-RQnua4","MpLFgMxiixg","_-1XdcVc8CE","J8wNUWHj4Qc","wold1namm-g","Dc8PHdtTbSk","_E3_xHQXm44","jKUllSICWrY","QLsRcsJEXqE","o1LP5aFtH2U","N76Y1KZucvQ","ZSYsvX3BwBg","S3cZBnOi0BQ","PfI3Azft9L0","ssUnxAuj98w","zngDilsCsN4","3m6exxR-sLM","P1rqEin-7Gw","yo-qPKYoIK0","lnaXridzVZo","RvK1o3QDV_0","NO6KJyC9rM8","Gy9lwl9yjIM","c45ugOVqSV4"]
      , Target  = ["p5sFGwcMVGE","ioYM-RQnua4","MpLFgMxiixg","_-1XdcVc8CE","J8wNUWHj4Qc","wold1namm-g","Dc8PHdtTbSk","_E3_xHQXm44","jKUllSICWrY","QLsRcsJEXqE","o1LP5aFtH2U","N76Y1KZucvQ","ZSYsvX3BwBg","S3cZBnOi0BQ","PfI3Azft9L0","ssUnxAuj98w","zngDilsCsN4","3m6exxR-sLM","P1rqEin-7Gw","yo-qPKYoIK0","lnaXridzVZo","RvK1o3QDV_0","NO6KJyC9rM8","Gy9lwl9yjIM","c45ugOVqSV4"]
      , RemoveCount = 1
      , UpdateCount = 0
      , AddCount = 0
    }
  ];
  
  
  [Theory]
  [MemberData(nameof(TestDataWrapper))]
  public async Task UpdateOrderIsCorrect(TestCase testCase)
  {
    var service = testCase.GetPlaylistService();
    var updater = new PlaylistUpdater(service); // Verwende deine PlaylistUpdater-Klasse
    await updater.UpdateTargetPlaylistAsync("test", testCase.Target);
    var final = service.GetFinalSimulatedPositions();
    Assert.Equal(testCase.Target.Count, final.Count);

    Assert.All(final, item =>
    {
      var target = testCase.Target.IndexOf(item.Key);
      Assert.Equal(target, item.Value);
      Assert.NotEqual(-1, target);
    });
  }

  [Theory]
  [MemberData(nameof(TestDataWrapper))]
  public async Task CalcChangPlanCheck(TestCase testCase)
  {
    var service = testCase.GetPlaylistService();
    var source = await service.ListItemsAsync("");
    var updater = new PlaylistUpdater(service);
    var changePlan = updater.CalculateChangePlan(source, testCase.Target, "test");
    Assert.Equal(testCase.AddCount, changePlan.ToInsert.Count);
    Assert.Equal(testCase.RemoveCount, changePlan.ToDelete.Count);
    Assert.Equal(testCase.UpdateCount, changePlan.ToUpdate.Count);
  }
}