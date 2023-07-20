using Entities;
using Newtonsoft.Json;
using OF_DRM_Video_Downloader.Entities;
using OF_DRM_Video_Downloader.Helpers;
using Spectre.Console;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OF_DRM_Video_Downloader
{
    public class Program
    {
        public static Auth? auth = JsonConvert.DeserializeObject<Auth>(File.ReadAllText("auth.json"));
        public static List<long> paid_post_ids = new List<long>();
        private static IAPIHelper apiHelper;
        private static IDBHelper dBHelper;
        private static IDownloadHelper downloadHelper;
        static Program()
        {
            apiHelper = new APIHelper();
            dBHelper = new DBHelper();
            downloadHelper = new DownloadHelper();
        }

        public static async Task Main()
        {
            try
            {
                AnsiConsole.Write(new FigletText("OF-DRM").Color(Color.Red));

                if (!File.Exists(auth.YTDLP_PATH))
                {
                    AnsiConsole.Markup($"[red]Cannot locate yt-dlp.exe with specified path {auth.YTDLP_PATH}, please modify auth.json with the correct path, press any key to exit[/]");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
                else
                {
                    AnsiConsole.Markup($"[green]yt-dlp.exe located successfully![/]\n");
                }

                if (!File.Exists(auth.FFMPEG_PATH))
                {
                    AnsiConsole.Markup($"[red]Cannot locate ffmpeg.exe with specified path {auth.FFMPEG_PATH}, please modify auth.json with the correct path, press any key to exit[/]");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
                else
                {
                    AnsiConsole.Markup($"[green]ffmpeg.exe located successfully![/]\n");
                }

                if (!File.Exists(auth.MP4DECRYPT_PATH))
                {
                    AnsiConsole.Markup($"[red]Cannot locate mp4decrypt.exe with specified path {auth.MP4DECRYPT_PATH}, please modify auth.json with the correct path, press any key to exit[/]");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
                else
                {
                    AnsiConsole.Markup($"[green]mp4decrypt.exe located successfully![/]\n");
                }

                User me = await apiHelper.GetUserInfo("/users/me", auth);
                if (me.name == null && me.username == null)
                {
                    AnsiConsole.Markup($"[red]Auth failed, please check the values in auth.json are correct, press any key to exit[/]");
                    Console.ReadKey();
                }
                else
                {
                    AnsiConsole.Markup($"[green]Logged In successfully as {me.name} {me.username}\n[/]");
                    do
                    {
                        DateTime startTime = DateTime.Now;
                        Dictionary<string, int> users = await apiHelper.GetSubscriptions("/subscriptions/subscribes", auth.IncludeExpiredSubscriptions, auth);
                        Dictionary<string, int> lists = await apiHelper.GetLists("/lists", auth);
                        Dictionary<string, int> selectedUsers = new Dictionary<string, int>();
                        // Call the HandleUserSelection method to handle user selection and processing
                        KeyValuePair<bool, Dictionary<string, int>> hasSelectedUsersKVP = await HandleUserSelection(selectedUsers, users, lists);

                        if (hasSelectedUsersKVP.Key && !hasSelectedUsersKVP.Value.ContainsKey("AuthChanged"))
                        {
                            // Iterate over each user in the list of users
                            foreach (KeyValuePair<string, int> user in hasSelectedUsersKVP.Value)
                            {
                                AnsiConsole.Markup($"[red]\nScraping Data for {user.Key}\n[/]");
                                string path = "";
                                if (!string.IsNullOrEmpty(auth.DownloadPath))
                                {
                                    path = System.IO.Path.Combine(auth.DownloadPath, user.Key);
                                }
                                else
                                {
                                    path = $"__user_data__/sites/OnlyFans/{user.Key}"; // specify the path for the new folder
                                }
                                if (!Directory.Exists(path))
                                {
                                    Directory.CreateDirectory(path);
                                    AnsiConsole.Markup($"[red]Created folder for {user.Key}\n[/]");
                                }
                                else
                                {
                                    AnsiConsole.Markup($"[red]Folder for {user.Key} already created\n[/]");
                                }
                                User user_info = await apiHelper.GetUserInfo($"/users/{user.Key}", auth);
                                await dBHelper.CreateDB(path);
                                if (auth.DownloadPaidPosts)
                                {
                                    AnsiConsole.Markup($"[red]Getting Paid Posts\n[/]");
                                    PaidPostCollection paidPosts = await apiHelper.GetPaidPostVideos("/posts/paid", user.Key, path, auth);
                                    if (paidPosts != null && paidPosts.Video_URLS.Count > 0 && paidPosts.PaidPosts.Count > 0)
                                    {
                                        AnsiConsole.Markup($"[red]Found {paidPosts.Video_URLS.Count} Paid Posts with DRM Video(s)\n[/]");
                                        int oldPaidPostCount = 0;
                                        int newPaidPostCount = 0;
                                        var selectedPaidPostsPrompt = new MultiSelectionPrompt<string>();
                                        selectedPaidPostsPrompt.PageSize(10);
                                        selectedPaidPostsPrompt.AddChoice("[red]None[/]");
                                        selectedPaidPostsPrompt.AddChoice("[red]All[/]");
                                        foreach (KeyValuePair<long, DateTime> p in paidPosts.PaidPosts)
                                        {
                                            selectedPaidPostsPrompt.AddChoice($"[red]{string.Format("Post ID: {0} Posted At DateTime: {1}", p.Key, p.Value.ToString("dd/MM/yyy HH:mm:ss"))}[/]");
                                        }
                                        var paidPostSelection = AnsiConsole.Prompt(selectedPaidPostsPrompt);
                                        List<string> videos_to_download = new List<string>();
                                        if (paidPostSelection.Contains("[red]None[/]"))
                                        {
                                            AnsiConsole.Markup("[red]You selected to download 0 paid post videos[/]\n");
                                        }
                                        else if (paidPostSelection.Contains("[red]All[/]"))
                                        {
                                            AnsiConsole.Markup("[red]You selected to download all paid post videos[/]\n");
                                            foreach (KeyValuePair<long, DateTime> p in paidPosts.PaidPosts)
                                            {
                                                paid_post_ids.Add(p.Key);
                                                videos_to_download.AddRange(paidPosts.Video_URLS[p.Key]);
                                            }
                                        }
                                        else
                                        {
                                            AnsiConsole.Markup($"[red]You selected to download {paidPostSelection.Count} paid post videos[/]\n");
                                            foreach (string video in paidPostSelection)
                                            {
                                                string pattern = @"Post ID: (\d+)";
                                                Match match = Regex.Match(video, pattern);
                                                if (match.Success)
                                                {
                                                    long postId = Convert.ToInt64(match.Groups[1].Value);
                                                    paid_post_ids.Add(postId);
                                                    videos_to_download.AddRange(paidPosts.Video_URLS[postId]);
                                                }
                                            }
                                        }
                                        if (videos_to_download.Count > 0)
                                        {
                                            await AnsiConsole.Progress().Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new DownloadedColumn(), new RemainingTimeColumn()).StartAsync(async ctx =>
                                            {
                                                var task = ctx.AddTask($"[red]Downloading {videos_to_download.Count} Video(s)[/]", autoStart: false);

                                                task.MaxValue = await downloadHelper.CalculateTotalFileSize(videos_to_download, auth);
                                                task.StartTask();
                                                foreach (string video in videos_to_download)
                                                {
                                                    bool isNew;
                                                    if (video.Contains("cdn3.onlyfans.com/dash/files"))
                                                    {
                                                        string[] messageUrlParsed = video.Split(',');
                                                        string mpdURL = messageUrlParsed[0];
                                                        string policy = messageUrlParsed[1];
                                                        string signature = messageUrlParsed[2];
                                                        string kvp = messageUrlParsed[3];
                                                        string mediaId = messageUrlParsed[4];
                                                        string postId = messageUrlParsed[5];
                                                        string? licenseURL = null;
                                                        string? pssh = await apiHelper.GetDRMMPDPSSH(mpdURL, policy, signature, kvp, auth);
                                                        if (pssh != null)
                                                        {
                                                            DateTime lastModified = await apiHelper.GetDRMMPDLastModified(mpdURL, policy, signature, kvp, auth);
                                                            Dictionary<string, string> drmHeaders = await apiHelper.Headers($"/api2/v2/users/media/{mediaId}/drm/post/{postId}", "?type=widevine", auth);
                                                            string decryptionKey = await apiHelper.GetDecryptionKeyNew(drmHeaders, $"https://onlyfans.com/api2/v2/users/media/{mediaId}/drm/post/{postId}?type=widevine", pssh);
                                                            isNew = await downloadHelper.DownloadPurchasedPostDRMVideo(auth.YTDLP_PATH, auth.MP4DECRYPT_PATH, auth.FFMPEG_PATH, auth.USER_AGENT, policy, signature, kvp, auth.COOKIE, mpdURL, decryptionKey, path, lastModified, Convert.ToInt64(mediaId), task);
                                                            if (isNew)
                                                            {
                                                                newPaidPostCount++;
                                                            }
                                                            else
                                                            {
                                                                oldPaidPostCount++;
                                                            }
                                                        }
                                                    }
                                                }
                                            });
                                            AnsiConsole.Markup($"[red]Paid Post DRM Videos Skipped/Already Downloaded: {oldPaidPostCount} New Paid Post DRM Videos Downloaded: {newPaidPostCount}[/]\n");
                                        }
                                    }
                                    else
                                    {
                                        AnsiConsole.Markup($"[red]Found 0 Paid Posts with DRM videos\n[/]");
                                    }
                                }
                                if (auth.DownloadPosts)
                                {
                                    AnsiConsole.Markup($"[red]Getting Posts\n[/]");
                                    PostCollection posts = await apiHelper.GetPostVideos($"/users/{user.Value}/posts/videos", path, auth);
                                    if (posts != null && posts.Video_URLS.Count > 0 && posts.Posts.Count > 0)
                                    {
                                        AnsiConsole.Markup($"[red]Found {posts.Video_URLS.Count} Posts with DRM Video(s)\n[/]");
                                        int oldPostCount = 0;
                                        int newPostCount = 0;
                                        var selectedPostsPrompt = new MultiSelectionPrompt<string>();
                                        selectedPostsPrompt.WrapAround = true;
                                        selectedPostsPrompt.PageSize(10);
                                        selectedPostsPrompt.AddChoice("[red]None[/]");
                                        selectedPostsPrompt.AddChoice("[red]All[/]");
                                        foreach (KeyValuePair<long, DateTime> p in posts.Posts)
                                        {
                                            selectedPostsPrompt.AddChoice($"[red]{string.Format("Post ID: {0} Posted At DateTime: {1}", p.Key, p.Value.ToString("dd/MM/yyy HH:mm:ss"))}[/]");
                                        }
                                        var postSelection = AnsiConsole.Prompt(selectedPostsPrompt);
                                        List<string> videos_to_download = new List<string>();
                                        if (postSelection.Contains("[red]None[/]"))
                                        {
                                            AnsiConsole.Markup("[red]You selected to download 0 post videos[/]\n");
                                        }
                                        else if (postSelection.Contains("[red]All[/]"))
                                        {
                                            AnsiConsole.Markup("[red]You selected to download all of the post videos[/]\n");
                                            foreach (KeyValuePair<long, DateTime> p in posts.Posts)
                                            {
                                                if (!paid_post_ids.Contains(p.Key))
                                                {
                                                    videos_to_download.AddRange(posts.Video_URLS[p.Key]);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            AnsiConsole.Markup($"[red]You selected to download {postSelection.Count} post videos[/]\n");
                                            foreach (string video in postSelection)
                                            {
                                                string pattern = @"Post ID: (\d+)";
                                                Match match = Regex.Match(video, pattern);
                                                if (match.Success)
                                                {
                                                    long postId = Convert.ToInt64(match.Groups[1].Value);
                                                    if (!paid_post_ids.Contains(postId))
                                                    {
                                                        videos_to_download.AddRange(posts.Video_URLS[postId]);
                                                    }
                                                }
                                            }
                                        }
                                        if (videos_to_download.Count > 0)
                                        {
                                            await AnsiConsole.Progress().Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new DownloadedColumn(), new RemainingTimeColumn()).StartAsync(async ctx =>
                                            {
                                                var task = ctx.AddTask($"[red]Downloading {videos_to_download.Count} Video(s)[/]", autoStart: false);

                                                task.MaxValue = await downloadHelper.CalculateTotalFileSize(videos_to_download, auth);
                                                task.StartTask();
                                                foreach (string video in videos_to_download)
                                                {
                                                    bool isNew;
                                                    if (video.Contains("cdn3.onlyfans.com/dash/files"))
                                                    {
                                                        string[] messageUrlParsed = video.Split(',');
                                                        string mpdURL = messageUrlParsed[0];
                                                        string policy = messageUrlParsed[1];
                                                        string signature = messageUrlParsed[2];
                                                        string kvp = messageUrlParsed[3];
                                                        string mediaId = messageUrlParsed[4];
                                                        string postId = messageUrlParsed[5];
                                                        string? licenseURL = null;
                                                        string? pssh = await apiHelper.GetDRMMPDPSSH(mpdURL, policy, signature, kvp, auth);
                                                        if (pssh != null)
                                                        {
                                                            DateTime lastModified = await apiHelper.GetDRMMPDLastModified(mpdURL, policy, signature, kvp, auth);
                                                            Dictionary<string, string> drmHeaders = await apiHelper.Headers($"/api2/v2/users/media/{mediaId}/drm/post/{postId}", "?type=widevine", auth);
                                                            string decryptionKey = await apiHelper.GetDecryptionKeyNew(drmHeaders, $"https://onlyfans.com/api2/v2/users/media/{mediaId}/drm/post/{postId}?type=widevine", pssh);
                                                            isNew = await downloadHelper.DownloadPostDRMVideo(auth.YTDLP_PATH, auth.MP4DECRYPT_PATH, auth.FFMPEG_PATH, auth.USER_AGENT, policy, signature, kvp, auth.COOKIE, mpdURL, decryptionKey, path, lastModified, Convert.ToInt64(mediaId), task);
                                                            if (isNew)
                                                            {
                                                                newPostCount++;
                                                            }
                                                            else
                                                            {
                                                                newPostCount++;
                                                            }
                                                        }
                                                    }
                                                }
                                            });
                                            AnsiConsole.Markup($"[red]Post DRM Videos Skipped/Already Downloaded: {oldPostCount} New Post DRM Videos Downloaded: {newPostCount}[/]\n");
                                        }
                                    }
                                    else
                                    {
                                        AnsiConsole.Markup($"[red]Found 0 Posts with DRM videos\n[/]");
                                    }
                                }
                                if (auth.DownloadArchived)
                                {
                                    AnsiConsole.Markup($"[red]Getting Archived Posts\n[/]");
                                    ArchivedCollection archived = await apiHelper.GetArchivedVideos($"/users/{user.Value}/posts", path, auth);
                                    if (archived != null && archived.Video_URLS.Count > 0 && archived.Archived.Count > 0)
                                    {
                                        AnsiConsole.Markup($"[red]Found {archived.Video_URLS.Count} Archived Posts with DRM Video(s)\n[/]");
                                        int oldArchivedCount = 0;
                                        int newArchivedCount = 0;
                                        var selectedArchivedPostsPrompt = new MultiSelectionPrompt<string>();
                                        selectedArchivedPostsPrompt.PageSize(10);
                                        selectedArchivedPostsPrompt.AddChoice("[red]None[/]");
                                        selectedArchivedPostsPrompt.AddChoice("[red]All[/]");
                                        foreach (KeyValuePair<long, DateTime> p in archived.Archived)
                                        {
                                            selectedArchivedPostsPrompt.AddChoice($"[red]{string.Format("Post ID: {0} Posted At DateTime: {1}", p.Key, p.Value.ToString("dd/MM/yyy HH:mm:ss"))}[/]");
                                        }
                                        var archivedPostSelection = AnsiConsole.Prompt(selectedArchivedPostsPrompt);
                                        List<string> videos_to_download = new List<string>();
                                        if (archivedPostSelection.Contains("[red]None[/]"))
                                        {
                                            AnsiConsole.Markup("[red]You selected to download 0 archived videos[/]\n");
                                        }
                                        else if (archivedPostSelection.Contains("[red]All[/]"))
                                        {
                                            AnsiConsole.Markup("[red]You selected to download all archived videos[/]\n");
                                            foreach (KeyValuePair<long, DateTime> p in archived.Archived)
                                            {
                                                videos_to_download.AddRange(archived.Video_URLS[p.Key]);
                                            }
                                        }
                                        else
                                        {
                                            AnsiConsole.Markup($"[red]You selected to download {archivedPostSelection.Count} archived videos[/]\n");
                                            foreach (string video in archivedPostSelection)
                                            {
                                                string pattern = @"Post ID: (\d+)";
                                                Match match = Regex.Match(video, pattern);
                                                if (match.Success)
                                                {
                                                    long postId = Convert.ToInt64(match.Groups[1].Value);
                                                    videos_to_download.AddRange(archived.Video_URLS[postId]);
                                                }
                                            }
                                        }
                                        if (videos_to_download.Count > 0)
                                        {
                                            await AnsiConsole.Progress().Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new DownloadedColumn(), new RemainingTimeColumn()).StartAsync(async ctx =>
                                            {
                                                var task = ctx.AddTask($"[red]Downloading {videos_to_download.Count} Video(s)[/]", autoStart: false);

                                                task.MaxValue = await downloadHelper.CalculateTotalFileSize(videos_to_download, auth);
                                                task.StartTask();
                                                foreach (string video in videos_to_download)
                                                {
                                                    bool isNew;
                                                    if (video.Contains("cdn3.onlyfans.com/dash/files"))
                                                    {
                                                        string[] messageUrlParsed = video.Split(',');
                                                        string mpdURL = messageUrlParsed[0];
                                                        string policy = messageUrlParsed[1];
                                                        string signature = messageUrlParsed[2];
                                                        string kvp = messageUrlParsed[3];
                                                        string mediaId = messageUrlParsed[4];
                                                        string postId = messageUrlParsed[5];
                                                        string? licenseURL = null;
                                                        string? pssh = await apiHelper.GetDRMMPDPSSH(mpdURL, policy, signature, kvp, auth);
                                                        if (pssh != null)
                                                        {
                                                            DateTime lastModified = await apiHelper.GetDRMMPDLastModified(mpdURL, policy, signature, kvp, auth);
                                                            Dictionary<string, string> drmHeaders = await apiHelper.Headers($"/api2/v2/users/media/{mediaId}/drm/post/{postId}", "?type=widevine", auth);
                                                            string decryptionKey = await apiHelper.GetDecryptionKeyNew(drmHeaders, $"https://onlyfans.com/api2/v2/users/media/{mediaId}/drm/post/{postId}?type=widevine", pssh);
                                                            isNew = await downloadHelper.DownloadArchivedDRMVideo(auth.YTDLP_PATH, auth.MP4DECRYPT_PATH, auth.FFMPEG_PATH, auth.USER_AGENT, policy, signature, kvp, auth.COOKIE, mpdURL, decryptionKey, path, lastModified, Convert.ToInt64(mediaId), task);
                                                            if (isNew)
                                                            {
                                                                newArchivedCount++;
                                                            }
                                                            else
                                                            {
                                                                oldArchivedCount++;
                                                            }
                                                        }
                                                    }
                                                }
                                            });
                                            AnsiConsole.Markup($"[red]Archived Post DRM Videos Skipped/Already Downloaded: {oldArchivedCount} New Archived Post DRM Videos Downloaded: {newArchivedCount}[/]\n");
                                        }
                                    }
                                    else
                                    {
                                        AnsiConsole.Markup($"[red]Found 0 Archived Posts with DRM videos\n[/]");
                                    }
                                }
                                if (auth.DownloadMessages)
                                {
                                    AnsiConsole.Markup($"[red]Getting Messages\n[/]");
                                    MessagesCollection messages = await apiHelper.GetMessageVideos($"/chats/{user.Value}/media/videos", path, auth);
                                    if (messages != null && messages.Video_URLS.Count > 0 && messages.Messages.Count > 0)
                                    {
                                        AnsiConsole.Markup($"[red]Found {messages.Video_URLS.Count} Messages with DRM Video(s)\n[/]");
                                        int oldMessageCount = 0;
                                        int newMessageCount = 0;
                                        var selectedMessagesPrompt = new MultiSelectionPrompt<string>();
                                        selectedMessagesPrompt.PageSize(10);
                                        selectedMessagesPrompt.AddChoice("[red]None[/]");
                                        selectedMessagesPrompt.AddChoice("[red]All[/]");
                                        foreach (KeyValuePair<long, DateTime> p in messages.Messages)
                                        {
                                            selectedMessagesPrompt.AddChoice($"[red]{string.Format("Message ID: {0} Sent DateTime: {1}", p.Key, p.Value.ToString("dd/MM/yyy HH:mm:ss"))}[/]");
                                        }
                                        var messagesSelection = AnsiConsole.Prompt(selectedMessagesPrompt);
                                        List<string> videos_to_download = new List<string>();
                                        if (messagesSelection.Contains("[red]None[/]"))
                                        {
                                            AnsiConsole.Markup("[red]You selected to download 0 message videos[/]\n");
                                        }
                                        else if (messagesSelection.Contains("[red]All[/]"))
                                        {
                                            AnsiConsole.Markup("[red]You selected to download all message videos[/]\n");
                                            foreach (KeyValuePair<long, DateTime> p in messages.Messages)
                                            {
                                                videos_to_download.AddRange(messages.Video_URLS[p.Key]);
                                            }
                                        }
                                        else
                                        {
                                            AnsiConsole.Markup($"[red]You selected to download {messagesSelection.Count} paid message videos[/]\n");
                                            foreach (string video in messagesSelection)
                                            {
                                                string pattern = @"Message ID: (\d+)";
                                                Match match = Regex.Match(video, pattern);
                                                if (match.Success)
                                                {
                                                    long messageId = Convert.ToInt64(match.Groups[1].Value);
                                                    videos_to_download.AddRange(messages.Video_URLS[messageId]);
                                                }
                                            }
                                        }
                                        if (videos_to_download.Count > 0)
                                        {
                                            await AnsiConsole.Progress().Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new DownloadedColumn(), new RemainingTimeColumn()).StartAsync(async ctx =>
                                            {
                                                var task = ctx.AddTask($"[red]Downloading {videos_to_download.Count} Video(s)[/]", autoStart: false);

                                                task.MaxValue = await downloadHelper.CalculateTotalFileSize(videos_to_download, auth);
                                                task.StartTask();
                                                foreach (string video in videos_to_download)
                                                {
                                                    bool isNew;
                                                    if (video.Contains("cdn3.onlyfans.com/dash/files"))
                                                    {
                                                        string[] messageUrlParsed = video.Split(',');
                                                        string mpdURL = messageUrlParsed[0];
                                                        string policy = messageUrlParsed[1];
                                                        string signature = messageUrlParsed[2];
                                                        string kvp = messageUrlParsed[3];
                                                        string mediaId = messageUrlParsed[4];
                                                        string postId = messageUrlParsed[5];
                                                        string? licenseURL = null;
                                                        string? pssh = await apiHelper.GetDRMMPDPSSH(mpdURL, policy, signature, kvp, auth);
                                                        if (pssh != null)
                                                        {
                                                            DateTime lastModified = await apiHelper.GetDRMMPDLastModified(mpdURL, policy, signature, kvp, auth);
                                                            Dictionary<string, string> drmHeaders = await apiHelper.Headers($"/api2/v2/users/media/{mediaId}/drm/message/{postId}", "?type=widevine", auth);
                                                            string decryptionKey = await apiHelper.GetDecryptionKeyNew(drmHeaders, $"https://onlyfans.com/api2/v2/users/media/{mediaId}/drm/message/{postId}?type=widevine", pssh);
                                                            isNew = await downloadHelper.DownloadMessageDRMVideo(auth.YTDLP_PATH, auth.MP4DECRYPT_PATH, auth.FFMPEG_PATH, auth.USER_AGENT, policy, signature, kvp, auth.COOKIE, mpdURL, decryptionKey, path, lastModified, Convert.ToInt64(mediaId), task);
                                                            if (isNew)
                                                            {
                                                                newMessageCount++;
                                                            }
                                                            else
                                                            {
                                                                oldMessageCount++;
                                                            }
                                                        }
                                                    }
                                                }
                                            });
                                            AnsiConsole.Markup($"[red]Message DRM Videos Skipped/Already Downloaded: {oldMessageCount} New Message DRM Videos Downloaded: {newMessageCount}[/]\n");
                                        }
                                    }
                                    else
                                    {
                                        AnsiConsole.Markup($"[red]Found 0 Messages with DRM videos\n[/]");
                                    }
                                }
                                if (auth.DownloadPaidMessages)
                                {
                                    AnsiConsole.Markup($"[red]Getting Paid Messages\n[/]");
                                    PaidMessagesCollection paidMessages = await apiHelper.GetPaidMessageVideos("/posts/paid", user.Key, path, auth);
                                    if (paidMessages != null && paidMessages.Video_URLS.Count > 0 && paidMessages.PaidMessages.Count > 0)
                                    {
                                        AnsiConsole.Markup($"[red]Found {paidMessages.Video_URLS.Count} Paid Messages with DRM Video(s)\n[/]");
                                        int oldPaidMessageCount = 0;
                                        int newPaidMessageCount = 0;
                                        var selectedPaidMessagesPrompt = new MultiSelectionPrompt<string>();
                                        selectedPaidMessagesPrompt.PageSize(10);
                                        selectedPaidMessagesPrompt.AddChoice("[red]None[/]");
                                        selectedPaidMessagesPrompt.AddChoice("[red]All[/]");
                                        foreach (KeyValuePair<long, DateTime> p in paidMessages.PaidMessages)
                                        {
                                            selectedPaidMessagesPrompt.AddChoice($"[red]{string.Format("Message ID: {0} Sent DateTime: {1}", p.Key, p.Value.ToString("dd/MM/yyy HH:mm:ss"))}[/]");
                                        }
                                        var paidMessagesSelection = AnsiConsole.Prompt(selectedPaidMessagesPrompt);
                                        List<string> videos_to_download = new List<string>();
                                        if (paidMessagesSelection.Contains("[red]None[/]"))
                                        {
                                            AnsiConsole.Markup("[red]You selected to download 0 paid message videos[/]\n");
                                        }
                                        else if (paidMessagesSelection.Contains("[red]All[/]"))
                                        {
                                            AnsiConsole.Markup("[red]You selected to download all paid message videos[/]\n");
                                            foreach (KeyValuePair<long, DateTime> p in paidMessages.PaidMessages)
                                            {
                                                videos_to_download.AddRange(paidMessages.Video_URLS[p.Key]);
                                            }
                                        }
                                        else
                                        {
                                            AnsiConsole.Markup($"[red]You selected to download {paidMessagesSelection.Count} paid message videos[/]\n");
                                            foreach (string video in paidMessagesSelection)
                                            {
                                                string pattern = @"Message ID: (\d+)";
                                                Match match = Regex.Match(video, pattern);
                                                if (match.Success)
                                                {
                                                    long messageId = Convert.ToInt64(match.Groups[1].Value);
                                                    videos_to_download.AddRange(paidMessages.Video_URLS[messageId]);
                                                }
                                            }
                                        }
                                        if (videos_to_download.Count > 0)
                                        {
                                            await AnsiConsole.Progress().Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new DownloadedColumn(), new RemainingTimeColumn()).StartAsync(async ctx =>
                                            {
                                                var task = ctx.AddTask($"[red]Downloading {videos_to_download.Count} Video(s)[/]", autoStart: false);

                                                task.MaxValue = await downloadHelper.CalculateTotalFileSize(videos_to_download, auth);
                                                task.StartTask();
                                                foreach (string video in videos_to_download)
                                                {
                                                    bool isNew;
                                                    if (video.Contains("cdn3.onlyfans.com/dash/files"))
                                                    {
                                                        string[] messageUrlParsed = video.Split(',');
                                                        string mpdURL = messageUrlParsed[0];
                                                        string policy = messageUrlParsed[1];
                                                        string signature = messageUrlParsed[2];
                                                        string kvp = messageUrlParsed[3];
                                                        string mediaId = messageUrlParsed[4];
                                                        string postId = messageUrlParsed[5];
                                                        string? licenseURL = null;
                                                        string? pssh = await apiHelper.GetDRMMPDPSSH(mpdURL, policy, signature, kvp, auth);
                                                        if (pssh != null)
                                                        {
                                                            DateTime lastModified = await apiHelper.GetDRMMPDLastModified(mpdURL, policy, signature, kvp, auth);
                                                            Dictionary<string, string> drmHeaders = await apiHelper.Headers($"/api2/v2/users/media/{mediaId}/drm/message/{postId}", "?type=widevine", auth);
                                                            string decryptionKey = await apiHelper.GetDecryptionKeyNew(drmHeaders, $"https://onlyfans.com/api2/v2/users/media/{mediaId}/drm/message/{postId}?type=widevine", pssh);
                                                            isNew = await downloadHelper.DownloadPaidMessageDRMVideo(auth.YTDLP_PATH, auth.MP4DECRYPT_PATH, auth.FFMPEG_PATH, auth.USER_AGENT, policy, signature, kvp, auth.COOKIE, mpdURL, decryptionKey, path, lastModified, Convert.ToInt64(mediaId), task);
                                                            if (isNew)
                                                            {
                                                                newPaidMessageCount++;
                                                            }
                                                            else
                                                            {
                                                                oldPaidMessageCount++;
                                                            }
                                                        }
                                                    }
                                                }
                                            });
                                            AnsiConsole.Markup($"[red]Paid Message DRM Videos Skipped/Already Downloaded: {oldPaidMessageCount} New Paid Message DRM Videos Downloaded: {newPaidMessageCount}[/]\n");
                                        }
                                    }
                                    else
                                    {
                                        AnsiConsole.Markup($"[red]Found 0 Paid Messages with DRM videos\n[/]");
                                    }
                                }
                            }
                            DateTime endTime = DateTime.Now;
                            TimeSpan totalTime = endTime - startTime;
                            AnsiConsole.Markup($"\n[green]Scrape Completed in {totalTime.TotalMinutes.ToString("0.00")} minutes\n\n[/]");
                        }
                        else if (hasSelectedUsersKVP.Key && hasSelectedUsersKVP.Value != null ? hasSelectedUsersKVP.Value.ContainsKey("AuthChanged") : false)
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    } while (true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught: {0}\n\nStackTrace: {1}", ex.Message, ex.StackTrace);

                if (ex.InnerException != null)
                {
                    Console.WriteLine("\nInner Exception:");
                    Console.WriteLine("Exception caught: {0}\n\nStackTrace: {1}", ex.InnerException.Message, ex.InnerException.StackTrace);
                }
            }
        }


        public static async Task<KeyValuePair<bool, Dictionary<string, int>>> HandleUserSelection(Dictionary<string, int> selectedUsers, Dictionary<string, int> users, Dictionary<string, int> lists)
        {
            bool hasSelectedUsers = false;

            while (!hasSelectedUsers)
            {
                var mainMenuOptions = GetMainMenuOptions(users, lists);

                var mainMenuSelection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[red]Select Accounts to Scrape | Select All = All Accounts | List = Download content from users on List | Custom = Specific Account(s)[/]")
                        .AddChoices(mainMenuOptions)
                );
                switch (mainMenuSelection)
                {
                    case "[red]Select All[/]":
                        selectedUsers = users;
                        hasSelectedUsers = true;
                        break;
                    case "[red]List[/]":
                        while (true)
                        {
                            var listSelectionPrompt = new MultiSelectionPrompt<string>();
                            listSelectionPrompt.Title = "[red]Select List[/]";
                            listSelectionPrompt.PageSize = 10;
                            listSelectionPrompt.AddChoice("[red]Go Back[/]");
                            foreach (string key in lists.Keys.Select(k => $"[red]{k}[/]").ToList())
                            {
                                listSelectionPrompt.AddChoice(key);
                            }
                            var listSelection = AnsiConsole.Prompt(listSelectionPrompt);

                            if (listSelection.Contains("[red]Go Back[/]"))
                            {
                                break; // Go back to the main menu
                            }
                            else
                            {
                                hasSelectedUsers = true;
                                List<string> listUsernames = new List<string>();
                                foreach (var item in listSelection)
                                {
                                    int listId = lists[item.Replace("[red]", "").Replace("[/]", "")];
                                    List<string> usernames = await apiHelper.GetListUsers($"/lists/{listId}/users", auth);
                                    foreach (string user in usernames)
                                    {
                                        listUsernames.Add(user);
                                    }
                                }
                                selectedUsers = users.Where(x => listUsernames.Contains($"{x.Key}")).Distinct().ToDictionary(x => x.Key, x => x.Value);
                                AnsiConsole.Markup(string.Format("[red]Downloading from List(s): {0}[/]", string.Join(", ", listSelection)));
                                break;
                            }
                        }
                        break;
                    case "[red]Custom[/]":
                        while (true)
                        {
                            var selectedNamesPrompt = new MultiSelectionPrompt<string>();
                            selectedNamesPrompt.Title("[red]Select users[/]");
                            selectedNamesPrompt.PageSize(10);
                            selectedNamesPrompt.AddChoice("[red]Go Back[/]");
                            foreach (string key in users.Keys.Select(k => $"[red]{k}[/]").ToList())
                            {
                                selectedNamesPrompt.AddChoice(key);
                            }
                            var userSelection = AnsiConsole.Prompt(selectedNamesPrompt);
                            if (userSelection.Contains("[red]Go Back[/]"))
                            {
                                break; // Go back to the main menu
                            }
                            else
                            {
                                hasSelectedUsers = true;
                                selectedUsers = users.Where(x => userSelection.Contains($"[red]{x.Key}[/]")).ToDictionary(x => x.Key, x => x.Value);
                                break;
                            }
                        }
                        break;
                    case "[red]Edit Auth.json[/]":
                        while (true)
                        {
                            var choices = new List<(string choice, bool isSelected)>();
                            choices.AddRange(new[]
                            {
                                ( "[red]Go Back[/]", false ),
                                ( "[red]DownloadPaidPosts[/]", auth.DownloadPaidPosts ),
                                ( "[red]DownloadPosts[/]", auth.DownloadPosts ),
                                ( "[red]DownloadArchived[/]", auth.DownloadArchived ),
                                ( "[red]DownloadMessages[/]", auth.DownloadMessages ),
                                ( "[red]DownloadPaidMessages[/]", auth.DownloadPaidMessages ),
                                ( "[red]IncludeExpiredSubscriptions[/]", auth.IncludeExpiredSubscriptions )
                            });

                            MultiSelectionPrompt<string> multiSelectionPrompt = new MultiSelectionPrompt<string>()
                                .Title("[red]Edit Auth.json[/]")
                                .PageSize(7);

                            foreach (var choice in choices)
                            {
                                multiSelectionPrompt.AddChoices(choice.choice, (selectionItem) => { if (choice.isSelected) selectionItem.Select(); });
                            }

                            var authOptions = AnsiConsole.Prompt(multiSelectionPrompt);

                            if (authOptions.Contains("[red]Go Back[/]"))
                            {
                                break;
                            }

                            Auth newAuth = new Auth();
                            newAuth.USER_ID = auth.USER_ID;
                            newAuth.USER_AGENT = auth.USER_AGENT;
                            newAuth.X_BC = auth.X_BC;
                            newAuth.COOKIE = auth.COOKIE;
                            newAuth.YTDLP_PATH = auth.YTDLP_PATH;
                            newAuth.FFMPEG_PATH = auth.FFMPEG_PATH;
                            newAuth.MP4DECRYPT_PATH = auth.MP4DECRYPT_PATH;

                            if (authOptions.Contains("[red]DownloadPaidPosts[/]"))
                            {
                                newAuth.DownloadPaidPosts = true;
                            }
                            else
                            {
                                newAuth.DownloadPaidPosts = false;
                            }

                            if (authOptions.Contains("[red]DownloadPosts[/]"))
                            {
                                newAuth.DownloadPosts = true;
                            }
                            else
                            {
                                newAuth.DownloadPosts = false;
                            }

                            if (authOptions.Contains("[red]DownloadArchived[/]"))
                            {
                                newAuth.DownloadArchived = true;
                            }
                            else
                            {
                                newAuth.DownloadArchived = false;
                            }

                            if (authOptions.Contains("[red]DownloadMessages[/]"))
                            {
                                newAuth.DownloadMessages = true;
                            }
                            else
                            {
                                newAuth.DownloadMessages = false;
                            }

                            if (authOptions.Contains("[red]DownloadPaidMessages[/]"))
                            {
                                newAuth.DownloadPaidMessages = true;
                            }
                            else
                            {
                                newAuth.DownloadPaidMessages = false;
                            }

                            if (authOptions.Contains("[red]IncludeExpiredSubscriptions[/]"))
                            {
                                newAuth.IncludeExpiredSubscriptions = true;
                            }
                            else
                            {
                                newAuth.IncludeExpiredSubscriptions = false;
                            }

                            string newAuthString = JsonConvert.SerializeObject(newAuth, Formatting.Indented);
                            File.WriteAllText("auth.json", newAuthString);
                            if (newAuth.IncludeExpiredSubscriptions != auth.IncludeExpiredSubscriptions)
                            {
                                auth = JsonConvert.DeserializeObject<Auth>(File.ReadAllText("auth.json"));
                                return new KeyValuePair<bool, Dictionary<string, int>>(true, new Dictionary<string, int> { { "AuthChanged", 0 } });
                            }
                            auth = JsonConvert.DeserializeObject<Auth>(File.ReadAllText("auth.json"));
                            break;
                        }
                        break;
                    case "[red]Exit[/]":
                        return new KeyValuePair<bool, Dictionary<string, int>>(false, null); // Return false to indicate exit
                }
            }

            return new KeyValuePair<bool, Dictionary<string, int>>(true, selectedUsers); // Return true to indicate selected users
        }

        public static List<string> GetMainMenuOptions(Dictionary<string, int> users, Dictionary<string, int> lists)
        {
            if (lists.Count > 0)
            {
                return new List<string>
                {
                    "[red]Select All[/]",
                    "[red]List[/]",
                    "[red]Custom[/]",
                    "[red]Edit Auth.json[/]",
                    "[red]Exit[/]"
                };
            }
            else
            {
                return new List<string>
                {
                    "[red]Select All[/]",
                    "[red]Custom[/]",
                    "[red]Edit Auth.json[/]",
                    "[red]Exit[/]"
                };
            }
        }
    }
}