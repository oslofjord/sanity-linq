# Migrating from Sanity LINQ 1.x to 2.0

Version 2.0 replaces Newtonsoft.Json with `System.Text.Json` ([#71](https://github.com/oslofjord/sanity-linq/issues/71)).
Newtonsoft.Json is no longer a dependency of `Sanity.Linq`.

The JSON serializer is not purely an implementation detail of this library — it appears in
the public API and in your own model classes — so 2.0 is a breaking release. This guide
covers what changed, and how to keep 1.x code working while you migrate.

**The wire format did not change.** GROQ queries, mutation payloads and `ToHtml` output are
byte-for-byte the same as 1.x, apart from the two fixes and the one naming change called out
below. This is enforced by an offline golden test suite.

---

## TL;DR

| If your 1.x code…                                       | Then…                                                                 |
| ------------------------------------------------------- | --------------------------------------------------------------------- |
| only queries and saves documents, using this library's own types | Recompile. Nothing to change.                            |
| uses `[JsonProperty]` on your models                    | Queries keep working. For **serialization**, either switch to `[JsonPropertyName]` or install `Sanity.Linq.Newtonsoft`. |
| passes custom `JsonSerializerSettings`                   | Install `Sanity.Linq.Newtonsoft` and add `.ToSerializerOptions()`.    |
| registers custom block content serializers              | Change `JToken` to `JsonNode`, or install `Sanity.Linq.Newtonsoft`.   |
| subclasses `SanityHtmlBuilder` / `SanityTreeBuilder` / `SanityHtmlSerializers` | Update the overridden signatures by hand. Not covered by the compat package. |

---

## The compatibility package

```
dotnet add package Sanity.Linq.Newtonsoft
```

`Sanity.Linq.Newtonsoft` is a supported package, published in lockstep with `Sanity.Linq`.
It exists to make migration incremental: you can move to 2.0 now and convert your models and
serializer configuration at your own pace, rather than in one commit.

It is intended as migration help rather than a permanent part of your stack — code that only
uses `System.Text.Json` will be simpler and faster — but there is no deadline for removing it,
and it is not deprecated.

---

## 1. Serializer configuration

`JsonSerializerSettings` became `JsonSerializerOptions`, and the properties were renamed to
match.

| 1.x                                          | 2.0                                          |
| -------------------------------------------- | -------------------------------------------- |
| `SanityClient.SerializerSettings`            | `SanityClient.SerializerOptions`             |
| `SanityClient.DeserializerSettings`          | `SanityClient.DeserializerOptions`           |
| `SanityDataContext.SerializerSettings`       | `SanityDataContext.SerializerOptions`        |
| `SanityDataContext.DeserializerSettings`     | `SanityDataContext.DeserializerOptions`      |
| `SanityMutationBuilder.Build(settings)`      | `SanityMutationBuilder.Build(options)`       |

If you never passed settings of your own, there is nothing to do — the defaults are
equivalent (camelCased property *and* dictionary keys, nulls omitted, Sanity reference
handling, and relaxed escaping so HTML and non-ASCII content are not escaped).

### Keeping your existing settings

```csharp
using Sanity.Linq.Newtonsoft;

// 1.x
var sanity = new SanityDataContext(options, mySettings);

// 2.0
var sanity = new SanityDataContext(options, mySettings.ToSerializerOptions());
```

`ToSerializerOptions()` starts from the library defaults and layers your settings on top. It
also installs Newtonsoft attribute support (section 2), so this single change usually covers
both concerns.

**What translates:**

| Setting                                             | Handling                                                                 |
| --------------------------------------------------- | ------------------------------------------------------------------------ |
| `NullValueHandling`                                 | → `DefaultIgnoreCondition`                                               |
| `DefaultValueHandling`                              | → `DefaultIgnoreCondition = WhenWritingDefault`                          |
| `MissingMemberHandling.Error`                       | → `UnmappedMemberHandling = Disallow`                                    |
| `Formatting`                                        | → `WriteIndented`                                                        |
| `MaxDepth`                                          | → `MaxDepth`                                                             |
| `StringEscapeHandling`                              | → `Encoder`                                                              |
| `ContractResolver` = `CamelCasePropertyNamesContractResolver` | → `PropertyNamingPolicy` + `DictionaryKeyPolicy`                |
| `ContractResolver` = `DefaultContractResolver` customising only `NamingStrategy` | → the strategy is called directly, so names are identical to 1.x |
| `Converters`                                        | Each Newtonsoft converter is wrapped and run inside `System.Text.Json`   |

**What does not, and throws `NotSupportedException` rather than silently changing your
payloads:**

- **Any other `ContractResolver`** — including subclasses of `DefaultContractResolver` and
  resolvers that override property inclusion, ordering, or per-member converters. There is no
  mechanical mapping to `IJsonTypeInfoResolver`. Rewrite it as an `IJsonTypeInfoResolver`;
  derive from `NewtonsoftAttributeTypeInfoResolver` to keep Newtonsoft attribute support.
- **`TypeNameHandling`**, **`PreserveReferencesHandling`** — no equivalent, and neither
  produces valid Sanity documents.
- **A custom `DateFormatString`** — `System.Text.Json` has no global date format. Register a
  `JsonConverter<DateTime>` / `JsonConverter<DateTimeOffset>` instead. Note that Sanity
  expects ISO 8601, which is the default in both libraries, so the default needs nothing.

Wrapped Newtonsoft converters work by buffering the value and handing it to the converter
through its own DOM. They are correct but not streaming; rewriting hot-path converters
against `System.Text.Json` is worthwhile eventually.

---

## 2. Model classes

`System.Text.Json` does not recognise Newtonsoft's attributes. The preferred change is a
find-and-replace:

```csharp
// 1.x
using Newtonsoft.Json;

public class Category
{
    [JsonProperty("_id")] public string CategoryId { get; set; }
    [JsonIgnore]          public string Scratch { get; set; }
}

// 2.0
using System.Text.Json.Serialization;

public class Category
{
    [JsonPropertyName("_id")] public string CategoryId { get; set; }
    [JsonIgnore]              public string Scratch { get; set; }
}
```

### If you are not ready to change your models

The core library reads `[JsonProperty]` and `[JsonIgnore]` **reflectively, by attribute type
name**, so 1.x models keep generating correct **GROQ** with no compat package and no
Newtonsoft dependency. Queries, projections, includes and `Where` clauses all resolve the
right Sanity field names.

Reproducing those names during **serialization** does need the compat package, because that
part is `System.Text.Json`'s to decide:

```csharp
using Sanity.Linq.Newtonsoft;

// Models still using [JsonProperty], no custom settings of your own:
var sanity = new SanityDataContext(options, NewtonsoftCompat.CreateSerializerOptions());
```

Without this, a model with `[JsonProperty("_id")] public string CategoryId` would be written
as `"categoryId"` rather than `"_id"` — the document would be saved with the wrong field
names. If you are migrating models gradually, install the package first.

### One behavioural change

1.x applied its naming strategy *on top of* explicit attribute names, so
`[JsonProperty("MyField")]` was written as `"myField"`. `System.Text.Json` treats an explicit
name as final, so `[JsonPropertyName("MyField")]` is written as `"MyField"`.

This is not replicated, because the `System.Text.Json` behaviour is the less surprising one.
It only affects names that were not already lowercase — every Sanity system field
(`_id`, `_type`, `_rev`, `_key`, `_createdAt`, `_updatedAt`) is unaffected. If you relied on
it, write the intended name in the attribute.

---

## 3. Block content serializers

Custom serializers now receive `System.Text.Json.Nodes.JsonNode` instead of `JToken`:

```csharp
// 1.x
sanity.AddHtmlSerializer("myType", (JToken node, SanityOptions options) =>
    Task.FromResult($"<div>{node["title"]}</div>"));

// 2.0
sanity.AddHtmlSerializer("myType", (JsonNode node, SanityOptions options) =>
    Task.FromResult($"<div>{node["title"]}</div>"));
```

The two DOMs are similar, but `JsonNode` indexers are less forgiving: reading a missing field
and casting it will throw where `JToken` returned null. The library's own
`Sanity.Linq.Json.SanityJsonNode` helpers (`GetString`, `GetInt`, `GetBool`, `GetArray`) are
public and null-tolerant if you want the 1.x leniency back.

### Keeping JToken serializers

Add a `using` and your existing delegates compile unchanged:

```csharp
using Sanity.Linq.Newtonsoft;

sanity.AddHtmlSerializer("myType", (JToken node, SanityOptions options) =>
    Task.FromResult($"<div>{node["title"]}</div>"));
```

These overloads deliberately live in the `Sanity.Linq.Newtonsoft` namespace rather than
`Sanity.Linq`. If both were in scope, an implicitly-typed lambda would be ambiguous:

```csharp
sanity.AddHtmlSerializer("myType", (node, options) => ...);  // JsonNode or JToken?
```

Requiring the extra `using` keeps every call site unambiguous, and makes the remaining
compatibility surface easy to find later with a single grep.

**Write the parameter types explicitly** (`(JToken node, SanityOptions options)`) if you have
not already — an implicitly-typed lambda will bind to the `JsonNode` overload and then fail
to compile in its body.

---

## 4. Not covered by the compatibility package

Extension methods can restore a signature; they cannot change a virtual one. These need
manual updates:

- **Subclasses of `SanityHtmlSerializers`**, in particular overrides of
  `TrySerializeMarkDef` — the `markDef` parameter is now `JsonNode`.
- **Subclasses of `SanityTreeBuilder`** — `Build(JArray)` is now `Build(JsonArray)`.
- **Subclasses of `SanityHtmlBuilder`** — the `Serializers` dictionary and the `AddSerializer`
  / `BuildAsync` overloads are typed on `JsonNode`.
- **Direct casts of `SanityLocale<T>` values**. The dictionary holds raw JSON, which is now
  `JsonElement` rather than `JObject`. `Get<T>()` and `Translations` are unchanged and
  continue to be the supported way to read a translation; `(JObject)locale["en"]` is not.
- **Catching `Newtonsoft.Json.JsonException`** from library calls. Serialization failures are
  wrapped in `SanitySerializationException` (introduced in 1.8.0); catch that, or
  `SanityException` for any Sanity-domain failure.

---

## 5. Fixes included in 2.0

Two pre-existing defects were fixed as part of the rewrite. Both change behaviour for the
better, and both are covered by tests.

- **Dereferenced references carrying `_key` or `_weak` no longer throw.** In 1.x the raw JSON
  token was assigned straight onto the `string` / `bool?` property, producing
  `ArgumentException: Object of type 'Newtonsoft.Json.Linq.JValue' cannot be converted to
  type 'System.String'`. Sanity emits `_key` for objects inside arrays, so any projection
  that dereferenced such a reference failed.
- **Schemaless fields project correctly.** A field typed as a free-form JSON object emitted
  `field{...}` *and* a second projection built from the DOM type's own CLR members. Free-form
  fields now emit `{...}` only, and `JsonObject`, `JsonNode` and `JsonElement` are recognised
  alongside Newtonsoft's `JObject`.

## 6. Known issue, unchanged

`OrderBy(...)` combined with `Skip`/`Take` repeats the ordering clause once per re-visit of
the expression tree — `order(title asc, title asc, title asc, title asc)`. The query is still
valid and correctly ordered. This predates 2.0 and was deliberately left alone so the
migration is provably behaviour-preserving; it is pinned by a golden test and tracked
separately.
