using Revalidate.Api;
using System.Text.Json.Serialization;

namespace Revalidate;

[JsonSerializable(typeof(ValidationRequest))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;