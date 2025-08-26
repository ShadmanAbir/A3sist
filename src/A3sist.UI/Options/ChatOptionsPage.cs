using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace A3sist.UI.Options
{
    /// <summary>
    /// Options page for chat interface settings
    /// </summary>
    [Guid("8F9E5A2B-1C3D-4E5F-6A7B-8C9D0E1F2A3B")]
    [ComVisible(true)]
    public class ChatOptionsPage : DialogPage
    {
        private string _defaultModel = "gpt-4";
        private int _maxTokens = 4000;
        private double _temperature = 0.7;
        private bool _enableStreaming = true;
        private bool _showSuggestions = true;
        private bool _autoSave = true;
        private int _historyLimit = 100;
        private string _chatTheme = "Auto";
        private bool _enableNotifications = true;
        private bool _enableSounds = false;
        private int _typingDelay = 1500;

        [Category("AI Model Settings")]
        [DisplayName("Default Model")]
        [Description("The default AI model to use for chat responses")]
        public string DefaultModel
        {
            get => _defaultModel;
            set => _defaultModel = value;
        }

        [Category("AI Model Settings")]
        [DisplayName("Max Tokens")]
        [Description("Maximum number of tokens for AI responses")]
        public int MaxTokens
        {
            get => _maxTokens;
            set => _maxTokens = Math.Max(100, Math.Min(8000, value));
        }

        [Category("AI Model Settings")]
        [DisplayName("Temperature")]
        [Description("Creativity level of AI responses (0.0 = deterministic, 1.0 = creative)")]
        public double Temperature
        {
            get => _temperature;
            set => _temperature = Math.Max(0.0, Math.Min(2.0, value));
        }

        [Category("Chat Interface")]
        [DisplayName("Enable Streaming")]
        [Description("Enable real-time streaming of AI responses")]
        public bool EnableStreaming
        {
            get => _enableStreaming;
            set => _enableStreaming = value;
        }

        [Category("Chat Interface")]
        [DisplayName("Show Smart Suggestions")]
        [Description("Show contextual suggestions panel by default")]
        public bool ShowSuggestions
        {
            get => _showSuggestions;
            set => _showSuggestions = value;
        }

        [Category("Chat Interface")]
        [DisplayName("Auto Save Conversations")]
        [Description("Automatically save chat conversations")]
        public bool AutoSave
        {
            get => _autoSave;
            set => _autoSave = value;
        }

        [Category("Chat Interface")]
        [DisplayName("History Limit")]
        [Description("Maximum number of conversations to keep in history (0 = unlimited)")]
        public int HistoryLimit
        {
            get => _historyLimit;
            set => _historyLimit = Math.Max(0, Math.Min(1000, value));
        }

        [Category("Appearance")]
        [DisplayName("Chat Theme")]
        [Description("Visual theme for the chat interface")]
        [TypeConverter(typeof(ThemeConverter))]
        public string ChatTheme
        {
            get => _chatTheme;
            set => _chatTheme = value;
        }

        [Category("Notifications")]
        [DisplayName("Enable Notifications")]
        [Description("Show notifications for chat events")]
        public bool EnableNotifications
        {
            get => _enableNotifications;
            set => _enableNotifications = value;
        }

        [Category("Notifications")]
        [DisplayName("Enable Sounds")]
        [Description("Play sounds for chat events")]
        public bool EnableSounds
        {
            get => _enableSounds;
            set => _enableSounds = value;
        }

        [Category("Advanced")]
        [DisplayName("Typing Delay (ms)")]
        [Description("Delay before showing typing indicators")]
        public int TypingDelay
        {
            get => _typingDelay;
            set => _typingDelay = Math.Max(500, Math.Min(5000, value));
        }
    }

    /// <summary>
    /// Type converter for chat theme selection
    /// </summary>
    public class ThemeConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new[]
            {
                "Auto",
                "Light", 
                "Dark",
                "High Contrast"
            });
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }
    }
}