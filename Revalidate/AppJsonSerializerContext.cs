using Revalidate.Models;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(ExeBuild[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
