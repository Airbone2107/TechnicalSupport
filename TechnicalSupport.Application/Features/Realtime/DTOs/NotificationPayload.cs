namespace TechnicalSupport.Application.Features.Realtime.DTOs
{
    /// <summary>
    /// Represents the data structure for a real-time notification sent to the client.
    /// </summary>
    public class NotificationPayload
    {
        /// <summary>
        /// A unique identifier for the notification.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The notification message to be displayed to the user.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// A URL link for the user to navigate to when the notification is clicked.
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// The timestamp when the notification was generated.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The severity or type of the notification, used for styling on the client-side.
        /// e.g., "info", "success", "warning", "error".
        /// </summary>
        public string Type { get; set; } = "info";

        /// <summary>
        /// Indicates if the notification requires immediate user attention (e.g., a persistent toast).
        /// </summary>
        public bool IsHighlight { get; set; } = false;
    }
} 