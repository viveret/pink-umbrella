using System;
using System.Text.Json.Serialization;
using Poncho.Util;

namespace Poncho.Models.Peer
{
    [IsDocumented]
    public class PeerStatsModel
    {
        [JsonPropertyName("userCount")]
        public int UserCount { get; set; }

        [JsonPropertyName("postCount")]
        public int PostCount { get; set; }

        [JsonPropertyName("peerCount")]
        public int PeerCount { get; set; }

        [JsonPropertyName("shopCount")]
        public int ShopCount { get; set; }

        [JsonPropertyName("mediaCount")]
        public int MediaCount { get; set; }

        [JsonPropertyName("startTime")]
        public DateTime StartTime { get; set; }
    }
}