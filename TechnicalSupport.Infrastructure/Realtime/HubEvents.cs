namespace TechnicalSupport.Infrastructure.Realtime
{
    public static class HubEvents
    {
        // Server-to-Client Events
        public const string ReceiveNotification = "ReceiveNotification";
        public const string TicketListUpdated = "TicketListUpdated";
        public const string NewTicketAdded = "NewTicketAdded";

        // Client-to-Server Events
        public const string JoinTicketGroup = "JoinTicketGroup";
        public const string LeaveTicketGroup = "LeaveTicketGroup";
    }
} 