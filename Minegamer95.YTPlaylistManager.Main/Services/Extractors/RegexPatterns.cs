namespace Minegamer95.YTPlaylistManager.Main.Services.Extractors;

public static class RegexPatterns
{
  /// <summary>
  /// Erfasst Staffel- und Episodennummern in typischen Benennungsmustern mehrerer Sprachen.
  /// 
  /// Erlaubte Begriffe für die Staffel:<br/>
  ///   - "Season" (Englisch)<br/>
  ///   - "Staffel" (Deutsch)<br/>
  ///   - "Saison" (Französisch)<br/>
  ///   - "Temporada" (Spanisch)<br/>
  ///
  /// Erlaubte Begriffe für die Episode:<br/>
  ///   - "Episode" (Englisch)<br/>
  ///   - "Folge" (Deutsch)<br/>
  ///   - "Épisode" (Französisch)<br/>
  ///   - "Episodio" (Spanisch)<br/>
  ///   - "Capítulo" (Spanisch, inkl. Akzentvarianten)<br/>
  ///<br/>
  /// Das Muster ignoriert dabei Groß-/Kleinschreibung und erlaubt flexible Trennzeichen 
  /// wie Leerzeichen, Bindestriche oder Doppelpunkte zwischen Staffel- und Episodenangabe.
  /// </summary>
  public const string SeasonEpisodePattern =
    @"(?i)\b(?:S|Season|Staffel|Saison|Temporada)\s*(?<season>\d{1,2})[\s\-:]*\b(?:E|Episode|Folge|Épisode|Episodio|Cap[ií]tulo)\s*(?<episode>\d{1,3})";  
}