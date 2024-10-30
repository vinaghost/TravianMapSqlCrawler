namespace VillageCrawlerCustom
{
    public static class DonwloadMapSqlCommand
    {
        public static async Task<IList<RawVillage>> Handle(string url)
        {
            using var httpClient = new HttpClient();
            using var responseStream = await httpClient.GetStreamAsync(string.Format("https://{0}/map.sql", url));
            using var reader = new StreamReader(responseStream);
            var villages = MapSqlParser.Parse(reader);
            return villages;
        }
    }
}