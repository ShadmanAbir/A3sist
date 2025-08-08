using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CodeAssist.Agents
{
    public class AgentConfiguration
    {
        private readonly string _configFilePath;
        private Dictionary<string, object> _configurations;

        public AgentConfiguration(string configFilePath)
        {
            _configFilePath = configFilePath ?? throw new ArgumentNullException(nameof(configFilePath));
            _configurations = new Dictionary<string, object>();
        }

        public async Task LoadConfigurationAsync()
        {
            if (File.Exists(_configFilePath))
            {
            var json = await System.IO.File.ReadAllTextAsync(_configFilePath);
                _configurations = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            }
            else
            {
                _configurations = new Dictionary<string, object>();
            }
        }

        public async Task SaveConfigurationAsync()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_configurations, options);
            await System.IO.File.WriteAllTextAsync(_configFilePath, json);
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (_configurations.TryGetValue(key, out var value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            return defaultValue;
        }

        public void SetValue<T>(string key, T value)
        {
            _configurations[key] = value;
        }

        public bool ContainsKey(string key)
        {
            return _configurations.ContainsKey(key);
        }

        public void RemoveKey(string key)
        {
            _configurations.Remove(key);
        }

        public IEnumerable<string> GetAllKeys()
        {
            return _configurations.Keys;
        }
    }
}