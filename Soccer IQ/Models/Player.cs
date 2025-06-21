using System.Text.Json.Serialization;

namespace Soccer_IQ.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? PhotoUrl { get; set; }
        public string Position { get; set; }
        [JsonIgnore]
        public List<PLayerStat> PlayerStats { get; set; }
        public int ClubId { get; set; }
        [JsonIgnore]
        public Club Club { get; set; }


   
    }
}