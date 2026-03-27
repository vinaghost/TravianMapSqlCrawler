namespace ServerScanner.Entities
{
    public class LobbyServer
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Url { get; set; }
        public required DateTime UpdateAt { get; set; }
    }
}