using System.Collections.Generic;
using System.Text.Json;
using System.Text;
using System;
using Serilog.Events;

namespace Seq.Forwarder.Util
{
    public class MessageExtracter
    {
        public long? timeUnixNano { get; set; }
        public long? observedTimeUnixNano { get; set; }
        public string? severityText { get; set; }
        public string? MessageTemplate { get; set; }
        public string? traceId { get; set; }
        public string? spanId { get; set; }
        public string? Body { get; set; }
        public Dictionary<string, object>? Attributes { get; set; }

        public MessageExtracter(byte[] jsonData) 
        {
            if (jsonData == null || jsonData.Length == 0)
                throw new ArgumentException("JSON data cannot be null or empty", nameof(jsonData));

            // Convert byte array to string
            string jsonString = Encoding.UTF8.GetString(jsonData);

            // Parse the JSON string
            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                var root = doc.RootElement;

                // Extract Timestamp
                if (root.TryGetProperty("Timestamp", out JsonElement timestampElement))
                {
                    string? timestampString = timestampElement.GetString();
                    if (!string.IsNullOrEmpty(timestampString) && DateTime.TryParse(timestampString, out DateTime parsedDateTime))
                    {
                        timeUnixNano = ToUnixTimeNanoseconds(parsedDateTime);
                    }
                }

                // Extract Level (Handle missing field by using nullable string)
                if (root.TryGetProperty("Level", out JsonElement levelElement))
                {
                    if (String.IsNullOrEmpty(levelElement.GetString()))
                        severityText = "Information";
                    else
                        severityText = levelElement.GetString();
                }

                // Extract MessageTemplate and assign to Body
                if (root.TryGetProperty("MessageTemplate", out JsonElement messageTemplateElement))
                {
                    MessageTemplate = messageTemplateElement.GetString();
                    Body = MessageTemplate;
                }

                // Extract additional attributes
                //if (root.TryGetProperty("Attributes", out JsonElement attributesElement))
                //{
                //    Attributes = new Dictionary<string, object>();

                //    foreach (JsonProperty attribute in attributesElement.EnumerateObject())
                //    {
                //        Attributes.Add(attribute.Name, ParseAttributeValue(attribute.Value));
                //    }
                //}
                Attributes = new Dictionary<string, object>
                {
                    { "example.attribute", "example value" }
                };
            }

            this.traceId = Guid.NewGuid().ToString("N").ToUpper();
            this.spanId = Guid.NewGuid().ToSpanId();
            this.observedTimeUnixNano = timeUnixNano;
        }

        private long ToUnixTimeNanoseconds(DateTime dateTime)
        {
            // Convert to UTC to avoid time zone issues
            dateTime = dateTime.ToUniversalTime();

            // Calculate Unix time in nanoseconds
            long unixTimeNanoseconds = (dateTime.Ticks - DateTime.UnixEpoch.Ticks) * 100;

            return unixTimeNanoseconds;
        }

        // Helper method to parse attribute values based on their JSON type
        private object ParseAttributeValue(JsonElement valueElement)
        {
            return valueElement.ValueKind switch
            {
                JsonValueKind.String => valueElement.GetString() ?? string.Empty,
                JsonValueKind.Number => valueElement.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Object => valueElement.ToString() ?? string.Empty, // Convert objects to string if necessary
                JsonValueKind.Array => valueElement.ToString() ?? string.Empty,  // Convert arrays to string if necessary
                _ => valueElement.ToString()
            };
        }
    }
}
