﻿namespace ServerCrawler.Entities
{
    public class Server
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
        public DateTime StartDate { get; set; }
    }
}