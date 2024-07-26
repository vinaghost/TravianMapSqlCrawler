using Microsoft.EntityFrameworkCore;
using OasisCrawler.Enums;

namespace OasisCrawler.Entities
{
    [PrimaryKey(nameof(Id))]
    [Index(nameof(X), nameof(Y), nameof(Type), nameof(Detail))]
    [Index(nameof(Type), nameof(Detail))]
    public class Oasis
    {
        public int Id { get; set; }

        public int X { get; set; }
        public int Y { get; set; }

        public TileType Type { get; set; }
        public TileDetail Detail { get; set; }
    }

    public static class OasisExtensions
    {
        public const int MapSize = 200;

        public static Oasis Create(int x, int y, TileType type, TileDetail detail)
            => new()
            {
                Id = 1 + ((200 - y) * (MapSize * 2 + 1)) + MapSize + x,
                X = x,
                Y = y,
                Type = type,
                Detail = detail
            };
    }
}