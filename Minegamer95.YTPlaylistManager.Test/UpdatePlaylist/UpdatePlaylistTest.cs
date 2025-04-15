using Google.Apis.YouTube.v3.Data;
using Minegamer95.YTPlaylistManager.Main.Services;

namespace Minegamer95.YTPlaylistManager.Test.UpdatePlaylist;

public class UpdatePlaylistTest
{
  private readonly SimulatedPlaylistService _simulatedPlaylistService;

  private readonly List<string> _targetOrder =
  [
    "1a6e5-Tj6vI",
    "MXFQ3L-yQtA",
    "QT0RJhsuVfA",
    "ByftEwigC5I",
    "KYq2km_1Z3k",
    "6vXzGbJnuIw",
    "Oqg7hmZkHh0",
    "_Uy4IcElxN4",
    "mjdSJvH6Fs4",
    "D4XnbL4oIsU",
    "3EWc60ADkFI",
    "_H8QNtcXbI8",
    "DTLq6NYVWEc",
    "GBNjT3z_dUo",
    "RG28Ofw8hXE",
    "iDk3qcXWYzE",
    "VHwODRfrF54",
    "IILvmV66l4c",
    "UH7bnjZWoME",
    "DJOM4_Pq_SI",
    "o24wF4JPzg4",
    "eOa0pRLvc0k",
    "HgoERh3diko",
    "Xqy3hCvPlkY",
    "yUB1iy4vHdc",
    "xPSI2NEEq9s",
  ];

  public UpdatePlaylistTest()
  {
    var items = new List<(string videoId, long position)>
    {
      ("1a6e5-Tj6vI", 0),
      ("QT0RJhsuVfA", 1),
      ("Oqg7hmZkHh0", 2),
      ("D4XnbL4oIsU", 3),
      ("MXFQ3L-yQtA", 4),
      ("_Uy4IcElxN4", 5),
      ("3EWc60ADkFI", 6),
      ("DTLq6NYVWEc", 7),
      ("GBNjT3z_dUo", 8),
      ("iDk3qcXWYzE", 9),
      ("DJOM4_Pq_SI", 10),
      ("ByftEwigC5I", 11),
      ("xPSI2NEEq9s", 12),
      ("VHwODRfrF54", 13),
      ("eOa0pRLvc0k", 14),
      ("9mgiEei1Zrc", 15),
      ("kdt1KSjJStg", 16),
      ("vcqWPCt2TmE", 17),
      ("o92P-iC6vns", 18),
      ("fSFCh6EbxmI", 19),
      ("o92P-iC6vns", 20),
    };
    _simulatedPlaylistService = new SimulatedPlaylistService(items);
  }

  [Fact]
  public async Task UpdateOrderIsCorrect()
  {
    var updater = new PlaylistUpdater(_simulatedPlaylistService); // Verwende deine PlaylistUpdater-Klasse
    await updater.UpdateTargetPlaylistAsync("test", _targetOrder);
    var final = _simulatedPlaylistService.GetFinalSimulatedPositions();
    Assert.Equal(_targetOrder.Count, final.Count);

    Assert.All(final, item =>
    {
      var target = _targetOrder.IndexOf(item.Key);
      Assert.Equal(target, item.Value);
      Assert.NotEqual(-1, target);
    });
  }
}