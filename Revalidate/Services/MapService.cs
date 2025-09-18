using GBX.NET.Engines.Game;
using GBX.NET.Imaging.SkiaSharp;
using ManiaAPI.NadeoAPI;
using Microsoft.EntityFrameworkCore;
using Revalidate.Api;
using Revalidate.Entities;
using Revalidate.Exceptions;
using Revalidate.Models;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using TmEssentials;

namespace Revalidate.Services;

public interface IMapService
{
    Task<MapEntity> GetOrCreateMapAsync(UploadedMap uploadedMap, CancellationToken cancellationToken);
    Task<MapEntity?> GetOrCreateMapAsync(GameVersion gameVersion, string mapUid, bool downloadExternally, CancellationToken cancellationToken);
}

public sealed partial class MapService : IMapService
{
    private readonly AppDbContext db;
    private readonly NadeoLiveServices nls;
    private readonly HttpClient http;

    public MapService(AppDbContext db, NadeoLiveServices nls, HttpClient http)
    {
        this.db = db;
        this.nls = nls;
        this.http = http;
    }

    public async Task<MapEntity> GetOrCreateMapAsync(UploadedMap uploadedMap, CancellationToken cancellationToken)
    {
        var map = await db.Maps.FirstOrDefaultAsync(x => x.Sha256 == uploadedMap.Sha256, cancellationToken);

        if (map is not null)
        {
            return map;
        }

        var mapNode = uploadedMap.MapGbx.Node;

        await using var thumbnailStream = new MemoryStream();
        mapNode.ExportThumbnail(thumbnailStream, SkiaSharp.SKEncodedImageFormat.Jpeg, 95);

        ValidateMapUidOrThrow(mapNode.MapUid);

        map = new MapEntity
        {
            MapUid = mapNode.MapUid,
            Sha256 = uploadedMap.Sha256,
            GameVersion = mapNode.GameVersion switch
            {
                GBX.NET.GameVersion.TM2020 => GameVersion.TM2020,
                GBX.NET.GameVersion.MP4 or GBX.NET.GameVersion.MP4 | GBX.NET.GameVersion.TM2020 => GameVersion.TM2,
                GBX.NET.GameVersion.TMF => GameVersion.TMF,
                _ => GameVersion.None
            },
            Name = mapNode.MapName,
            DeformattedName = TextFormatter.Deformat(mapNode.MapName),
            EnvironmentId = mapNode.GetEnvironment(),
            ModeId = mapNode.Mode.ToString(),
            AuthorTime = mapNode.AuthorTime,
            AuthorScore = mapNode.AuthorScore,
            NbLaps = mapNode.IsLapRace ? mapNode.NbLaps : 1,
            File = uploadedMap.File,
            Thumbnail = thumbnailStream.ToArray(),
            UserUploaded = true, // rn always but might change
        };

        await db.Maps.AddAsync(map, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return map;
    }

    private static void ValidateMapUidOrThrow(string mapUid)
    {
        if (string.IsNullOrWhiteSpace(mapUid))
        {
            throw new MapUidException("MapUid is null, empty, or whitespace. This is not allowed for validation.");
        }

        if (!MapUidRegex().IsMatch(mapUid))
        {
            throw new MapUidException($"MapUid '{mapUid}' is not valid. It must be 1-32 characters long and can only contain letters, numbers, and, underscores.");
        }
    }

    public async Task<MapEntity?> GetOrCreateMapAsync(GameVersion gameVersion, string mapUid, bool downloadExternally, CancellationToken cancellationToken)
    {
        // because sha256 is not known, there needs to be a check for a single source of trust via !UserUploaded+MapUid+GameVersion
        var map = await db.Maps
            .Where(x => x.GameVersion == gameVersion && x.MapUid == mapUid)
            .OrderBy(x => !x.UserUploaded) // prefer non-user-uploaded maps
            .ThenBy(x => x.Id) // then prefer older maps (arbitrary but stable)
            .FirstOrDefaultAsync(cancellationToken);

        if (map is not null || !downloadExternally)
        {
            return map;
        }

        // download from web services like tm2020, maniaplanet maps, tmx etc
        switch (gameVersion)
        {
            case GameVersion.TM2020:
                var tm2020Map = await nls.GetMapInfoAsync(mapUid, cancellationToken);

                if (tm2020Map is null)
                {
                    return null;
                }

                using (var mapResponse = await http.GetAsync(tm2020Map.DownloadUrl, cancellationToken))
                {
                    mapResponse.EnsureSuccessStatusCode();

                    var mapData = await mapResponse.Content.ReadAsByteArrayAsync(cancellationToken);
                    await using var mapStream = new MemoryStream(mapData);

                    var hashStartTimestamp = Stopwatch.GetTimestamp();

                    var sha256 = await SHA256.HashDataAsync(mapStream, cancellationToken);

                    mapStream.Position = 0;
                    var mapNode = GBX.NET.Gbx.ParseHeaderNode<CGameCtnChallenge>(mapStream);

                    await using var thumbnailStream = new MemoryStream();
                    mapNode.ExportThumbnail(thumbnailStream, SkiaSharp.SKEncodedImageFormat.Jpeg, 95);

                    var hashElapsedTime = Stopwatch.GetElapsedTime(hashStartTimestamp);
                    
                    map = new MapEntity
                    {
                        MapUid = tm2020Map.Uid,
                        Sha256 = sha256,
                        GameVersion = GameVersion.TM2020,
                        Name = tm2020Map.Name,
                        DeformattedName = tm2020Map.Name,
                        EnvironmentId = mapNode.GetEnvironment(),
                        ModeId = mapNode.Mode.ToString(),
                        AuthorTime = tm2020Map.AuthorTime,
                        AuthorScore = mapNode.AuthorScore,
                        NbLaps = mapNode.IsLapRace ? mapNode.NbLaps : 1,
                        File = await FileEntity.FromStreamAsync(mapStream, cancellationToken),
                        Thumbnail = thumbnailStream.ToArray(),
                        UserUploaded = false,
                        MapId = tm2020Map.MapId
                    };
                    await db.Maps.AddAsync(map, cancellationToken);
                    await db.SaveChangesAsync(cancellationToken);
                }
                break;
            case GameVersion.TM2:
                break;
        }

        return map;
    }

    [GeneratedRegex(@"^[_0-9a-zA-Z]{1,32}$")]
    private static partial Regex MapUidRegex();
}
