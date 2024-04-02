using EpicGamesLauncher.Notifications.Models;

using EpicManifestParser;
using EpicManifestParser.Api;
using EpicManifestParser.UE;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using ZlibngDotNet;

namespace EpicGamesLauncher.Notifications;

class Program
{
    private const string AuthBaseUrl = "https://account-public-service-prod.ol.epicgames.com/account/api/oauth";
    private const string LauncherAssetsUrl = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/Windows/launcher";

    // launcherAppClient2
    private const string LauncherClientId = "34a02cf8f4414e29b15921876da36f9a";
    private const string LauncherClientSecret = "daafbccc737745039dffe53d94fc76cf";

    private const string LauncherContentApp = "EpicGamesLauncherContent";
    private const string LauncherContentNotiFile = "BuildNotificationsV2.json";

    private const string LabelArg = "--label";

    private static readonly HttpClient _apiClient = new();
    private static readonly string _libsPath = Path.Combine(Environment.CurrentDirectory, "Libs");
    private static readonly string _cachePath = Path.Combine(Environment.CurrentDirectory, "Cache");

    public static void Main(string[] args)
    {
        CancellationTokenSource csrc = new();
        CancellationToken token = csrc.Token;

        MainAsync(args, token).GetAwaiter().GetResult();
    }

    private static async Task MainAsync(string[] args, CancellationToken token)
    {
        AuthTokenResponse auth = await GetAuthAsync(token);
        FBuildPatchAppManifest manifest = await GetManifestAsync(args, auth, token);
        FFileManifest fileManifest = manifest.FileManifestList.First(x => x.FileName == LauncherContentNotiFile);
        FFileManifestStream fileStream = fileManifest.GetStream(false);

        byte[] fileBytes = await fileStream.SaveBytesAsync(cancellationToken: token);
        string fileContent = Encoding.UTF8.GetString(fileBytes);

        BuildNotificationsData buildNotificationData = JsonSerializer.Deserialize<BuildNotificationsData>(fileContent)!;

        foreach (BuildNotification notification in buildNotificationData.BuildNotifications)
        {
            FFileManifest? imageFile = manifest.FileManifestList.FirstOrDefault(x => x.FileName == notification.ImagePath);

            if (imageFile is not null)
            {
                using FileStream imgFileStream = new($"./~EGL-Notifications-Image.{imageFile.FileName}", FileMode.OpenOrCreate, FileAccess.Write);
                using FFileManifestStream imgStream = imageFile!.GetStream();

                await imgStream.SaveToAsync(imgFileStream, cancellationToken: token);
            }
        }

        // TODO: json beautify
        File.WriteAllText("./~EGL-Notifications.json", fileContent);

        // Cleanup
        await KillAuthAsync(auth, token);
    }

    private static string? GetArg(string[] args, string argName)
    {
        string? arg = args.FirstOrDefault(x => x.StartsWith(argName));

        // +1, because we need to substring after "="
        return arg?[(argName.Length + 1)..];
    }

    private static async Task EnsureSuccessResponseAsync(HttpResponseMessage res, CancellationToken token)
    {
        if (!res.IsSuccessStatusCode)
        {
            StringBuilder builder = new();

            if (res.RequestMessage is not null)
            {
                builder.Append($"Request {res.RequestMessage.Method} {res.RequestMessage.RequestUri}");
            }
            else
            {
                builder.Append($"Request");
            }

            builder.Append(' ');
            builder.Append($"failed with status {(int)res.StatusCode} {res.ReasonPhrase ?? res.StatusCode.ToString()}");

            if (res.Content is not null
                && res.Content.Headers.ContentLength is not null and > 0)
            {
                builder.AppendLine();
                builder.Append(await res.Content.ReadAsStringAsync(token));
            }

            throw new HttpRequestException(builder.ToString(), null, res.StatusCode);
        }
    }

    private static async Task<T?> ParseResponseAsync<T>(HttpResponseMessage res, CancellationToken token) where T : class
    {
        await EnsureSuccessResponseAsync(res, token);

        return await res.Content.ReadFromJsonAsync<T>(cancellationToken: token);
    }

    private static async Task<AuthTokenResponse> GetAuthAsync(CancellationToken token)
    {
        HttpRequestMessage request = new()
        {
            RequestUri = new Uri($"{AuthBaseUrl}/token"),
            Method = HttpMethod.Post,
            Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new("grant_type", "client_credentials"),
                new("token_type", "eg1"),
            }),
        };

        byte[] basicAuthBytes = Encoding.UTF8.GetBytes($"{LauncherClientId}:{LauncherClientSecret}");
        string basicAuthBase64 = Convert.ToBase64String(basicAuthBytes);
        request.Headers.Authorization = new AuthenticationHeaderValue("basic", basicAuthBase64);

        HttpResponseMessage res = await _apiClient.SendAsync(request, token);
        AuthTokenResponse? auth = await ParseResponseAsync<AuthTokenResponse>(res, token);

        ArgumentNullException.ThrowIfNull(auth);

        return auth;
    }

    private static async Task KillAuthAsync(AuthTokenResponse auth, CancellationToken token)
    {
        HttpRequestMessage request = new()
        {
            RequestUri = new Uri($"{AuthBaseUrl}/sessions/kill/{auth.AccessToken}"),
            Method = HttpMethod.Delete,
        };

        request.Headers.Authorization = new AuthenticationHeaderValue(auth.TokenType, auth.AccessToken);

        HttpResponseMessage res = await _apiClient.SendAsync(request, token);

        await EnsureSuccessResponseAsync(res, token);
    }

    private static async Task<FBuildPatchAppManifest> GetManifestAsync(string[] args, AuthTokenResponse auth, CancellationToken token)
    {
        string label = GetArg(args, LabelArg) ?? "Live";

        HttpRequestMessage request = new()
        {
            RequestUri = new Uri($"{LauncherAssetsUrl}?label={label}"),
            Method = HttpMethod.Get,
        };

        request.Headers.Authorization = new AuthenticationHeaderValue(auth.TokenType, auth.AccessToken);

        HttpResponseMessage res = await _apiClient.SendAsync(request, token);

        await EnsureSuccessResponseAsync(res, token);

        ManifestInfo? manifestInfo = await res.Content.ReadManifestInfoAsync(token);
        ArgumentNullException.ThrowIfNull(manifestInfo);

        (FBuildPatchAppManifest manifest, ManifestInfoElement infoElement) = await manifestInfo.DownloadAndParseAsync(
            elementPredicate: x => x.AppName == LauncherContentApp,
            optionsBuilder: (opt) =>
            {
                if (OperatingSystem.IsWindows())
                {
                    opt.Zlibng = new Zlibng(Path.Combine(_libsPath, "zlib-ng2.dll"));
                }
                else if (OperatingSystem.IsLinux())
                {
                    opt.Zlibng = new Zlibng(Path.Combine(_libsPath, "libz-ng.so"));
                }
                else
                {
                    throw new PlatformNotSupportedException();
                }

                if (!Directory.Exists(_cachePath))
                {
                    Directory.CreateDirectory(_cachePath);
                }

                opt.ChunkBaseUrl = $"http://epicgames-download1.akamaized.net/Builds/UnrealEngineLauncher/CloudDir/";
                opt.ManifestCacheDirectory = _cachePath;
                opt.ChunkCacheDirectory = _cachePath;
            },
            cancellationToken: token);

        return manifest;
    }
}
