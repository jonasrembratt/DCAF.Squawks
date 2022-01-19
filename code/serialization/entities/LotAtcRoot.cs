using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DCAF.Squawks
{
    class LotAtcRoot
    {
        [JsonPropertyName("__comments")]
        public string? Comments { get; set; }
        
        [JsonPropertyName("enable")]
        public bool? Enable { get; set; }
        
        [JsonPropertyName("transponders")]
        public List<Transponder>? Transponders { get; set; }
    }
}