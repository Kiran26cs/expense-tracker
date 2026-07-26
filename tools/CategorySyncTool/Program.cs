using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

// ── Args ────────────────────────────────────────────────────────────────────
bool apply    = args.Contains("--apply");
bool dryRun   = !apply;
string? connStr = "mongodb://exptr-cosmos-si:2GgAiwBtdA6OCkiBrsM8b57l4asRxjs8wYmUoKc2aDWz9lyAGwTv76azcx0YaUftEoJJAkdThDbLACDb52aHMw%3D%3D@exptr-cosmos-si.mongo.cosmos.azure.com:10255/?ssl=true&replicaSet=globaldb&retrywrites=false&maxIdleTimeMS=120000&appName=@exptr-cosmos-si@";

if (string.IsNullOrEmpty(connStr))
{
    Console.Error.WriteLine("ERROR: Provide MongoDB connection string via:");
    Console.Error.WriteLine("  --connection <string>");
    Console.Error.WriteLine("  or env var MONGODB_CONNECTION_STRING");
    return 1;
}

string? dbName = args.SkipWhile(a => a != "--db").Skip(1).FirstOrDefault()
              ?? Environment.GetEnvironmentVariable("MONGODB_DATABASE")
              ?? "ExpenseTrackerDB";

Console.WriteLine($"Mode    : {(dryRun ? "DRY RUN (pass --apply to write)" : "APPLY")}");
Console.WriteLine($"Database: {dbName}");
Console.WriteLine();

// ── Connect ─────────────────────────────────────────────────────────────────
var client   = new MongoClient(connStr);
var db       = client.GetDatabase(dbName);
var catColl  = db.GetCollection<CategoryDoc>("categories");
var bookColl = db.GetCollection<ExpenseBookDoc>("expenseBooks");

// ── Load defaults ────────────────────────────────────────────────────────────
var defaults = await catColl
    .Find(Builders<CategoryDoc>.Filter.Eq(c => c.IsDefault, true))
    .ToListAsync();

if (defaults.Count == 0)
{
    Console.WriteLine("No default categories found (isDefault=true). Nothing to sync.");
    return 0;
}

Console.WriteLine($"Found {defaults.Count} default categories:");
foreach (var d in defaults.OrderBy(d => d.Type).ThenBy(d => d.Name))
    Console.WriteLine($"  [{d.Type,-8}] {d.Name}");
Console.WriteLine();

// ── Load all expense books ───────────────────────────────────────────────────
var books = await bookColl
    .Find(Builders<ExpenseBookDoc>.Filter.Empty)
    .ToListAsync();

Console.WriteLine($"Found {books.Count} expense book(s).\n");

int totalAdded = 0;
int totalBooks = 0;

foreach (var book in books)
{
    // Load existing categories for this book
    var existing = await catColl
        .Find(Builders<CategoryDoc>.Filter.Eq(c => c.ExpenseBookId, book.Id))
        .ToListAsync();

    // Index by "name::type" for fast lookup
    var existingKeys = existing
        .Select(c => $"{c.Name.Trim().ToLowerInvariant()}::{c.Type.ToLowerInvariant()}")
        .ToHashSet();

    var toAdd = defaults
        .Where(d => !existingKeys.Contains($"{d.Name.Trim().ToLowerInvariant()}::{d.Type.ToLowerInvariant()}"))
        .ToList();

    if (toAdd.Count == 0)
    {
        Console.WriteLine($"Book [{book.Id}] \"{book.Name}\" — already up to date.");
        continue;
    }

    totalBooks++;
    Console.WriteLine($"Book [{book.Id}] \"{book.Name}\" — {toAdd.Count} category/ies to add:");
    foreach (var cat in toAdd)
        Console.WriteLine($"  + [{cat.Type,-8}] {cat.Name}");

    if (apply)
    {
        var newDocs = toAdd.Select(d => new CategoryDoc
        {
            Id            = ObjectId.GenerateNewId().ToString(),
            ExpenseBookId = book.Id,
            Name          = d.Name,
            Type          = d.Type,
            Icon          = d.Icon,
            Color         = d.Color,
            IsDefault     = false,
            FinancialClass= d.FinancialClass,
            CreatedAt     = DateTime.UtcNow,
        }).ToList();

            
        totalAdded += newDocs.Count;
        Console.WriteLine($"  → Inserted {newDocs.Count} categor{(newDocs.Count == 1 ? "y" : "ies")}.");
    }

    Console.WriteLine();
}

Console.WriteLine("─────────────────────────────────────────────");
if (dryRun)
{
    Console.WriteLine($"DRY RUN complete. {books.Count} book(s) scanned, {totalBooks} would be updated.");
    Console.WriteLine("Run with --apply to persist changes.");
}
else
{
    Console.WriteLine($"Done. {totalAdded} categor{(totalAdded == 1 ? "y" : "ies")} added across {totalBooks} book(s).");
}

return 0;

// ── Models ───────────────────────────────────────────────────────────────────

[BsonIgnoreExtraElements]
class CategoryDoc
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("expenseBookId")]
    public string? ExpenseBookId { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = "expense";

    [BsonElement("icon")]
    public string Icon { get; set; } = "fa-solid fa-tag";

    [BsonElement("color")]
    public string Color { get; set; } = "#6366f1";

    [BsonElement("isDefault")]
    public bool IsDefault { get; set; }

    [BsonElement("financialClass")]
    [BsonIgnoreIfNull]
    public string? FinancialClass { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements]
class ExpenseBookDoc
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
}
