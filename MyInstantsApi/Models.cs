namespace MyInstantsApi;

public class Sound
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string Mp3 { get; set; } = "";
}

public class Uploader
{
    public string Username { get; set; } = "";
    public string Url { get; set; } = "";
}

public class SoundDetail
{
    public string Id { get; set; } = "";
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string Mp3 { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public string Favorites { get; set; } = "";
    public string Views { get; set; } = "";
    public Uploader Uploader { get; set; } = new();
}
