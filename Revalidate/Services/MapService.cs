using GBX.NET.Imaging.SkiaSharp;
using Microsoft.EntityFrameworkCore;
using Revalidate.Api;
using Revalidate.Entities;
using Revalidate.Models;
using TmEssentials;

namespace Revalidate.Services;

public interface IMapService
{
    Task<MapEntity> GetOrCreateMapAsync(UploadedMap uploadedMap, CancellationToken cancellationToken);
    Task<MapEntity> GetOrCreateMapAsync(GameVersion gameVersion, string mapUid, CancellationToken cancellationToken);
}

public sealed class MapService : IMapService
{
    private readonly AppDbContext db;

    public MapService(AppDbContext db)
    {
        this.db = db;
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

        map = new MapEntity
        {
            MapUid = mapNode.MapUid,
            Sha256 = uploadedMap.Sha256,
            GameVersion = mapNode.GameVersion switch
            {
                GBX.NET.GameVersion.TM2020 => Api.GameVersion.TM2020,
                GBX.NET.GameVersion.MP4 or GBX.NET.GameVersion.MP4 | GBX.NET.GameVersion.TM2020 => Api.GameVersion.TM2,
                GBX.NET.GameVersion.TMF => Api.GameVersion.TMF,
                _ => Api.GameVersion.None
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

    public async Task<MapEntity> GetOrCreateMapAsync(GameVersion gameVersion, string mapUid, CancellationToken cancellationToken)
    {
        // because sha256 is not known, there needs to be a check for a single source of trust via !UserUploaded+MapUid+GameVersion
        var map = await db.Maps
            .Where(x => x.GameVersion == gameVersion && x.MapUid == mapUid)
            .OrderBy(x => !x.UserUploaded) // prefer non-user-uploaded maps
            .ThenBy(x => x.Id) // then prefer older maps (arbitrary but stable)
            .FirstOrDefaultAsync(cancellationToken);

        if (map is not null)
        {
            return map;
        }

        // download from web services like tm2020, maniaplanet maps, tmx etc


        throw new NotImplementedException();
    }
}
