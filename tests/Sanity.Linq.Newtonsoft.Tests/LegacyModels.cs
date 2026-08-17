using Newtonsoft.Json;
using Sanity.Linq.CommonTypes;
using System.Collections.Generic;

namespace Sanity.Linq.Newtonsoft.Tests
{
    /// <summary>
    /// A model exactly as it would have been written against Sanity LINQ 1.x: Sanity system
    /// fields mapped with Newtonsoft.Json attributes, and no System.Text.Json attributes
    /// anywhere. These types are the reason the compatibility package exists.
    /// </summary>
    public class LegacyCategory
    {
        [JsonProperty("_id")]
        public string CategoryId { get; set; }

        [JsonProperty("_type")]
        public string DocumentType => "category";

        public string Title { get; set; }

        public int InternalId { get; set; }

        [JsonProperty("customFieldName")]
        public string RenamedField { get; set; }

        [JsonIgnore]
        public string NotPersisted { get; set; }

        public List<string> Tags { get; set; }
    }

    /// <summary>
    /// A legacy model that also holds a reference, to confirm the core reference converter
    /// still resolves _id through a Newtonsoft-attributed property.
    /// </summary>
    public class LegacyPost
    {
        [JsonProperty("_id")]
        public string PostId { get; set; }

        [JsonProperty("_type")]
        public string DocumentType => "post";

        public string Title { get; set; }

        public SanityReference<LegacyCategory> Category { get; set; }
    }

    /// <summary>
    /// Value object used to exercise a custom Newtonsoft converter through the adapter.
    /// </summary>
    public class Money
    {
        public Money() { }

        public Money(string currency, decimal amount)
        {
            Currency = currency;
            Amount = amount;
        }

        public string Currency { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// A hand-written Newtonsoft converter of the kind an application might already own:
    /// it flattens Money to a single string and parses it back.
    /// </summary>
    public class MoneyConverter : JsonConverter
    {
        public override bool CanConvert(System.Type objectType) => objectType == typeof(Money);

        public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
        {
            var raw = reader.Value as string;
            if (string.IsNullOrEmpty(raw))
            {
                return null;
            }

            var parts = raw.Split(' ');
            return new Money(parts[0], decimal.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var money = (Money)value;
            writer.WriteValue($"{money.Currency} {money.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }
    }

    public class LegacyProduct
    {
        [JsonProperty("_id")]
        public string ProductId { get; set; }

        [JsonProperty("_type")]
        public string DocumentType => "product";

        public Money Price { get; set; }
    }
}
