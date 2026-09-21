using MyInstantsApi;

var client = new MyInstantsClient();

Console.WriteLine("=== Search: vine boom ===");
var results = await client.SearchAsync("vine boom");
Console.WriteLine($"Found {results.Count} sounds");
foreach (var s in results.Take(3))
    Console.WriteLine($"  [{s.Id}] {s.Title} -> {s.Mp3}");

Console.WriteLine();
Console.WriteLine("=== Recent ===");
var recent = await client.GetRecentAsync();
Console.WriteLine($"Found {recent.Count} sounds");
foreach (var s in recent.Take(3))
    Console.WriteLine($"  [{s.Id}] {s.Title} -> {s.Mp3}");

Console.WriteLine();
Console.WriteLine("=== Trending (id) ===");
var trending = await client.GetTrendingAsync("id");
Console.WriteLine($"Found {trending.Count} sounds");
foreach (var s in trending.Take(3))
    Console.WriteLine($"  [{s.Id}] {s.Title} -> {s.Mp3}");

foreach (var id in new[] { "vine-boom-sound-70972", "weed-legalize-in-genova-city-14302" })
{
    Console.WriteLine();
    Console.WriteLine($"=== Detail: {id} ===");
    var detail = await client.GetDetailAsync(id);
    Console.WriteLine($"Title: {detail.Title}");
    Console.WriteLine($"Mp3: {detail.Mp3}");
    Console.WriteLine($"Description: {detail.Description}");
    Console.WriteLine($"Tags: {string.Join(", ", detail.Tags)}");
    Console.WriteLine($"Favorites: {detail.Favorites}");
    Console.WriteLine($"Views: {detail.Views}");
    Console.WriteLine($"Uploader: {detail.Uploader.Username} ({detail.Uploader.Url})");
}

Console.WriteLine();
Console.WriteLine("=== Favorites/Uploaded: sto17 ===");
var favs = await client.GetFavoritesAsync("sto17");
Console.WriteLine($"Favorites count: {favs.Count}");
var uploaded = await client.GetUploadedAsync("sto17");
Console.WriteLine($"Uploaded count: {uploaded.Count}");
