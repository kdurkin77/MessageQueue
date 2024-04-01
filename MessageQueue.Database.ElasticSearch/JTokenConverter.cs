using Newtonsoft.Json.Linq;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KM.MessageQueue.Database.ElasticSearch
{
    internal class JTokenConverter : JsonConverter<JToken>
    {
        private void WriteStringValueOrNull(Utf8JsonWriter writer, string? value)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value);
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
                case JTokenType.Array:
                    {
                        var jarray = (JArray)value;
                        writer.WriteStartArray();
                        if (jarray.HasValues)
                        {
                            foreach (var item in jarray.Children())
                            {
                                Write(writer, item, options);
                            }
                        }
                        writer.WriteEndArray();
                    }
                    break;

                case JTokenType.Boolean:
                    {
                        var b = ((JValue)value).Value as bool?;
                        if (b is null)
                        {
                            writer.WriteNullValue();
                        }
                        else
                        {
                            writer.WriteBooleanValue(b.Value);
                        }
                    }
                    break;

                case JTokenType.Bytes:
                    {
                        var bytes = ((JValue)value).Value as byte[];
                        writer.WriteBase64StringValue(bytes);
                    }
                    break;

                case JTokenType.Date:
                    {
                        var date = ((JValue)value).Value;
                        if (date is null)
                        {
                            writer.WriteNullValue();
                        }
                        else
                        {
                            switch (date)
                            {
                                case DateTimeOffset dto:
                                    WriteStringValueOrNull(writer, dto.ToString("o"));
                                    break;
                                case DateTime dt:
                                    WriteStringValueOrNull(writer, dt.ToString("o"));
                                    break;
                                default:
                                    throw new NotSupportedException($"Unknown underlying JTokenType.Date type {date.GetType().FullName}");
                            }
                        }
                    }
                    break;

                case JTokenType.Float:
                    {
                        writer.WriteRawValue(value.ToString());
                    }
                    break;

                case JTokenType.Guid:
                    {
                        WriteStringValueOrNull(writer, value.ToString());
                    }
                    break;

                case JTokenType.Integer:
                    {
                        writer.WriteRawValue(value.ToString());
                    }
                    break;

                //case JTokenType.None:
                //    {
                //        throw new NotImplementedException();
                //    }
                //    break;

                case JTokenType.Null:
                    {
                        writer.WriteNullValue();
                    }
                    break;

                case JTokenType.Object:
                    {
                        var obj = (JObject)value;
                        writer.WriteStartObject();
                        if (obj.HasValues)
                        {
                            foreach (var item in obj.Children())
                            {
                                Write(writer, item, options);
                            }
                        }
                        writer.WriteEndObject();
                    }
                    break;

                case JTokenType.Property:
                    {
                        var property = (JProperty)value;
                        writer.WritePropertyName(property.Name);
                        Write(writer, property.Value, options);
                    }
                    break;

                case JTokenType.Raw:
                    {
                        writer.WriteRawValue(value.ToString());
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
                        WriteStringValueOrNull(writer, value.ToString());
                    }
                    break;

                case JTokenType.Undefined:
                    {
                        writer.WriteNullValue();
                    }
                    break;

                case JTokenType.Uri:
                    {
                        var uri = ((JValue)value).Value as Uri;
                        WriteStringValueOrNull(writer, uri?.ToString());
                    }
                    break;

                default:
                    throw new NotSupportedException($"Unknown JTokenType {value.Type}");
            };
        }
    }
}
