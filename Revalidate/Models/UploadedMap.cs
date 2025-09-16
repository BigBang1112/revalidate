using GBX.NET;
using GBX.NET.Engines.Game;
using Revalidate.Entities;

namespace Revalidate.Models;

public sealed record UploadedMap(Gbx<CGameCtnChallenge> MapGbx, byte[] Sha256, FileEntity File);
