using Microsoft.EntityFrameworkCore;
using OasisCrawler.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace OasisCrawler.Entities
{
    [PrimaryKey(nameof(Id))]
    [Index(nameof(X), nameof(Y), nameof(Type), nameof(Detail))]
    [Index(nameof(Type), nameof(Detail))]
    public class Oasis
    {
        public const int MapSize = 200;

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id => 1 + ((200 - Y) * (MapSize * 2 + 1)) + MapSize + Y;

        public int X { get; set; }
        public int Y { get; set; }

        public TileType Type { get; set; }
        public TileDetail Detail { get; set; }
    }
}