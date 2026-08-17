using Sanity.Linq.CommonTypes;
using Sanity.Linq.Extensions;
using Xunit;

namespace Sanity.Linq.Tests.Golden
{
    /// <summary>
    /// Pins property-name conversion.
    ///
    /// Newtonsoft's CamelCaseNamingStrategy and System.Text.Json's
    /// JsonNamingPolicy.CamelCase implement the same idea but are separate
    /// implementations, and they are the most likely place for the migration to silently
    /// rename a field. Acronym runs are the interesting cases.
    /// </summary>
    public class PropertyNamingGoldenTests
    {
        [Fact]
        public void CamelCasing_of_awkward_property_names()
        {
            var context = GoldenFixtures.CreateContext();
            var json = context.DocumentSet<NamingProbe>().Create(new NamingProbe { Id = "probe-1" }).Build();

            JsonAssert.Equivalent(
                "{\"mutations\":[{\"create\":{" +
                "\"simple\":\"a\"," +
                "\"id\":\"b\"," +              // ID          -> id
                "\"ipAddress\":\"c\"," +       // IPAddress   -> ipAddress
                "\"urlValue\":\"d\"," +        // URLValue    -> urlValue
                "\"a\":\"e\"," +               // A           -> a
                "\"ab\":\"f\"," +              // AB          -> ab
                "\"abCd\":\"g\"," +            // ABCd        -> abCd
                "\"value2Name\":\"h\"," +
                "\"already_Snake\":\"i\"," +   // underscores are left alone
                "\"xmlHttpRequest\":\"j\"," +  // XMLHttpRequest -> xmlHttpRequest
                "\"_id\":\"probe-1\"," +
                "\"_type\":\"namingProbe\"" +
                "}}]}",
                json);
        }

        [Fact]
        public void Dictionary_keys_are_camel_cased()
        {
            // SanityLocale<T> is a Dictionary<string, object>, and the configured naming
            // policy applies to dictionary keys as well as to property names. Language codes
            // and _type are unaffected in practice, but the behaviour is part of the
            // contract and has to be configured explicitly under System.Text.Json.
            var context = GoldenFixtures.CreateContext();
            var probe = new LocaleProbe { Id = "probe-1" };
            probe.Title["en"] = "Hello";
            probe.Title["nb-NO"] = "Hei";
            probe.Title["SomeKey"] = "Cased";

            JsonAssert.Equivalent(
                "{\"mutations\":[{\"create\":{" +
                "\"title\":{\"_type\":\"localeString\",\"en\":\"Hello\",\"nb-NO\":\"Hei\",\"someKey\":\"Cased\"}," +
                "\"_id\":\"probe-1\",\"_type\":\"localeProbe\"" +
                "}}]}",
                context.DocumentSet<LocaleProbe>().Create(probe).Build());
        }
    }

    /// <summary>
    /// Property names chosen to stress acronym runs, single letters, digits and
    /// underscores. Deliberately carries no serializer attributes: the names here are
    /// produced purely by the configured naming policy.
    /// </summary>
    public class NamingProbe : SanityDocument
    {
        public string Simple { get; set; } = "a";
        public string ID { get; set; } = "b";
        public string IPAddress { get; set; } = "c";
        public string URLValue { get; set; } = "d";
        public string A { get; set; } = "e";
        public string AB { get; set; } = "f";
        public string ABCd { get; set; } = "g";
        public string Value2Name { get; set; } = "h";
        public string Already_Snake { get; set; } = "i";
        public string XMLHttpRequest { get; set; } = "j";
    }

    /// <summary>
    /// Carries a localized field so dictionary-key casing is observable.
    /// </summary>
    public class LocaleProbe : SanityDocument
    {
        public SanityLocale<string> Title { get; set; } = new SanityLocale<string>("localeString");
    }
}
