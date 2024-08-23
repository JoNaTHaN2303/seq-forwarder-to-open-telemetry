using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Events;
using Serilog.Parsing;

namespace Seq.Forwarder.SerilogJsonConverter
{
    /// <summary>
    /// Reads a single JSON object representing a log event.
    /// </summary>
    public class LogEventReader
    {
        static readonly MessageTemplateParser Parser = new();
        static readonly Rendering[] NoRenderings = Array.Empty<Rendering>();
        readonly JsonSerializer _serializer;

        /// <summary>
        /// Construct a <see cref="LogEventReader"/> with an optional JSON serializer.
        /// </summary>
        /// <param name="serializer">If specified, a JSON serializer used when converting event documents.</param>
        public LogEventReader(JsonSerializer? serializer = null)
        {
            _serializer = serializer ?? CreateSerializer();
        }

        /// <summary>
        /// Parse a single log event from a JSON string.
        /// </summary>
        /// <param name="json">The event in JSON format.</param>
        /// <returns>The log event.</returns>
        /// <exception cref="InvalidDataException">The data format is invalid.</exception>
        public LogEvent ReadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON string cannot be null or whitespace.", nameof(json));

            object? data;
            try
            {
                using var reader = new JsonTextReader(new StringReader(json));
                data = _serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("The JSON data could not be deserialized.", ex);
            }

            if (data is not JObject jObject)
                throw new InvalidDataException("The JSON data is not a complete JSON object.");

            return ReadFromJObject(jObject);
        }

        /// <summary>
        /// Read a single log event from an already-deserialized JSON object.
        /// </summary>
        /// <param name="jObject">The deserialized JSON event.</param>
        /// <returns>The log event.</returns>
        /// <exception cref="InvalidDataException">The data format is invalid.</exception>
        private static LogEvent ReadFromJObject(JObject jObject)
        {
            return ReadFromJObject(1, jObject);
        }

        private static LogEvent ReadFromJObject(int lineNumber, JObject jObject)
        {
            var timestamp = GetRequiredTimestampField(lineNumber, jObject, JsonFields.Timestamp);

            string? messageTemplate;
            if (TryGetOptionalField(lineNumber, jObject, JsonFields.MessageTemplate, out var mt))
                messageTemplate = mt;
            else if (TryGetOptionalField(lineNumber, jObject, JsonFields.Message, out var m))
                messageTemplate = MessageTemplateSyntax.Escape(m);
            else
                messageTemplate = null;

            var level = LogEventLevel.Information;
            if (TryGetOptionalField(lineNumber, jObject, JsonFields.Level, out var l) && !Enum.TryParse(l, true, out level))
                throw new InvalidDataException($"The `{JsonFields.Level}` value on line {lineNumber} is not a valid `{nameof(LogEventLevel)}`.");

            Exception? exception = null;
            if (TryGetOptionalField(lineNumber, jObject, JsonFields.Exception, out var ex))
                exception = new TextException(ex);

            ActivityTraceId traceId = default;
            if (TryGetOptionalField(lineNumber, jObject, JsonFields.TraceId, out var tr))
                traceId = ActivityTraceId.CreateFromString(tr.AsSpan());

            ActivitySpanId spanId = default;
            if (TryGetOptionalField(lineNumber, jObject, JsonFields.SpanId, out var sp))
                spanId = ActivitySpanId.CreateFromString(sp.AsSpan());

            var parsedTemplate = messageTemplate == null ?
                new MessageTemplate(Array.Empty<MessageTemplateToken>()) :
                Parser.Parse(messageTemplate);

            var renderings = NoRenderings;

            if (jObject.TryGetValue(JsonFields.Renderings, out var r))
            {
                if (r is not JArray renderedByIndex)
                    throw new InvalidDataException($"The `{JsonFields.Renderings}` value on line {lineNumber} is not an array as expected.");

                renderings = parsedTemplate.Tokens
                    .OfType<PropertyToken>()
                    .Where(t => t.Format != null)
                    .Zip(renderedByIndex, (t, rd) => new Rendering(t.PropertyName, t.Format!, rd.Value<string>()!))
                    .ToArray();
            }

            var properties = jObject
                .Properties()
                .Where(f => !JsonFields.All.Contains(f.Name))
                .Select(f =>
                {
                    var name = f.Name;
                    var renderingsByFormat = renderings.Length != 0 ? renderings.Where(rd => rd.Name == name).ToArray() : NoRenderings;
                    return PropertyFactory.CreateProperty(name, f.Value, renderingsByFormat);
                })
                .ToList();

            if (TryGetOptionalEventId(lineNumber, jObject, JsonFields.EventId, out var eventId))
            {
                properties.Add(new LogEventProperty("EventId", new ScalarValue(eventId)));
            }

            return new LogEvent(timestamp, level, exception, parsedTemplate, properties, traceId, spanId);
        }

        private static bool TryGetOptionalField(int lineNumber, JObject data, string field, [NotNullWhen(true)] out string? value)
        {
            if (!data.TryGetValue(field, out var token) || token.Type == JTokenType.Null)
            {
                value = null;
                return false;
            }

            if (token.Type != JTokenType.String)
                throw new InvalidDataException($"The value of `{field}` on line {lineNumber} is not in a supported format.");

            value = token.Value<string>()!;
            return true;
        }

        private static bool TryGetOptionalEventId(int lineNumber, JObject data, string field, out object? eventId)
        {
            if (!data.TryGetValue(field, out var token) || token.Type == JTokenType.Null)
            {
                eventId = null;
                return false;
            }

            switch (token.Type)
            {
                case JTokenType.String:
                    eventId = token.Value<string>();
                    return true;
                case JTokenType.Integer:
                    eventId = token.Value<uint>();
                    return true;
                default:
                    throw new InvalidDataException(
                        $"The value of `{field}` on line {lineNumber} is not in a supported format.");
            }
        }

        private static DateTimeOffset GetRequiredTimestampField(int lineNumber, JObject data, string field)
        {
            if (!data.TryGetValue(field, out var token) || token.Type == JTokenType.Null)
                throw new InvalidDataException($"The data on line {lineNumber} does not include the required `{field}` field.");

            if (token.Type == JTokenType.Date)
            {
                var dt = token.Value<JValue>()!.Value;
                if (dt is DateTimeOffset offset)
                    return offset;

                return (DateTime)dt!;
            }
            else
            {
                if (token.Type != JTokenType.String)
                    throw new InvalidDataException($"The value of `{field}` on line {lineNumber} is not in a supported format.");

                var text = token.Value<string>()!;
                if (!DateTimeOffset.TryParse(text, out var offset))
                    throw new InvalidDataException($"The value of `{field}` on line {lineNumber} is not in a supported timestamp format.");

                return offset;
            }
        }

        private static JsonSerializer CreateSerializer()
        {
            return JsonSerializer.Create(new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.None,
                Culture = CultureInfo.InvariantCulture
            });
        }
    }
}
