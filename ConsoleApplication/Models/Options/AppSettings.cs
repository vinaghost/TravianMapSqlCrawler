namespace ConsoleApplication.Models.Options
{
    public sealed class AppSettings
    {
        public string Greeting { get; set; } = "";
        public string[] GreetingArray { get; set; } = [];
    }
}