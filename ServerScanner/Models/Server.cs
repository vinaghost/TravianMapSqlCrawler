namespace ServerScanner.Models
{
    public class Server
    {
        public string Id { get; set; } // The unique identifier for the server
        public string Name { get; set; } // The name of the server
        public string Url { get; set; } // The domain name of the server URL

        public Server(string id, string name, string url)
        {
            Id = id;
            Name = name;
            Url = ExtractDomain(url);
        }

        private static string ExtractDomain(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return uri.Host; // Extract the domain name
            }
            return url; // Return the original URL if parsing fails
        }
    }
}