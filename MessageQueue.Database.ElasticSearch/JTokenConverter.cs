using Newtonsoft.Json.Linq;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KM.MessageQueue.Database.ElasticSearch
{
    internal class JTokenConverter : JsonConverter<JToken>
    {
        private void WriteStringValueOrNull(Utf8JsonWriter writer, object? value)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue((string)value);
            }
        }

        public override JToken? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JToken.Parse(JsonDocument.ParseValue(ref reader).RootElement.GetRawText());
        }

        public override void Write(Utf8JsonWriter writer, JToken value, JsonSerializerOptions options)
        {
            switch (value.Type)
            {
                case JTokenType.Bytes:
                    {
                        var bytes = ((JValue)value).Value as byte[];
                        writer.WriteBase64StringValue(bytes);
                    }
                    break;

                case JTokenType.Date:
                    {
                        var date = ((JValue)value).Value;
                        switch (date)
                        {
                            case DateTimeOffset dto:
                                WriteStringValueOrNull(writer, dto.ToString("o"));
                                break;
                            case DateTime dt:
                                WriteStringValueOrNull(writer, dt.ToString("o"));
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }
                    break;

                case JTokenType.Guid:
                    {
                        var guid = ((JValue)value).Value as Guid?;
                        WriteStringValueOrNull(writer, guid?.ToString());
                    }
                    break;

                case JTokenType.Undefined:
                    {
                        writer.WriteNullValue();
                    }
                    break;

                case JTokenType.String:
                    {
                        var str = ((JValue)value).Value as string;
                        WriteStringValueOrNull(writer, str);
                    }
                    break;

                case JTokenType.TimeSpan:
                    {
                        var timespan = ((JValue)value).Value as TimeSpan?;
                        WriteStringValueOrNull(writer, timespan?.ToString());
                    }
                    break;

                case JTokenType.Uri:
                    {
                        var uri = ((JValue)value).Value as Uri;
                        WriteStringValueOrNull(writer, uri?.ToString());
                    }
                    break;

                default:
                    writer.WriteRawValue(value.ToString());
                    break;
            };
        }
    }
}
