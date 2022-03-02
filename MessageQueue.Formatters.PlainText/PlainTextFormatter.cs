using System.Text;

namespace KM.MessageQueue.Formatters.PlainText
{
    public sealed class PlainTextFormatter : IMessageFormatter<string, byte[]>
    {
        public byte[] FormatMessage(string message) =>
            Encoding.UTF8.GetBytes(message);

        public string RevertMessage(byte[] bytes) =>
            Encoding.UTF8.GetString(bytes);
    }
}
