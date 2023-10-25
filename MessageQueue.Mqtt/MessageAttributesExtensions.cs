using MQTTnet.Protocol;
using System;

namespace KM.MessageQueue
{
    public static class MessageAttributesExtensions
    {
        /// <summary>
        /// Gets the QualityOfService in the message attributes
        /// </summary>
        /// <param name="messageAttributes"></param>
        /// <returns></returns>
        public static MqttQualityOfServiceLevel QualityOfService(this MessageAttributes messageAttributes)
        {
            if (!messageAttributes._attributes.TryGetValue("QualityOfService", out var value))
            {
                return MqttQualityOfServiceLevel.ExactlyOnce;
            }

            var stringValue = value?.ToString();
            if (stringValue is null)
            {
                return MqttQualityOfServiceLevel.ExactlyOnce;
            }

            if (!Enum.TryParse<MqttQualityOfServiceLevel>(stringValue, out var enumValue))
            {
                return MqttQualityOfServiceLevel.ExactlyOnce;
            }

            return enumValue;
        }

        /// <summary>
        /// Sets the QualityOfService in the message attributes
        /// </summary>
        /// <param name="messageAttributes"></param>
        /// <param name="value"></param>
        public static void QualityOfService(this MessageAttributes messageAttributes, MqttQualityOfServiceLevel value)
        {
            messageAttributes._attributes["QualityOfService"] = value.ToString();
        }

        /// <summary>
        /// Get the value of RetainMessage in the message attributes
        /// </summary>
        /// <param name="messageAttributes"></param>
        /// <returns></returns>
        public static bool RetainMessage(this MessageAttributes messageAttributes)
        {
            if (!messageAttributes._attributes.TryGetValue("RetainMessage", out var value))
            {
                return false;
            }

            var stringValue = value?.ToString();
            if (stringValue is null)
            {
                return false;
            }

            if (!bool.TryParse(stringValue, out var boolValue))
            {
                return false;
            }

            return boolValue;
        }

        /// <summary>
        /// Sets the value of RetainMessage in the message attributes
        /// </summary>
        /// <param name="messageAttributes"></param>
        /// <param name="value"></param>
        public static void RetainMessage(this MessageAttributes messageAttributes, bool value)
        {
            messageAttributes._attributes["RetainMessage"] = value;
        }
    }
}
