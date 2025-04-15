using Xunit;
using Minegamer95.YTPlaylistManager.Main.Model;
using Minegamer95.YTPlaylistManager.Main.Services.Extractors;

namespace Minegamer95.YTPlaylistManager.Test;

public class RegexPatternsTests
{
  private static readonly RegexSeasonEpisodeExtractor SeasonEpisodePattern = new RegexSeasonEpisodeExtractor();

  public static IEnumerable<object[]> ValidTestData =>
    ValidTestDataMixed.Concat(ValidTestDataMiraculousDe)
      .Concat(ValidTestDataMiraculousEn)
      .Concat(ValidTestDataMiraculousFr);

  public static IEnumerable<object[]> InValidTestData =>
    InValidTestDataMiraculousDe
      .Concat(InValidTestDataMiraculousEn)
      .Concat(InValidTestDataMiraculousFr);

  #region TestDataValid

  private static IEnumerable<object[]> ValidTestDataMixed =>
  [
    ["English Kurzname", "S2 E1", 2, 1],
    ["English", "Season 2 Episode 5", 2, 5],
    ["Deutsch", "Staffel 3 Folge 12", 3, 12],
    ["Denglisch Nr.1", "Staffel 3 Episode 14", 3, 14],
    ["Denglisch Nr.2", "Staffel 3 E 14", 3, 14],
    ["Denglisch Nr.3", "Season 3 Folge 14", 3, 14],
    ["Französisch", "Saison 1 Épisode 8", 1, 8],
    ["Spanisch", "Temporada 4 Episodio 20", 4, 20],
    ["Spanisch mit Akzent", "Temporada 2 Capítulo 15", 2, 15],
    ["Mixed Case", "sEaSoN 5 ePiSoDe 10", 5, 10],
  ];

  private static IEnumerable<object[]> ValidTestDataMiraculousDe =>
  [
    ["MiraculousDE", "MIRACULOUS | 🐞 CHLOÉ SIEHT GELB 🐾 | GANZE FOLGE ▶️ Staffel 4 Folge 8", 4, 8],
    ["MiraculousDE", "MIRACULOUS | 🐞 DER COLLECTOR KEHRT ZURÜCK 🐾 | GANZE FOLGE ▶️ Staffel 4 Folge 9", 4, 9],
    ["MiraculousDE", "MIRACULOUS | 🐞 DER GROẞE STREIT 🐾 | GANZE FOLGE ▶️ Staffel 4 Folge 12", 4, 12],
    ["MiraculousDE", "MIRACULOUS | 🐞 DER KAMPF DER MIRACULOUS - TEIL 1 🐾 | GANZE FOLGE ▶️ Staffel 3 Folge 25", 3, 25],
    ["MiraculousDE", "MIRACULOUS | 🐞 DER KAMPF DER MIRACULOUS - TEIL 2 🐾 | GANZE FOLGE ▶️ Staffel 3 Folge 26", 3, 26],
    ["MiraculousDE", "MIRACULOUS | 🐞 EINE FALLE FÜR LADYBUG 🐾 | GANZE FOLGE ▶️ Staffel 4 Folge 14", 4, 14],
    ["MiraculousDE", "MIRACULOUS | 🐞 LÜGEN 🐾 | GANZE FOLGE ▶️ Staffel 4 Folge 2", 4, 2],
    ["MiraculousDE", "MIRACULOUS | 🐞 WILLKOMMEN IN PARIS 🐾 | GANZE FOLGE ▶️ Staffel 4 Folge 7", 4, 7],
    ["MiraculousDE", "MIRACULOUS | 🐞 ZU VIELE GEHEIMNISSE 🐾 | GANZE FOLGE ▶️ Staffel 4 Folge 3", 4, 3],
    ["MiraculousDE", "MIRACULOUS | 🐞 OPTIGAMI 🐾 | GANZE FOLGE ▶️ Staffel 4 Folge 13", 4, 13],
    ["MiraculousDE", "MIRACULOUS | 🐞 MR. PIGEON 72 🐾 | GANZE FOLGE ▶️ Staffel 4 Folge 4", 4, 4],
  ];

  private static IEnumerable<object[]> ValidTestDataMiraculousEn =>
  [
    ["MiraculousEN", "MIRACULOUS | 🐞 PENALTEAM 🐾 | FULL EPISODE ▶️ Season 4 Episode 24", 4, 24],
    ["MiraculousEN", "MIRACULOUS | 🐞 REFLEKDOLL 🐾 | FULL EPISODE ▶️ Season 3 Episode 5", 3, 5],
    ["MiraculousEN", "MIRACULOUS | 🐞 SILENCER 🐾 | FULL EPISODE ▶️ Season 3 Episode 7", 3, 7],
    ["MiraculousEN", "MIRACULOUS | 🐞 SOLE CRUSHER 🐾 | FULL EPISODE ▶️ Season 4 Episode 7", 4, 7],
    ["MiraculousEN", "MIRACULOUS | 🐞 STORMY WEATHER 2 🐾 | FULL EPISODE ▶️ Season 3 Episode 17", 3, 17],
    ["MiraculousEN", "MIRACULOUS | 🐞 TIMETAGGER 🐾 | FULL EPISODE ▶️ Season 3 Episode 19", 3, 19],
    ["MiraculousEN", "MIRACULOUS | 🐞 TRUTH 🐾 | FULL EPISODE ▶️ Season 4 Episode 1", 4, 1],
    ["MiraculousEN", "MIRACULOUS | 🐞 WEREDAD 🐾 | FULL EPISODE ▶️ Season 3 Episode 6", 3, 6]
  ];

  private static IEnumerable<object[]> ValidTestDataMiraculousFr =>
  [
    ["MiraculousFR", "MIRACULOUS | 🐞 PIRKELL 🐾 | Episode entier ▶️ Saison 4 Episode 7", 4, 7],
    ["MiraculousFR", "MIRACULOUS | 🐞 QUEEN BANANA 🐾 | Episode entier ▶️ Saison 4 Episode 8", 4, 8],
    ["MiraculousFR", "MIRACULOUS | 🐞 SANGSURE 🐾 | Episode entier ▶️ Saison 4 Episode 10", 4, 10],
    ["MiraculousFR", "MIRACULOUS | 🐞 SENTIBULLEUR 🐾 | Episode entier ▶️ Saison 4 Episode 14", 4, 14]
  ];

  #endregion TestDataValid

  #region TestDataInvalid

  private static IEnumerable<object[]> InValidTestDataMiraculousDe
  {
    get
    {
      foreach (var item in new[]
               {
                 "MIRACULOUS | 🌎 TAG DER ERDE - KOMPILATION ♻️ | STAFFEL 5 | Geschichten von Ladybug und Cat Noir",
                 "MIRACULOUS | 🐞 ADRIEN 🔝 | STAFFEL 5 | Geschichten von Ladybug und Cat Noir",
                 "MIRACULOUS | 🐞 CAT NOIR 🔝 | STAFFEL 5 | Geschichten von Ladybug und Cat Noir",
                 "MIRACULOUS | 🐞 CHLOE 🔝 | STAFFEL 5 | Geschichten von Ladybug und Cat Noir",
                 "MIRACULOUS | 🐞 Kompilation 27 🐾 | GANZE FOLGE ▶️ STAFFEL 3",
                 "MIRACULOUS | 🐞 Kompilation 28 🐾 | GANZE FOLGE ▶️ STAFFEL 3",
                 "MIRACULOUS | 🐞 Kompilation 29 🐾 | GANZE FOLGE ▶️ [FALSCHES SPIEL-MARINETTE UNTER VERDACHT] STAFFEL 3",
                 "MIRACULOUS | 🐞 Kompilation 30 🐾 | GANZE FOLGE ▶️ [DER KAMPF DER MIRACULOUS] STAFFEL 3",
                 "MIRACULOUS | 🐞 LADYBUG 🔝 | STAFFEL 5 | Geschichten von Ladybug und Cat Noir",
                 "MIRACULOUS | 🐞 MARINETTE 🔝 | STAFFEL 5 | Geschichten von Ladybug und Cat Noir",
                 "MIRACULOUS | 🐞 NINO 🔝 | STAFFEL 5 | Geschichten von Ladybug und Cat Noir",
                 "MIRACULOUS | 🐞 PARTY 🔝 | STAFFEL 5 | Geschichten von Ladybug und Cat Noir",
                 "MIRACULOUS | 💖 ADRIENETTE 🐞 | Die 10 besten Momente | 10. Jubiläum 💫",
               })
      {
        yield return ["MiraculousDE", item];
      }
    }
  }

  private static IEnumerable<object[]> InValidTestDataMiraculousFr
  {
    get
    {
      foreach (var item in new[]
               {
                 "MIRACULOUS | 🌎 JOUR DE LA TERRE - COMPILATION ♻️ | SAISON 5 | Les aventures de Ladybug et Chat Noir",
                 "MIRACULOUS | 🐞 Compilation 30 🐾 ÉPISODES ENTIERS ▶️ [MANGEAMOUR - MIRACLE QUEEN] SAISON 3",
                 "MIRACULOUS | 🐞 DADDYCOP - Akumatisation 🐾 | SAISON 6 | Les aventures de Ladybug et Chat Noir",
                 "MIRACULOUS | 🐞 DESSINATRISTE - Akumatisation 🐾 | SAISON 6 | Les aventures de Ladybug et Chat Noir",
                 "MIRACULOUS | 🐞 FÊTE 🔝 | SAISON 5 | Les aventures de Ladybug et Chat Noir",
                 "MIRACULOUS | 🐞 NINO 🔝 | SAISON 5 | Les aventures de Ladybug et Chat Noir",
                 "MIRACULOUS | 🐞 PAPYS GAROUS - Akumatisation 🐾 | SAISON 6 | Les aventures de Ladybug et Chat Noir",
                 "MIRACULOUS | 🐞 PAPYS GAROUS - Les grands-parents d'Adrien 🐾 | SAISON 6",
                 "MIRACULOUS | 🐞 PAPYS GAROUS - TEASER 🐾 | SAISON 6",
                 "MIRACULOUS | 🐞 REVELATOR - TEASER 🐾 | SAISON 6",
                 "MIRACULOUS | 🐞 SUBLIMATION - Akumatisation 🐾 | SAISON 6 | Les aventures de Ladybug et Chat Noir",
                 "MIRACULOUS | 💖 ADRIENETTE 🐞 | Les 10 meilleurs moments | 10ème anniversaire 💫",
               })
      {
        yield return ["MiraculousDE", item];
      }
    }
  }

  private static IEnumerable<object[]> InValidTestDataMiraculousEn
  {
    get
    {
      foreach (var item in new[]
               {
                 "MIRACULOUS | 🌎 EARTH DAY - ACTION ♻️ | SEASON 5 | Tales of Ladybug & Cat Noir",
                 "MIRACULOUS | 🐞 ADRIEN 🔝 | SEASON 5 | Tales of Ladybug and Cat Noir",
                 "MIRACULOUS | 🐞 AKUMATIZED - Compilation 1 😈 | SEASON 6 | Tales of Ladybug & Cat Noir",
                 "MIRACULOUS | 🐞 CAT NOIR 🔝 | SEASON 5 | Tales of Ladybug and Cat Noir",
                 "MIRACULOUS | 🐞 COMPILATION 1 - SEASON 6 🐾 | Tales of Ladybug & Cat Noir",
                 "MIRACULOUS | 🐞 Compilation 🐾 | FULL EPISODES ▶️ [Feast - Gamer 2.0 - Stormy Weather 2] Season 3",
                 "MIRACULOUS | 🐞 DADDYCOP - Akumatized 🐾 | SEASON 6 | Tales of Ladybug & Cat Noir",
                 "MIRACULOUS | 🐞 FAMILY 🔝 | SEASON 5 | Tales of Ladybug and Cat Noir",
                 "MIRACULOUS | 🐞 LADYBUG 🔝 | SEASON 5 | Tales of Ladybug and Cat Noir",
                 "MIRACULOUS | 🐞 MARINETTE 🔝 | SEASON 5 | Tales of Ladybug and Cat Noir",
                 "MIRACULOUS | 🐞 NEW OFFICIAL TRAILER - SEASON 6 🐾 | Tales of Ladybug & Cat Noir",
                 "MIRACULOUS | 🐞 WEREPAPAS - Adrien & Nathalie 🐾 | SEASON 6 | Tales of Ladybug & Cat Noir",
                 "MIRACULOUS | 💖 ADRIENETTE 🐞 | Best clips from Season 1 to 6 | 10th anniversary ⭐️",
                 "MIRACULOUS | 🔝 FELIX 🐞 | SEASON 5 | Tales of Ladybug & Cat Noir",
                 "THE ULTIMATE RECAP [S1–5] 💥 | MIRACULOUS 🐞 | The Entire Story So Far Before Season 6 💫",
                 "VALENTINE'S DAY - Compilation 2025 SEASON 1 TO 6 | Miraculous - Tales of Ladybug and Cat Noir",
                 "💕 VALENTINE'S DAY - Compilation 2025 💌 SEASON 1 TO 6 ❗ | Miraculous - Tales of Ladybug and Cat Noir",
               })
      {
        yield return ["Miraculous EN", item[0]];
      }
    }
  }

  #endregion TestDataInvalid

  [Theory]
  [MemberData(nameof(ValidTestData))]
  public void SeasonEpisodePattern_ShouldExtractCorrectSeasonAndEpisode(
    string testCaseName,
    string title,
    int expectedSeason,
    int expectedEpisode)
  {
    // Arrange
    var videoInfo = new VideoInfo { Title = title };

    // Act
    var episodeInfo = SeasonEpisodePattern.ExtractSeasonEpisode(videoInfo);

    // Assert
    Assert.True(episodeInfo.Season.HasValue, $"[{testCaseName}] Season should have value for title: '{title}'");
    Assert.Equal(expectedSeason, episodeInfo.Season.Value);

    Assert.True(episodeInfo.Episode.HasValue, $"[{testCaseName}] Episode should have value for title: '{title}'");
    Assert.Equal(expectedEpisode, episodeInfo.Episode.Value);
  }

  [Theory]
  [MemberData(nameof(InValidTestData))]
  public void SeasonEpisodePattern_ShouldNotExtractFromInvalidInput(string testCaseName, string title)
  {
    // Arrange
    var videoInfo = new VideoInfo { Title = title };

    // Act
    var episodeInfo = SeasonEpisodePattern.ExtractSeasonEpisode(videoInfo);

    // Assert
    Assert.False(episodeInfo.Episode.HasValue, $"[{testCaseName}] Episode should be null for invalid title: '{title}'");
    Assert.False(episodeInfo.Season.HasValue, $"[{testCaseName}] Season should be null for invalid title: '{title}'");
  }
}