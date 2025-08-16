using Revalidate.Models;
using System.Text.Json.Serialization;

namespace Revalidate;

[JsonSerializable(typeof(User))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;