using Xunit;
using Minegamer95.YTPlaylistManager.Main.Model;
using Minegamer95.YTPlaylistManager.Main.Services.Extractors;

namespace Minegamer95.YTPlaylistManager.Test;

public class RegexPatternsTests
{
    private static readonly RegexSeasonEpisodeExtractor _seasonEpisodePattern = new RegexSeasonEpisodeExtractor();
    
    [Fact]
    public void SeasonEpisodePattern_ShortNameEnglish()
    {
        var input = new VideoInfo { Title = "Season 2 Episode 1" };
        var episodeInfo = _seasonEpisodePattern.ExtractSeasonEpisode(input);

        Assert.Equal(1, episodeInfo.Episode!.Value);
        Assert.Equal(2, episodeInfo.Season!.Value);
    }
    
    [Fact]
    public void SeasonEpisodePattern_MatchesEnglishSeasonAndEpisode()
    {
        var videoInfo = new VideoInfo { Title = "Season 2 Episode 5" };
        var episodeInfo = _seasonEpisodePattern.ExtractSeasonEpisode(videoInfo);

        Assert.Equal(5, episodeInfo.Episode!.Value);
        Assert.Equal(2, episodeInfo.Season!.Value);
    }

    [Fact]
    public void SeasonEpisodePattern_MatchesGermanStaffelAndFolge()
    {
        var videoInfo = new VideoInfo { Title = "Staffel 3 Folge 12" };
        var episodeInfo = _seasonEpisodePattern.ExtractSeasonEpisode(videoInfo);

        Assert.Equal(12, episodeInfo.Episode!.Value);
        Assert.Equal(3, episodeInfo.Season!.Value);
    }

    [Fact]
    public void SeasonEpisodePattern_MatchesFrenchSaisonAndEpisode()
    {
        var videoInfo = new VideoInfo { Title = "Saison 1 Épisode 8" };
        var episodeInfo = _seasonEpisodePattern.ExtractSeasonEpisode(videoInfo);

        Assert.Equal(8, episodeInfo.Episode!.Value);
        Assert.Equal(1, episodeInfo.Season!.Value);
    }

    [Fact]
    public void SeasonEpisodePattern_MatchesSpanishTemporadaAndEpisodio()
    {
        var videoInfo = new VideoInfo { Title = "Temporada 4 Episodio 20" };
        var episodeInfo = _seasonEpisodePattern.ExtractSeasonEpisode(videoInfo);

        Assert.Equal(20, episodeInfo.Episode!.Value);
        Assert.Equal(4, episodeInfo.Season!.Value);
    }

    [Fact]
    public void SeasonEpisodePattern_MatchesSpanishCapituloWithAccent()
    {
        var videoInfo = new VideoInfo { Title = "Temporada 2 Capítulo 15" };
        var episodeInfo = _seasonEpisodePattern.ExtractSeasonEpisode(videoInfo);

        Assert.Equal(15, episodeInfo.Episode!.Value);
        Assert.Equal(2, episodeInfo.Season!.Value);
    }

    [Fact]
    public void SeasonEpisodePattern_DoesNotMatchInvalidInput()
    {
        var videoInfo = new VideoInfo { Title = "Invalid Input" };
        var episodeInfo = _seasonEpisodePattern.ExtractSeasonEpisode(videoInfo);
        Assert.False(episodeInfo.Episode.HasValue);
        Assert.False(episodeInfo.Season.HasValue);
    }

    [Fact]
    public void SeasonEpisodePattern_IgnoresCaseSensitivity()
    {
        var videoInfo = new VideoInfo { Title = "season 5 episode 10" };
        var episodeInfo = _seasonEpisodePattern.ExtractSeasonEpisode(videoInfo);

        Assert.Equal(10, episodeInfo.Episode!.Value);
        Assert.Equal(5, episodeInfo.Season!.Value);
    }

    [Fact]
    public void SeasonEpisodePattern_AllowsFlexibleSeparators()
    {
        var videoInfo = new VideoInfo { Title = "Season 1 - Episode 3" };
        var episodeInfo = _seasonEpisodePattern.ExtractSeasonEpisode(videoInfo);

        Assert.Equal(3, episodeInfo.Episode!.Value);
        Assert.Equal(1, episodeInfo.Season!.Value);
    }
}