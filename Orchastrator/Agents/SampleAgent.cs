using System;
using System.Threading.Tasks;

namespace CodeAssist.Agents
{
    public class SampleAgent : BaseAgent
    {
        private readonly AgentConfiguration _configuration;
        private readonly AgentCommunication _communication;

        public SampleAgent(string name, string version, AgentConfiguration configuration, AgentCommunication communication)
            : base(name, version)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _communication = communication ?? throw new ArgumentNullException(nameof(communication));
        }

        protected override async Task OnInitialize()
        {
            Console.WriteLine($"Initializing {AgentName} v{AgentVersion}");

            // Load configuration
            await _configuration.LoadConfigurationAsync();

            // Example: Set default configuration values
            if (!_configuration.ContainsKey("SampleSetting"))
            {
                _configuration.SetValue("SampleSetting", "DefaultValue");
            await _configuration.SaveConfigurationAsync();
            }

            // Subscribe to messages from other agents
            _communication.Subscribe(AgentName, "OtherAgent");
        }

        protected override async Task OnExecute()
        {
            Console.WriteLine($"Executing {AgentName}");

            // Example: Get configuration value
            var sampleValue = _configuration.GetValue<string>("SampleSetting");
            Console.WriteLine($"SampleSetting value: {sampleValue}");

            // Example: Send a message to another agent
            await _communication.SendMessageAsync(AgentName, "OtherAgent", "Hello from SampleAgent!");

            // Simulate some work
            await Task.Delay(1000);
        }

        protected override async Task OnShutdown()
        {
            Console.WriteLine($"Shutting down {AgentName}");

            // Unsubscribe from messages
            _communication.Unsubscribe(AgentName, "OtherAgent");

            // Save configuration
            await _configuration.SaveConfiguration();
        }
    }
}