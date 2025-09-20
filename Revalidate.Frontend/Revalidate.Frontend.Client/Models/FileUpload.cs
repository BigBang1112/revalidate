using Revalidate.Api;
using Revalidate.Frontend.Client.Enums;

namespace Revalidate.Frontend.Client.Models;

public sealed record FileUpload(string FileName, byte[] Data, DateTimeOffset? LastModified, GBX.NET.Gbx? Gbx)
{
    public ValidationResult? Result { get; set; }
    public ValidationDistroResult? SelectedDistro { get; set; }
    public ResultSection Section { get; set; }
}
