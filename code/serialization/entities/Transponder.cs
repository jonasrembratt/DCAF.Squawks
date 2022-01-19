using System;
using System.Text.Json.Serialization;
using TetraPak.DynamicEntities;
using TetraPak.Serialization;

namespace DCAF.Squawks
{
    [Serializable]
    [JsonConverter(typeof(DynamicEntityJsonConverter<Transponder>))]
    class Transponder : DynamicEntity
    {
        [JsonPropertyName("mode1")]
        public string? Mode1
        {
            get => Get<string?>(); 
            set => Set(value);
        }

        [JsonPropertyName("mode3")]
        public string? Mode3
        {
            get => Get<string?>(); 
            set => Set(value);
        }

        [JsonPropertyName("comment")]
        public string? Comment
        {
            get => Get<string?>(); 
            set => Set(value);
        }
    }
}