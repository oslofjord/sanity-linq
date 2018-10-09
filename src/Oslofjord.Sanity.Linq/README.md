# Sanity LINQ
 **A strongly-typed .Net Client** for [Sanity CMS](https://sanity.io) with support for LINQ queries, mutations, transactions, joins, projections and more...


## Introduction
Sanity CMS is a headless CMS available at https://sanity.io with a powerful query API, file store and CDN.

Sanity LINQ was intially developed at [Oslofjord Convention Center](https://oslofjord.com) to facilitate development of .Net projects based on Sanity CMS.

Inspiration was drawn from the .Net client provided by [onyboy](https://github.com/onybo) at https://github.com/onybo/sanity-client.

The Sanity LINQ client goes beyond providing a simple HTTP client and introduces strongly typed queries, projections, mutations and joins - much in the same way as Entity Framework provides this for SQL.

## Getting Started
To get started, simply instaniate a new SanityDataContext:

``` csharp
var options = new SanityOptions
        {
            ProjectId = "#your-project-id#",
            Dataset = "#your-dataset#",
            Token = "#your-token#",
            UseCdn = false
        };

var sanity = new SanityDataContext(options);

```

### 1. Basic Queries
Start by defining entity classes which match documents in Sanity.


> **Alternative 1:**
> 
> POCO Entity with zero dependencies on Sanity Linq.
``` csharp
// Example
public class Category
{
    // Use of JsonProperty to serialize to Sanity _id field.
    [JsonProperty("_id")]
    public string CategoryId { get; set; }

    // Type field is also required
    [JsonProperty("_type")]
    public string DocumentType => "category";

    public string Title { get; set; }

    public string Description { get; set; }
}
```


> **Alternative 2:**
> 
> Create a class which inherits SanityDocument.

``` csharp
// Example
public class Author : SanityDocument
{
    public Author() : base() { }

    public string Name { get; set; }

    // etc...
}

//Example
public class Post : SanityDocument
{
    public string Title { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    // etc...
}

```

Next, simply run Linq queries against the SanityDataContext:
``` csharp
var posts = sanity.DocumentSet<Post>();

var totalNumberOfPosts = await posts.CountAsync();
var publishedToday = await posts.Where(p => p.PublishedAt > DateTime.Today).ToListAsync();

```
The LINQ queries above are respectively translated to a Sanity GROQ query by Sanity Linq:

```
// Total number of posts:
count(*[_type == "post"])

// Published today:
*[(_type == "post") && ((publishedAt >= "2018-10-06T00:00:00.0000000+02:00"))]
```
### 2. Projections
LINQ selections are also supported by Sanity Linq:
``` csharp
// Returns a list of strings ordered by publish date
var postTitles = sanity.DocumentSet<Post>()
                       .OrderByDescending(p => p.PublishedAt)
                       .Select(p => p.Title)
                       .ToList();

```

### 3. Mutations
Mutations such as insert, update and delete can be performed on single documents or on multiple objects using a query!

**Insert document:**
``` csharp
var author = new Author
{
    Id = Guid.NewGuid().ToString(),
    Name = "Joe Bloggs",
};

await sanity.DocumentSet<Author>().Create(author).CommitAsync();

```

**Update document:**
``` csharp
var authors = sanity.DocumentSet<Author>();

var author = await authors.GetAsync("some-guid");
author.Name = "William Bloggs";

await authors.Update(author).CommitAsync();

```

**Delete document:**
``` csharp
var authors = sanity.DocumentSet<Author>();

// Delete by id
await authors.DeleteById("some-guid").CommitAsync();


```

**Delete multiple documents by query:**
``` csharp
var posts = sanity.DocumentSet<Post>();

// Delete by query
await posts.Where(p => p.Title.Contains("boring")).Delete().CommitAsync();

// Same as above:
await posts.DeleteByQuery(p => p.Title.Contains("boring")).CommitAsync();

```

**Patch document by id:**
``` csharp
var posts = sanity.DocumentSet<Post>();

// Patch title by id
posts.PatchById("some-guid", p => p.Set = new { Title = "New Title" }).CommitAsync();


```

**Patch document by query:**
``` csharp
var posts = sanity.DocumentSet<Post>();

// Patch title by query
posts.Where(p => p.Title == "").Patch(p => p.Set = new { Title = "Untitled" }).CommitAsync();

// Same as above:
posts.PatchByQuery(p => p.Title == "", p => p.Set = new { Title = "Untitled" }).CommitAsync();


```


### 4. Transactions - Bulk Mutations
The SanityDataContext keeps track of pending mutations, independent of type. To apply multiple mutations in a single transaction, simply wait with calling `CommitAsync()` until all mutations have been applied.

The fluent API also allows mutations to be chained.
``` csharp
var posts = sanity.DocumentSet<Post>();
var authors = sanity.DocumentSet<Author>();

// Delete and patch posts
posts.DeleteByQuery(p => p.Title == "")
     .PatchByQuery(p => p, p.LastUpdated = DateTime.Now)

// Add authors
authors.Create(new Author() { ... });

// Commit all changes 
await sanity.CommitAsync();


```

### 5. Joins 
The Sanity Linq library includes a helper class `SanityReference` which models the structure of relations in Sanity:

``` csharp
// Example: Post related to Author and a list of Categories
public class Post : SanityDocument
{
    public string Title { get; set; }

    public SanityReference<Author> Author { get; set; }

    public SanityImage MainImage { get; set; }

    public List<SanityReference<Category>> Categories { get; set; }
}

````

Related documents can be included in query results in several ways.

1. Relations are *automatically* followed by referencing `.Value` in projections:
    ``` csharp
    sanity.DocumentSet<Post>().Select(p => new { p.Title, p.Author.Value.Name });
    ```

2. The `[Include]` attribute can be placed above the property if it should always be included:
    ``` csharp
    [Include]
    public SanityReference<Author> Author { get; set; }

    [Include]
    public SanityImage MainImage { get; set; }

    [Include]
    public List<SanityReference<Category>> Categories { get; set; }

    ```
  > Note that `[Include]` not only works on `SanityReference`, but also lists of references and `SanityImage`.

3.  References can also be *selectively* joined, using the `Include()` extension method when querying:
    ``` csharp
    await sanity.DocumentSet<Post>().Include(p => p.Author).ToListAsync();

    ```

### 6. Files and Images
The `SanityDataContext` has two predefined document sets for Files and Images. These document sets can be used to manages Sanity assets:

#### Retrieving Files and Images
 ``` csharp
// Get all files and images:
var files = await sanity.Files.ToListAsync(); 
var images = await sanity.Images.ToListAsync();    
 ```

#### Uploading and Linking Assets
The `Files`and `Images` document sets also support uploading new assets, both using a `Stream`or by simply providing a source URL.

 ``` csharp
// Example: upload image and link to new document

// Upload new image
var imageUri = new Uri("https://www.sanity.io/static/images/opengraph/social.png");
var image = (await sanity.Images.UploadAsync(imageUri)).Document;

// Link image to new author
var author = new Author()
{
    Name = "Joe Bloggs",
    Image = new SanityImage
    {
        Asset = new SanityReference<SanityImageAsset> { Ref = image.Id },            
    }
};

await sanity.DocumentSet<Author>().Create(author).CommitAsync();

    
 ```

### 7. Debugging
In order to see the raw GROQ query for a particular LINQ query, simply call `GetSanityQuery()`.
```csharp
var groq = sanity.DocumentSet<Post>().Where(p => p.PublishedAt >= DateTime.Today).GetSanityQuery();

```
GROQ queries can be tested directly in the Sanity UI using the Vision plugin: <https://www.sanity.io/docs/front-ends/the-vision-plugin>


### 8. Raw Client
The `SanityClient` class can be used for making "raw" GROQ requests to Sanity:
```csharp
var client = new SanityClient(options);
await result = await client.FetchAsync("*[_type == "post"]");
```
-------

## Contribute
Feel free to submit pull-requests to the Sanity LINQ project! 


### Licence

[GNU General Public License v3.0](./LICENCE)

**Licence Summary**

You may copy, distribute and modify the software as long as you track changes/dates in source files. Any modifications to or software including (via compiler) GPL-licensed code must also be made available under the GPL along with build & install instructions.


