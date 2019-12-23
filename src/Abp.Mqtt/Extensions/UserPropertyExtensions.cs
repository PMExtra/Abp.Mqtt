using System;
using System.Linq;

// ReSharper disable once CheckNamespace
// ReSharper disable once IdentifierTypo
namespace MQTTnet
{
    public static class UserPropertyExtensions
    {
        public static string GetUserProperty(this MqttApplicationMessage message, string propertyName, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            return message.UserProperties.SingleOrDefault(up => up.Name.Equals(propertyName, comparisonType))?.Value;
        }
    }
}
