using TmEssentials;

namespace Revalidate.Models;

public sealed record LeaderboardRecord(int Rank, Guid? AccountId, string Login, TimeInt32 Time, string Url, string FileName);