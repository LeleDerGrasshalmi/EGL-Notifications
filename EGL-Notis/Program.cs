using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using EpicManifestParser.Objects;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EGL_Notis
{
    class Program
    {
        public static string _token = "";
        public static string _manifesturl = "";

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            GetAccessToken();
            string manifestURL = GetManifestUrl();
            Console.WriteLine(manifestURL);

            byte[] manifestData = GetManifest();
            File.WriteAllBytes("data.manifest", manifestData);
            EpicManifestParser.Objects.Manifest manifest = new EpicManifestParser.Objects.Manifest(manifestData, new ManifestOptions
            {
                ChunkBaseUri = new Uri("http://epicgames-download1.akamaized.net/Builds/UnrealEngineLauncher/CloudDir/ChunksV4/", UriKind.Absolute),
                ChunkCacheDirectory = Directory.CreateDirectory("./~Chunks")
            });

            FileManifest notificationsFile = manifest.FileManifests.Find(x => x.Name == "BuildNotificationsV2.json");
            FileManifestStream notiManifestFileStream = notificationsFile!.GetStream();

            MemoryStream fileMemoryStream = new MemoryStream((int)notiManifestFileStream.Length);
            notiManifestFileStream.Save(fileMemoryStream);

            string notiFileStr = Encoding.Default.GetString(fileMemoryStream.ToArray());
            NotificationsRoot notiFile = JsonConvert.DeserializeObject<NotificationsRoot>(notiFileStr);

            List<Notification> fortniteNotis = notiFile.BuildNotifications.Where(e => e.DisplayCondition.Contains("IsEntitled(48ff3f41680e403bb2717737f68731c5)")).ToList();
            foreach (Notification noti in fortniteNotis)
            {
                var img = manifest.FileManifests.Find(x => x.Name == noti.ImagePath);
                if (img != null)
                {
                    using (var imgFileStream = new FileStream($"./~EGL-Notifications-Image.{img.Name}", FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        using (var imgStream = img!.GetStream())
                        {
                            imgStream.Save(imgFileStream);
                        }
                    }
                }
            }
            File.WriteAllText("./~EGL-Notifications.FN.json", JsonConvert.SerializeObject(notiFile, Formatting.Indented));
            File.WriteAllText("./~EGL-Notifications.FN.txt", notiFileStr);
        }

        private static string GetAccessToken()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token");
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            request.Method = "POST";

            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = "UELauncher/13.1.2-18458102+++Portal+Release-Live Windows/10.0.19042.1.256.64bit";
            request.Headers.Add("X-Epic-Correlation-ID", "UE4-9d44f1444730e0ab67b97c96877fd423-4456F05F406FD9A4D2DC75A6EC8D70CF-5B619D4544431FD6636BF69374BDCA3D");
            request.Headers.Add("Authorization", "basic MzRhMDJjZjhmNDQxNGUyOWIxNTkyMTg3NmRhMzZmOWE6ZGFhZmJjY2M3Mzc3NDUwMzlkZmZlNTNkOTRmYzc2Y2Y=");

            var body = Encoding.ASCII.GetBytes($"grant_type=client_credentials&token_type=eg1");
            using (var stream = request.GetRequestStream())
            {
                stream.Write(body, 0, body.Length);
            }

            HttpWebResponse response;
            try { response = (HttpWebResponse)request.GetResponse(); } catch (WebException ee) { response = (HttpWebResponse)ee.Response; }

            _token = ((dynamic)JsonConvert.DeserializeObject(new StreamReader(response.GetResponseStream()).ReadToEnd())).access_token;
            return _token;
        }

        private static string GetManifestUrl()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/Windows/launcher?label=Live");
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = "UELauncher/13.1.2-18458102+++Portal+Release-Live Windows/10.0.19042.1.256.64bit";
            request.Headers.Add("X-Epic-Correlation-ID", "UE4-9d44f1444730e0ab67b97c96877fd423-4456F05F406FD9A4D2DC75A6EC8D70CF-5B619D4544431FD6636BF69374BDCA3D");
            request.Headers.Add("Authorization", $"bearer {GetAccessToken()}");

            HttpWebResponse response;
            try { response = (HttpWebResponse)request.GetResponse(); } catch (WebException ee) { response = (HttpWebResponse)ee.Response; }

            var raw = new StreamReader(response.GetResponseStream()).ReadToEnd();
            root data = System.Text.Json.JsonSerializer.Deserialize<root>(raw);
            Manifest manifest = data.elements.Find(e => e.appName == "EpicGamesLauncherContent").manifests[0];

            List<queryParam> queryParams = manifest.queryParams;
            string baseURL = manifest.uri;
            string query = String.Join("&", queryParams.ConvertAll<string>(e => $"{e.name}={e.value}").ToArray());

            _manifesturl = $"{baseURL}?{query}";
            return _manifesturl;
        }

        private static byte[] GetManifest()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_manifesturl);
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            HttpWebResponse response;
            try { response = (HttpWebResponse)request.GetResponse(); } catch (WebException ee) { response = (HttpWebResponse)ee.Response; }

            MemoryStream ms = new MemoryStream();
            response.GetResponseStream().CopyTo(ms);
            return ms.ToArray();
        }
    }

    class queryParam
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    class root
    {
        public List<ManifestEntry> elements { get; set; }
    }

    class ManifestEntry
    {
        public string appName { get; set; }
        public string labelName { get; set; }
        public string buildVersion { get; set; }
        public string hash { get; set; }
        public List<Manifest> manifests { get; set; }
    }

    class Manifest
    {
        public string uri { get; set; }
        public List<queryParam> queryParams { get; set; }
    }

    class NotificationsRoot
    {
        public List<Notification> BuildNotifications { get; set; }
    }

    class Notification
    {
        public string NotificationId { get; set; }
        public string DisplayCondition { get; set; }
        public string LayoutPath { get; set; }
        public string DismissId { get; set; }
        public string ImagePath { get; set; }
        public string UriLink { get; set; }

        public bool IsAdvert { get; set; }
        public bool IsFreeGame { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> AccountCountryBlackList { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object> Extensions {  get; set; }
    }
}
