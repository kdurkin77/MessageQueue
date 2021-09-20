using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace KM.MessageQueue.Formatters.DoubleJsonGZip
{
    public sealed class DoubleJsonGZipFormatter<TMessage> : IMessageFormatter<TMessage>
    {
        public byte[] MessageToBytes(TMessage message)
        {
            using (var outputStream = new MemoryStream())
            {
                var initialJson = JsonConvert.SerializeObject(message);
                var secondJson = JsonConvert.SerializeObject(initialJson);
                using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(secondJson)))
                {
                    using (var zipStream = new GZipStream(outputStream, CompressionMode.Compress, true))
                    {
                        inputStream.CopyTo(zipStream);
                    }

                    return outputStream.ToArray();
                }
            }
        }

        public TMessage BytesToMessage(byte[] bytes)
        {
            using (var inputStream = new MemoryStream(bytes))
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var zipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                    {
                        zipStream.CopyTo(outputStream);
                    }

                    var outString = Encoding.UTF8.GetString(outputStream.ToArray());
                    var firstDeserialize = JsonConvert.DeserializeObject<string>(outString) 
                        ?? throw new Exception($"Unable to convert bytes to type {typeof(TMessage)}");

                    return JsonConvert.DeserializeObject<TMessage>(firstDeserialize) 
                        ?? throw new Exception($"Unable to convert bytes to type {typeof(TMessage)}");
                }
            }
        }
    }
}
