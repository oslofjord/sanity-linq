// Copywrite 2018 Oslofjord Operations AS

// This file is part of Sanity LINQ (https://github.com/oslofjord/sanity-linq).

//  Sanity LINQ is free software: you can redistribute it and/or modify
//  it under the terms of the MIT Licence.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//  MIT Licence for more details.

//  You should have received a copy of the MIT Licence
//  along with this program.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sanity.Linq.Json;
using System;
using System.Text.Encodings.Web;
using StjJsonSerializerOptions = System.Text.Json.JsonSerializerOptions;
using StjJsonIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition;
using StjJsonUnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling;
using StjJsonNamingPolicy = System.Text.Json.JsonNamingPolicy;

namespace Sanity.Linq.Newtonsoft
{
    /// <summary>
    /// Bridges Sanity LINQ 1.x's Newtonsoft.Json configuration onto 2.x.
    ///
    /// Typical migration of a call site:
    ///
    /// <code>
    /// // 1.x
    /// var sanity = new SanityDataContext(options, mySettings);
    ///
    /// // 2.x, with this package installed
    /// var sanity = new SanityDataContext(options, mySettings.ToSerializerOptions());
    /// </code>
    ///
    /// The result starts from the library defaults, so Sanity's own behaviour (reference
    /// handling, camelCasing, null omission, relaxed escaping) stays in place, and adds
    /// support for Newtonsoft attributes on model classes.
    ///
    /// Not everything can be translated. Settings that have no equivalent throw
    /// <see cref="NotSupportedException"/> rather than being silently dropped, because
    /// ignoring them would change the JSON sent to Sanity. See MIGRATION.md.
    /// </summary>
    public static class NewtonsoftCompat
    {
        /// <summary>
        /// A pristine settings instance, used to tell "left at the default" apart from
        /// "explicitly set to something" for properties whose default is not null.
        /// </summary>
        private static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings();

        /// <summary>
        /// A pristine contract resolver, for the same reason as <see cref="DefaultSettings"/>.
        /// </summary>
        private static readonly DefaultContractResolver DefaultResolver = new DefaultContractResolver();


        /// <summary>
        /// Options equivalent to Sanity LINQ 1.x's defaults, with Newtonsoft attributes on
        /// model classes honoured. Use this when 1.x code passed no settings of its own but
        /// the models still use [JsonProperty] / [JsonIgnore].
        /// </summary>
        public static StjJsonSerializerOptions CreateSerializerOptions()
        {
            var options = SanityJsonOptions.CreateDefault();
            options.TypeInfoResolver = new NewtonsoftAttributeTypeInfoResolver();
            return options;
        }

        /// <summary>
        /// Translates Newtonsoft JsonSerializerSettings into the equivalent
        /// JsonSerializerOptions, on top of the Sanity LINQ defaults.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// A setting is in use that has no System.Text.Json equivalent.
        /// </exception>
        public static StjJsonSerializerOptions ToSerializerOptions(this JsonSerializerSettings settings)
        {
            var options = CreateSerializerOptions();

            if (settings == null)
            {
                return options;
            }

            ApplyContractResolver(settings, options);
            ApplyNullAndDefaultHandling(settings, options);
            ApplyFormatting(settings, options);
            ApplyConverters(settings, options);
            RejectUntranslatableSettings(settings);

            return options;
        }

        private static void ApplyContractResolver(JsonSerializerSettings settings, StjJsonSerializerOptions options)
        {
            switch (settings.ContractResolver)
            {
                case null:
                    // 1.x's own default was CamelCasePropertyNamesContractResolver, which the
                    // Sanity LINQ defaults already reproduce.
                    return;

                case CamelCasePropertyNamesContractResolver _:
                    options.PropertyNamingPolicy = StjJsonNamingPolicy.CamelCase;
                    options.DictionaryKeyPolicy = StjJsonNamingPolicy.CamelCase;
                    return;

                case DefaultContractResolver defaultResolver when IsNamingOnly(defaultResolver):
                    // A resolver whose only customisation is its naming strategy can be
                    // represented faithfully by delegating to that strategy.
                    var strategy = defaultResolver.NamingStrategy;
                    if (strategy == null)
                    {
                        options.PropertyNamingPolicy = null;
                        options.DictionaryKeyPolicy = null;
                    }
                    else
                    {
                        var policy = new NewtonsoftNamingPolicy(strategy);
                        options.PropertyNamingPolicy = policy;
                        options.DictionaryKeyPolicy = strategy.ProcessDictionaryKeys ? policy : null;
                    }
                    return;

                default:
                    throw new NotSupportedException(
                        $"Sanity.Linq.Newtonsoft cannot translate the contract resolver '{settings.ContractResolver.GetType().FullName}'. " +
                        "Only a null resolver, CamelCasePropertyNamesContractResolver, or a DefaultContractResolver that customises nothing " +
                        "but its NamingStrategy can be translated. Configure JsonSerializerOptions directly instead - for property naming use " +
                        "JsonSerializerOptions.PropertyNamingPolicy, and for per-type or per-member control use a custom IJsonTypeInfoResolver " +
                        "(deriving from NewtonsoftAttributeTypeInfoResolver keeps Newtonsoft attribute support).");
            }
        }

        /// <summary>
        /// Whether a DefaultContractResolver has been left at its defaults apart from the
        /// naming strategy. A subclass may override anything, so subclasses do not qualify.
        ///
        /// Compared against a pristine instance rather than against hardcoded values, because
        /// not all of these flags default to false.
        /// </summary>
        private static bool IsNamingOnly(DefaultContractResolver resolver)
        {
            return resolver.GetType() == typeof(DefaultContractResolver)
                && resolver.SerializeCompilerGeneratedMembers == DefaultResolver.SerializeCompilerGeneratedMembers
                && resolver.IgnoreSerializableInterface == DefaultResolver.IgnoreSerializableInterface
                && resolver.IgnoreSerializableAttribute == DefaultResolver.IgnoreSerializableAttribute
                && resolver.IgnoreIsSpecifiedMembers == DefaultResolver.IgnoreIsSpecifiedMembers
                && resolver.IgnoreShouldSerializeMembers == DefaultResolver.IgnoreShouldSerializeMembers;
        }

        private static void ApplyNullAndDefaultHandling(JsonSerializerSettings settings, StjJsonSerializerOptions options)
        {
            var ignoreNulls = settings.NullValueHandling == NullValueHandling.Ignore;
            var ignoreDefaults = settings.DefaultValueHandling == DefaultValueHandling.Ignore
                              || settings.DefaultValueHandling == DefaultValueHandling.IgnoreAndPopulate;

            // System.Text.Json has a single condition rather than two independent switches;
            // WhenWritingDefault also covers nulls.
            if (ignoreDefaults)
            {
                options.DefaultIgnoreCondition = StjJsonIgnoreCondition.WhenWritingDefault;
            }
            else if (ignoreNulls)
            {
                options.DefaultIgnoreCondition = StjJsonIgnoreCondition.WhenWritingNull;
            }
            else
            {
                options.DefaultIgnoreCondition = StjJsonIgnoreCondition.Never;
            }

            if (settings.MissingMemberHandling == MissingMemberHandling.Error)
            {
                options.UnmappedMemberHandling = StjJsonUnmappedMemberHandling.Disallow;
            }
        }

        private static void ApplyFormatting(JsonSerializerSettings settings, StjJsonSerializerOptions options)
        {
            options.WriteIndented = settings.Formatting == Formatting.Indented;

            if (settings.MaxDepth.HasValue)
            {
                options.MaxDepth = settings.MaxDepth.Value;
            }

            switch (settings.StringEscapeHandling)
            {
                case StringEscapeHandling.Default:
                    // Sanity LINQ's default (relaxed) encoder is the closest match: it leaves
                    // HTML characters and non-ASCII text alone.
                    break;
                case StringEscapeHandling.EscapeNonAscii:
                case StringEscapeHandling.EscapeHtml:
                    options.Encoder = JavaScriptEncoder.Default;
                    break;
            }
        }

        private static void ApplyConverters(JsonSerializerSettings settings, StjJsonSerializerOptions options)
        {
            if (settings.Converters == null)
            {
                return;
            }

            foreach (var converter in settings.Converters)
            {
                options.Converters.Add(new NewtonsoftConverterAdapter(converter, settings));
            }
        }

        private static void RejectUntranslatableSettings(JsonSerializerSettings settings)
        {
            if (settings.TypeNameHandling != TypeNameHandling.None)
            {
                throw new NotSupportedException(
                    "TypeNameHandling has no System.Text.Json equivalent. Sanity documents identify their type with the _type field, " +
                    "so this setting should not be needed; remove it, or use System.Text.Json polymorphic serialization " +
                    "([JsonDerivedType]) if type discriminators really are required.");
            }

            if (settings.PreserveReferencesHandling != PreserveReferencesHandling.None)
            {
                throw new NotSupportedException(
                    "PreserveReferencesHandling has no equivalent that would produce valid Sanity documents. " +
                    "Use JsonSerializerOptions.ReferenceHandler directly if you need cycle handling.");
            }

            // DateFormatString is never null - it defaults to Newtonsoft's ISO 8601 pattern,
            // which is also System.Text.Json's format - so only a changed value is a problem.
            if (settings.DateFormatString != DefaultSettings.DateFormatString)
            {
                throw new NotSupportedException(
                    "A custom DateFormatString cannot be translated: System.Text.Json has no global date format setting. " +
                    "Register a JsonConverter<DateTime> / JsonConverter<DateTimeOffset> on the returned options instead. " +
                    "Note that Sanity expects ISO 8601, which is System.Text.Json's default format.");
            }
        }

        /// <summary>
        /// Presents a Newtonsoft NamingStrategy as a System.Text.Json naming policy, so
        /// property names are produced by exactly the same code as in 1.x.
        /// </summary>
        private class NewtonsoftNamingPolicy : StjJsonNamingPolicy
        {
            private readonly NamingStrategy _strategy;

            public NewtonsoftNamingPolicy(NamingStrategy strategy)
            {
                _strategy = strategy;
            }

            public override string ConvertName(string name) => _strategy.GetPropertyName(name, false);
        }
    }
}
