using System.Text;
using System.Threading.Tasks;

namespace KM.MessageQueue.Formatters.StringToBytes
{
    public sealed class StringToBytesFormatter : IMessageFormatter<string, byte[]>
    {
        public Task<byte[]> FormatMessage(string message) =>
            Task.FromResult(Encoding.UTF8.GetBytes(message));

        public Task<string> RevertMessage(byte[] message) =>
            Task.FromResult(Encoding.UTF8.GetString(message));
    }
}
