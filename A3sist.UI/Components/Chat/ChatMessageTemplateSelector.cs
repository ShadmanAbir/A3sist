using System.Windows;
using System.Windows.Controls;
using A3sist.Shared.Enums;
using A3sist.UI.Models.Chat;

namespace A3sist.UI.Components.Chat
{
    /// <summary>
    /// Template selector for chat messages based on message type
    /// </summary>
    public class ChatMessageTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Template for user messages
        /// </summary>
        public DataTemplate? UserTemplate { get; set; }

        /// <summary>
        /// Template for assistant messages
        /// </summary>
        public DataTemplate? AssistantTemplate { get; set; }

        /// <summary>
        /// Template for system messages
        /// </summary>
        public DataTemplate? SystemTemplate { get; set; }

        /// <summary>
        /// Selects the appropriate template based on message type
        /// </summary>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ChatMessage message)
            {
                return message.Type switch
                {
                    ChatMessageType.User => UserTemplate ?? base.SelectTemplate(item, container),
                    ChatMessageType.Assistant => AssistantTemplate ?? base.SelectTemplate(item, container),
                    ChatMessageType.System => SystemTemplate ?? base.SelectTemplate(item, container),
                    _ => base.SelectTemplate(item, container)
                };
            }

            return base.SelectTemplate(item, container);
        }
    }
}