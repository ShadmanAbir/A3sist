using System;
using System.Collections.Generic;
using System.Text.Json;

namespace A3sist.Services
{
    public class ContextSerializer
    {
        private readonly Dictionary<string, Type> _contextTypes = new Dictionary<string, Type>();

        public void RegisterContextType(string contextType, Type type)
        {
            if (string.IsNullOrEmpty(contextType))
                throw new ArgumentNullException(nameof(contextType));

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            _contextTypes[contextType] = type;
        }

        public string SerializeContext(string contextType, object context)
        {
            if (string.IsNullOrEmpty(contextType))
                throw new ArgumentNullException(nameof(contextType));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!_contextTypes.TryGetValue(contextType, out var type))
                throw new ArgumentException($"Context type {contextType} not registered");

            if (!type.IsInstanceOfType(context))
                throw new ArgumentException($"Context is not of type {type.Name}");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(context, type, options);
        }

        public object DeserializeContext(string contextType, string serializedContext)
        {
            if (string.IsNullOrEmpty(contextType))
                throw new ArgumentNullException(nameof(contextType));

            if (string.IsNullOrEmpty(serializedContext))
                throw new ArgumentNullException(nameof(serializedContext));

            if (!_contextTypes.TryGetValue(contextType, out var type))
                throw new ArgumentException($"Context type {contextType} not registered");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize(serializedContext, type, options)
                ?? throw new InvalidOperationException("Deserialization failed");
        }

        public IEnumerable<string> GetRegisteredContextTypes()
        {
            return _contextTypes.Keys;
        }
    }
}