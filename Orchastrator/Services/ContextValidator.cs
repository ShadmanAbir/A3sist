using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace A3sist.Services
{
    public class ContextValidator
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

        public bool ValidateContext(string contextType, string serializedContext)
        {
            if (string.IsNullOrEmpty(contextType))
                throw new ArgumentNullException(nameof(contextType));

            if (string.IsNullOrEmpty(serializedContext))
                throw new ArgumentNullException(nameof(serializedContext));

            if (!_contextTypes.TryGetValue(contextType, out var type))
                throw new ArgumentException($"Context type {contextType} not registered");

            try
            {
                var context = new ContextSerializer().DeserializeContext(contextType, serializedContext);

                var validationContext = new ValidationContext(context);
                var validationResults = new List<ValidationResult>();

                return Validator.TryValidateObject(context, validationContext, validationResults, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Validation error: {ex.Message}");
                return false;
            }
        }

        public IEnumerable<string> GetRegisteredContextTypes()
        {
            return _contextTypes.Keys;
        }
    }
}