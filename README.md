# MyInstantsApi

Thư viện C# (.NET 8+) gọi trực tiếp [myinstants.com](https://www.myinstants.com) từ máy client — không cần server trung gian. Là bản port client-side của [abdipr/myinstants-api](https://github.com/abdipr/myinstants-api) (vốn viết bằng PHP để chạy trên server).

## Vì sao gọi bằng curl.exe thay vì HttpClient?

myinstants.com nằm sau Cloudflare. Cloudflare chặn request dựa trên TLS fingerprint (JA3) — `HttpClient` của .NET bị chặn (403) dù User-Agent giống hệt trình duyệt thật, trong khi `curl` thì không. Thư viện này gọi `curl.exe` qua `Process` để lấy HTML, rồi dùng `HtmlAgilityPack` để parse.

## Yêu cầu

- App tham chiếu thư viện phải chạy trên **.NET 8.0 trở lên**.
- Máy chạy app cần có **`curl.exe`** trong PATH. Windows 10 (từ bản 1803) và Windows 11 có sẵn tại `C:\Windows\System32\curl.exe`. Nếu không có, truyền đường dẫn riêng: `new MyInstantsClient(curlPath: @"C:\path\to\curl.exe")`.

## Cài đặt

### Cách 1: Build từ source
```
git clone https://github.com/thaovd/MyInstantsApi_Lib.git
cd MyInstantsApi_Lib/MyInstantsApi
dotnet build -c Release
```
Kết quả nằm ở `MyInstantsApi/bin/Release/net8.0/`, gồm `MyInstantsApi.dll` và `HtmlAgilityPack.dll` (dependency bắt buộc — thiếu file này sẽ lỗi khi gọi hàm).

### Cách 2: Gắn thẳng DLL vào project khác
Copy cả 2 file `MyInstantsApi.dll` và `HtmlAgilityPack.dll` vào project đích, rồi thêm vào `.csproj`:
```xml
<ItemGroup>
  <Reference Include="MyInstantsApi">
    <HintPath>libs\MyInstantsApi.dll</HintPath>
  </Reference>
  <Reference Include="HtmlAgilityPack">
    <HintPath>libs\HtmlAgilityPack.dll</HintPath>
  </Reference>
</ItemGroup>
```

### Cách 3: Project Reference (nếu cùng solution)
```xml
<ItemGroup>
  <ProjectReference Include="..\MyInstantsApi\MyInstantsApi.csproj" />
</ItemGroup>
```

## Sử dụng

```csharp
using MyInstantsApi;

var client = new MyInstantsClient();

// Tìm kiếm
List<Sound> results = await client.SearchAsync("vine boom");
foreach (var s in results)
    Console.WriteLine($"{s.Title} -> {s.Mp3}");

// Chi tiết 1 sound (id lấy từ Sound.Id ở các hàm danh sách)
SoundDetail detail = await client.GetDetailAsync("vine-boom-sound-70972");

// Danh sách khác
List<Sound> recent   = await client.GetRecentAsync();
List<Sound> trending = await client.GetTrendingAsync("id");   // mã quốc gia/khu vực
List<Sound> best     = await client.GetBestAsync("id");
List<Sound> favs     = await client.GetFavoritesAsync("hellmouz");
List<Sound> uploaded = await client.GetUploadedAsync("hellmouz");
```

## Model dữ liệu

```csharp
class Sound
{
    string Id;    // dùng cho GetDetailAsync
    string Title;
    string Url;   // link trang sound trên myinstants.com
    string Mp3;   // link file mp3, tải/phát trực tiếp được
}

class SoundDetail
{
    string Id;
    string Url;
    string Title;
    string Mp3;
    string Description;
    List<string> Tags;   // hiện luôn rỗng — myinstants.com đã bỏ tính năng tag khỏi trang chi tiết
    string Favorites;
    string Views;
    Uploader Uploader;    // Username/Url rỗng nếu sound không hiển thị người upload
}

class Uploader
{
    string Username;
    string Url;
}
```

## Xử lý lỗi

Mọi lỗi (mất mạng, trang không tồn tại, curl không chạy được, tham số rỗng...) ném `MyInstantsException` hoặc `ArgumentException`:

```csharp
try
{
    var results = await client.SearchAsync(query);
}
catch (MyInstantsException ex)
{
    Console.WriteLine($"Lỗi khi gọi myinstants: {ex.Message}");
}
```

## Cấu trúc repo

```
MyInstantsApi/    thư viện chính
TestConsole/      console app mẫu, minh hoạ cách gọi từng hàm
```
