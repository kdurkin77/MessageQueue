using System.Text;

namespace KM.MessageQueue.Formatters.StringToBytes
{
    public sealed class StringToBytesFormatter : IMessageFormatter<string, byte[]>
    {
        public byte[] FormatMessage(string message) =>
            Encoding.UTF8.GetBytes(message);

        public string RevertMessage(byte[] message) =>
            Encoding.UTF8.GetString(message);
    }
}
