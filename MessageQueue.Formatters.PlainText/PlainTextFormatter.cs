using System.Text;

namespace KM.MessageQueue.Formatters.PlainText
{
    public sealed class PlainTextFormatter : IMessageFormatter<string>
    {
        public byte[] MessageToBytes(string message) =>
            Encoding.UTF8.GetBytes(message);

        public string BytesToMessage(byte[] bytes) =>
            Encoding.UTF8.GetString(bytes);
    }
}
