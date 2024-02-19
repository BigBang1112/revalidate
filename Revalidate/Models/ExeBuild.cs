namespace Revalidate.Models;

internal record ExeBuild(string Game, DateTimeOffset Date)
{
    /*public static ExeBuild FromReplay(CGameCtnReplayRecord replay)
    {
        return FromGhost(replay.GetGhosts().First()); // add checks
    }

    public static ExeBuild FromGhost(CGameCtnGhost ghost)
    {
        if (ghost.Validate_ExeVersion is null)
        {
            throw new Exception("ExeVersion is null");
        }

        var match = RegexUtils.ExeVersionRegex().Match(ghost.Validate_ExeVersion);

        if (!match.Success)
        {
            throw new Exception("ExeVersion is not in the correct format");
        }

        var game = match.Groups[1].Value;
        var buildDate = DateTimeOffset.ParseExact(match.Groups[2].Value, "yyyy-MM-dd_HH_mm", CultureInfo.InvariantCulture);

        return new(game, buildDate);
    }*/
}
