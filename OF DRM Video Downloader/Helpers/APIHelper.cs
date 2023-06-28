using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using Entities;
using Newtonsoft.Json;
using OF_DRM_Video_Downloader.Entities;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace OF_DRM_Video_Downloader.Helpers
{
    public class APIHelper : IAPIHelper
    {
        private static IDBHelper dBHelper;

        static APIHelper()
        {
            dBHelper = new DBHelper();
        }
        public async Task<Dictionary<string, int>> GetSubscriptions(string endpoint, bool includeExpiredSubscriptions, Auth auth)
        {
            try
            {
                int post_limit = 50;
                int offset = 0;
                bool loop = true;
                Dictionary<string, string> GetParams = new Dictionary<string, string>();

                if (includeExpiredSubscriptions)
                {
                    GetParams = new Dictionary<string, string>
                    {
                        { "limit", post_limit.ToString() },
                        { "order", "publish_date_asc" },
                        { "type", "all" }
                    };
                }
                else
                {
                    GetParams = new Dictionary<string, string>
                    {
                        { "limit", post_limit.ToString() },
                        { "order", "publish_date_asc" },
                        { "type", "active" }
                    };
                }
                Dictionary<string, int> users = new Dictionary<string, int>();
                while (loop)
                {
                    string queryParams = "?";
                    foreach (KeyValuePair<string, string> kvp in GetParams)
                    {
                        if (kvp.Key == GetParams.Keys.Last())
                        {
                            queryParams += $"{kvp.Key}={kvp.Value}";
                        }
                        else
                        {
                            queryParams += $"{kvp.Key}={kvp.Value}&";
                        }
                    }

                    Dictionary<string, string> headers = await Headers("/api2/v2" + endpoint, queryParams, auth);

                    HttpClient client = new HttpClient();

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://onlyfans.com/api2/v2" + endpoint + queryParams);

                    foreach (KeyValuePair<string, string> keyValuePair in headers)
                    {
                        request.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                    using (var response = await client.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();
                        var body = await response.Content.ReadAsStringAsync();
                        List<Subscription> subscriptions = JsonConvert.DeserializeObject<List<Subscription>>(body);
                        if (subscriptions != null)
                        {
                            foreach (Subscription sub in subscriptions)
                            {
                                users.Add(sub.username, sub.id);
                            }
                            if (subscriptions.Count >= 50)
                            {
                                offset = offset + 50;
                                GetParams["offset"] = Convert.ToString(offset);
                            }
                            else
                            {
                                loop = false;
                            }
                        }
                        else
                        {
                            loop = false;
                        }
                    }
                }
                return users.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
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
            return null;
        }
        public async Task<Dictionary<string, int>> GetLists(string endpoint, Auth auth)
        {
            try
            {
                int offset = 0;
                bool loop = true;
                Dictionary<string, string> GetParams = new Dictionary<string, string>
                {
                    { "offset", offset.ToString() },
                    { "skip_users", "all" },
                    { "limit", "50" },
                    { "format", "infinite" }
                };
                Dictionary<string, int> lists = new Dictionary<string, int>();
                while (loop)
                {
                    string queryParams = "?";
                    foreach (KeyValuePair<string, string> kvp in GetParams)
                    {
                        if (kvp.Key == GetParams.Keys.Last())
                        {
                            queryParams += $"{kvp.Key}={kvp.Value}";
                        }
                        else
                        {
                            queryParams += $"{kvp.Key}={kvp.Value}&";
                        }
                    }

                    Dictionary<string, string> headers = await Headers("/api2/v2" + endpoint, queryParams, auth);

                    HttpClient client = new HttpClient();

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://onlyfans.com/api2/v2" + endpoint + queryParams);

                    foreach (KeyValuePair<string, string> keyValuePair in headers)
                    {
                        request.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                    using (var response = await client.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();
                        var body = await response.Content.ReadAsStringAsync();
                        UserList userList = JsonConvert.DeserializeObject<UserList>(body);
                        if (userList != null)
                        {
                            foreach (UserList.List l in userList.list)
                            {
                                if (IsStringOnlyDigits(l.id))
                                {
                                    lists.Add(l.name, Convert.ToInt32(l.id));
                                }
                            }
                            if (userList.hasMore.Value)
                            {
                                offset = offset + 50;
                                GetParams["offset"] = Convert.ToString(offset);
                            }
                            else
                            {
                                loop = false;
                            }
                        }
                        else
                        {
                            loop = false;
                        }
                    }
                }
                return lists;
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
            return null;
        }
        public async Task<List<string>> GetListUsers(string endpoint, Auth auth)
        {
            try
            {
                int offset = 0;
                bool loop = true;
                Dictionary<string, string> GetParams = new Dictionary<string, string>
                {
                    { "offset", offset.ToString() },
                    { "limit", "50" }
                };
                List<string> users = new List<string>();
                while (loop)
                {
                    string queryParams = "?";
                    foreach (KeyValuePair<string, string> kvp in GetParams)
                    {
                        if (kvp.Key == GetParams.Keys.Last())
                        {
                            queryParams += $"{kvp.Key}={kvp.Value}";
                        }
                        else
                        {
                            queryParams += $"{kvp.Key}={kvp.Value}&";
                        }
                    }

                    Dictionary<string, string> headers = await Headers("/api2/v2" + endpoint, queryParams, auth);

                    HttpClient client = new HttpClient();

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://onlyfans.com/api2/v2" + endpoint + queryParams);

                    foreach (KeyValuePair<string, string> keyValuePair in headers)
                    {
                        request.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                    using (var response = await client.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();
                        var body = await response.Content.ReadAsStringAsync();
                        List<UsersList> usersList = JsonConvert.DeserializeObject<List<UsersList>>(body);
                        if (usersList != null && usersList.Count > 0)
                        {
                            foreach (UsersList ul in usersList)
                            {
                                users.Add(ul.username);
                            }
                            if (users.Count >= 50)
                            {
                                offset = offset + 50;
                                GetParams["offset"] = Convert.ToString(offset);
                            }
                            else
                            {
                                loop = false;
                            }
                        }
                        else
                        {
                            loop = false;
                        }
                    }
                }
                return users;
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
            return null;
        }
        public async Task<User> GetUserInfo(string endpoint, Auth auth)
        {
            try
            {
                User user = new User();
                int post_limit = 50;
                Dictionary<string, string> GetParams = new Dictionary<string, string>
                {
                    { "limit", post_limit.ToString() },
                    { "order", "publish_date_asc" }
                };

                string queryParams = "?";
                foreach (KeyValuePair<string, string> kvp in GetParams)
                {
                    if (kvp.Key == GetParams.Keys.Last())
                    {
                        queryParams += $"{kvp.Key}={kvp.Value}";
                    }
                    else
                    {
                        queryParams += $"{kvp.Key}={kvp.Value}&";
                    }
                }

                Dictionary<string, string> headers = await Headers("/api2/v2" + endpoint, queryParams, auth);

                HttpClient client = new HttpClient();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://onlyfans.com/api2/v2" + endpoint + queryParams);

                foreach (KeyValuePair<string, string> keyValuePair in headers)
                {
                    request.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }
                var jsonSerializerSettings = new JsonSerializerSettings();
                jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                using (var response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        return user;
                    }
                    else
                    {
                        response.EnsureSuccessStatusCode();
                        var body = await response.Content.ReadAsStringAsync();
                        user = JsonConvert.DeserializeObject<Entities.User>(body, jsonSerializerSettings);
                    }

                }
                return user;
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
            return null;
        }
        public async Task<PaidPostCollection> GetPaidPostVideos(string endpoint, string username, string folder, Auth auth)
        {
            try
            {
                Purchased paidPosts = new Purchased();
                PaidPostCollection paidPostCollection = new PaidPostCollection();
                Dictionary<string, string> GetParams = new Dictionary<string, string>
                {
                    { "limit", "50" },
                    { "order", "publish_date_desc" },
                    { "format", "infinite" },
                    { "username", username }
                };

                string queryParams = "?";
                foreach (KeyValuePair<string, string> kvp in GetParams)
                {
                    if (kvp.Key == GetParams.Keys.Last())
                    {
                        queryParams += $"{kvp.Key}={kvp.Value}";
                    }
                    else
                    {
                        queryParams += $"{kvp.Key}={kvp.Value}&";
                    }
                }

                Dictionary<string, string> headers = await Headers("/api2/v2" + endpoint, queryParams, auth);

                HttpClient client = new HttpClient();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://onlyfans.com/api2/v2" + endpoint + queryParams);

                foreach (KeyValuePair<string, string> keyValuePair in headers)
                {
                    request.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }

                var jsonSerializerSettings = new JsonSerializerSettings();
                jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    paidPosts = JsonConvert.DeserializeObject<Purchased>(body, jsonSerializerSettings);
                    if (paidPosts != null && paidPosts.hasMore && !paidPosts.list.Any(p => p.postedAt < new DateTime(2023, 4, 1)))
                    {
                        GetParams["offset"] = paidPosts.list.Count.ToString();
                        while (true)
                        {
                            string loopqueryParams = "?";
                            foreach (KeyValuePair<string, string> kvp in GetParams)
                            {
                                if (kvp.Key == GetParams.Keys.Last())
                                {
                                    loopqueryParams += $"{kvp.Key}={kvp.Value}";
                                }
                                else
                                {
                                    loopqueryParams += $"{kvp.Key}={kvp.Value}&";
                                }
                            }
                            Purchased newPaidPosts = new Purchased();
                            Dictionary<string, string> loopheaders = await Headers("/api2/v2" + endpoint, loopqueryParams, auth);
                            HttpClient loopclient = new HttpClient();

                            HttpRequestMessage looprequest = new HttpRequestMessage(HttpMethod.Get, "https://onlyfans.com/api2/v2" + endpoint + loopqueryParams);

                            foreach (KeyValuePair<string, string> keyValuePair in loopheaders)
                            {
                                looprequest.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                            }
                            using (var loopresponse = await loopclient.SendAsync(looprequest))
                            {
                                loopresponse.EnsureSuccessStatusCode();
                                var loopbody = await loopresponse.Content.ReadAsStringAsync();
                                newPaidPosts = JsonConvert.DeserializeObject<Purchased>(loopbody, jsonSerializerSettings);
                            }
                            paidPosts.list.AddRange(newPaidPosts.list);
                            if (!newPaidPosts.hasMore || newPaidPosts.list.Any(p => p.postedAt < new DateTime(2023, 4, 1)))
                            {
                                break;
                            }
                            GetParams["offset"] = Convert.ToString(Convert.ToInt32(GetParams["offset"]) + Convert.ToInt32(GetParams["offset"]));
                        }
                    }

                    if (paidPosts != null && paidPosts.list.Count > 0)
                    {
                        foreach (Purchased.List paidpost in paidPosts.list)
                        {
                            if (paidpost.responseType == "post" && paidpost.media != null && paidpost.media.Count > 0)
                            {
                                List<long> previewids = new List<long>();
                                if (paidpost.previews != null)
                                {
                                    for (int i = 0; i < paidpost.previews.Count; i++)
                                    {
                                        if (!previewids.Contains((long)paidpost.previews[i]))
                                        {
                                            previewids.Add((long)paidpost.previews[i]);
                                        }
                                    }
                                }
                                foreach (Purchased.Medium media in paidpost.media)
                                {
                                    if (media.canView && media.files != null && media.files.drm != null && previewids.Any(cus => cus.Equals(media.id)))
                                    {
                                        await dBHelper.AddPost(folder, paidpost.id, paidpost.text != null ? paidpost.text : string.Empty, paidpost.price != null ? paidpost.price.ToString() : "0", paidpost.price != null && paidpost.isOpened ? true : false, paidpost.isArchived.HasValue ? paidpost.isArchived.Value : false, paidpost.createdAt != null ? paidpost.createdAt.Value : paidpost.postedAt.Value);
                                        if (!paidPostCollection.Video_URLS.ContainsKey(paidpost.id))
                                        {
                                            paidPostCollection.Video_URLS.Add(paidpost.id, new List<string>());
                                        }
                                        paidPostCollection.Video_URLS[paidpost.id].Add($"{media.files.drm.manifest.dash},{media.files.drm.signature.dash.CloudFrontPolicy},{media.files.drm.signature.dash.CloudFrontSignature},{media.files.drm.signature.dash.CloudFrontKeyPairId},{media.id},{paidpost.id}");
                                        if (!paidPostCollection.PaidPosts.ContainsKey(paidpost.id))
                                        {
                                            paidPostCollection.PaidPosts.Add(paidpost.id, paidpost.createdAt != null ? paidpost.createdAt.Value : paidpost.postedAt.Value);
                                        }
                                        await dBHelper.AddMedia(folder, media.id, media.id, media.files.drm.manifest.dash, null, null, null, "Posts", media.type == "photo" ? "Images" : (media.type == "video" || media.type == "gif" ? "Videos" : (media.type == "audio" ? "Audios" : null)), previewids.Contains(media.id) ? true : false, false, null);
                                    }
                                }
                            }
                        }
                    }
                    return paidPostCollection;
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
            return null;
        }
        public async Task<PostCollection> GetPostVideos(string endpoint, string folder, Auth auth)
        {
            try
            {
                Post posts = new Post();
                PostCollection postCollection = new PostCollection();
                Dictionary<string, string> GetParams = new Dictionary<string, string>
                {
                    { "limit", "50" },
                    { "order", "publish_date_desc" },
                    { "format", "infinite" }
                };

                string queryParams = "?";
                foreach (KeyValuePair<string, string> kvp in GetParams)
                {
                    if (kvp.Key == GetParams.Keys.Last())
                    {
                        queryParams += $"{kvp.Key}={kvp.Value}";
                    }
                    else
                    {
                        queryParams += $"{kvp.Key}={kvp.Value}&";
                    }
                }

                Dictionary<string, string> headers = await Headers("/api2/v2" + endpoint, queryParams, auth);

                HttpClient client = new HttpClient();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://onlyfans.com/api2/v2" + endpoint + queryParams);

                foreach (KeyValuePair<string, string> keyValuePair in headers)
                {
                    request.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }

                var jsonSerializerSettings = new JsonSerializerSettings();
                jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    posts = JsonConvert.DeserializeObject<Post>(body, jsonSerializerSettings);
                    if (posts != null && posts.hasMore && !posts.list.Any(p => p.postedAt < new DateTime(2023, 4, 1)))
                    {
                        GetParams["beforePublishTime"] = posts.tailMarker;
                        while (true)
                        {
                            string loopqueryParams = "?";
                            foreach (KeyValuePair<string, string> kvp in GetParams)
                            {
                                if (kvp.Key == GetParams.Keys.Last())
                                {
                                    loopqueryParams += $"{kvp.Key}={kvp.Value}";
                                }
                                else
                                {
                                    loopqueryParams += $"{kvp.Key}={kvp.Value}&";
                                }
                            }
                            Post newposts = new Post();
                            Dictionary<string, string> loopheaders = await Headers("/api2/v2" + endpoint, loopqueryParams, auth);
                            HttpClient loopclient = new HttpClient();

                            HttpRequestMessage looprequest = new HttpRequestMessage(HttpMethod.Get, "https://onlyfans.com/api2/v2" + endpoint + loopqueryParams);

                            foreach (KeyValuePair<string, string> keyValuePair in loopheaders)
                            {
                                looprequest.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                            }
                            using (var loopresponse = await loopclient.SendAsync(looprequest))
                            {
                                loopresponse.EnsureSuccessStatusCode();
                                var loopbody = await loopresponse.Content.ReadAsStringAsync();
                                newposts = JsonConvert.DeserializeObject<Post>(loopbody, jsonSerializerSettings);
                            }
                            posts.list.AddRange(newposts.list);
                            if (!newposts.hasMore || posts.list.Any(p => p.postedAt < new DateTime(2023, 4, 1)))
                            {
                                break;
                            }
                            GetParams["beforePublishTime"] = newposts.tailMarker;
                        }
                    }

                    if (posts != null && posts.list.Count > 0)
                    {
                        foreach (Post.List post in posts.list)
                        {
                            List<long> postPreviewIds = new List<long>();
                            if (post.preview != null && post.preview.Count > 0)
                            {
                                foreach (var id in post.preview)
                                {
                                    if (id?.ToString() != "poll")
                                    {
                                        if (!postPreviewIds.Contains(Convert.ToInt64(id)))
                                        {
                                            postPreviewIds.Add(Convert.ToInt64(id));
                                        }
                                    }
                                }
                            }
                            if (post.canViewMedia && post.media != null && post.media.Count > 0)
                            {
                                foreach (Post.Medium media in post.media)
                                {
                                    if (media.canView && media.files != null && media.files.drm != null)
                                    {
                                        await dBHelper.AddPost(folder, post.id, post.text != null ? post.text : string.Empty, post.price != null ? post.price.ToString() : "0", post.price != null && post.isOpened ? true : false, post.isArchived, post.postedAt);
                                        if (!postCollection.Video_URLS.ContainsKey(post.id))
                                        {
                                            postCollection.Video_URLS.Add(post.id, new List<string>());
                                        }
                                        postCollection.Video_URLS[post.id].Add($"{media.files.drm.manifest.dash},{media.files.drm.signature.dash.CloudFrontPolicy},{media.files.drm.signature.dash.CloudFrontSignature},{media.files.drm.signature.dash.CloudFrontKeyPairId},{media.id},{post.id}");
                                        if (!postCollection.Posts.ContainsKey(post.id))
                                        {
                                            postCollection.Posts.Add(post.id, post.postedAt);
                                        }
                                        await dBHelper.AddMedia(folder, media.id, post.id, media.files.drm.manifest.dash, null, null, null, "Posts", media.type == "photo" ? "Images" : (media.type == "video" || media.type == "gif" ? "Videos" : (media.type == "audio" ? "Audios" : null)), postPreviewIds.Contains((long)media.id) ? true : false, false, null);
                                    }
                                }
                            }
                        }
                    }
                    return postCollection;
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
            return null;
        }
        public async Task<ArchivedCollection> GetArchivedVideos(string endpoint, string folder, Auth auth)
        {
            try
            {
                Archived archived = new Archived();
                ArchivedCollection archivedCollection = new ArchivedCollection();
                Dictionary<string, string> GetParams = new Dictionary<string, string>
                {
                    { "limit", "50" },
                    { "order", "publish_date_desc" },
                    { "format", "infinite" },
                    { "label", "archived" }
                };

                string queryParams = "?";
                foreach (KeyValuePair<string, string> kvp in GetParams)
                {
                    if (kvp.Key == GetParams.Keys.Last())
                    {
                        queryParams += $"{kvp.Key}={kvp.Value}";
                    }
                    else
                    {
                        queryParams += $"{kvp.Key}={kvp.Value}&";
                    }
                }

                Dictionary<string, string> headers = await Headers("/api2/v2" + endpoint, queryParams, auth);

                HttpClient client = new HttpClient();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://onlyfans.com/api2/v2" + endpoint + queryParams);

                foreach (KeyValuePair<string, string> keyValuePair in headers)
                {
                    request.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }

                var jsonSerializerSettings = new JsonSerializerSettings();
                jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    archived = JsonConvert.DeserializeObject<Archived>(body, jsonSerializerSettings);
                    if (archived != null && archived.hasMore && !archived.list.Any(p => p.postedAt < new DateTime(2023, 4, 1)))
                    {
                        GetParams["beforePublishTime"] = archived.tailMarker;
                        while (true)
                        {
                            string loopqueryParams = "?";
                            foreach (KeyValuePair<string, string> kvp in GetParams)
                            {
                                if (kvp.Key == GetParams.Keys.Last())
                                {
                                    loopqueryParams += $"{kvp.Key}={kvp.Value}";
                                }
                                else
                                {
                                    loopqueryParams += $"{kvp.Key}={kvp.Value}&";
                                }
                            }
                            Archived newArchived = new Archived();
                            Dictionary<string, string> loopheaders = await Headers("/api2/v2" + endpoint, loopqueryParams, auth);
                            HttpClient loopclient = new HttpClient();

                            HttpRequestMessage looprequest = new HttpRequestMessage(HttpMethod.Get, "https://onlyfans.com/api2/v2" + endpoint + loopqueryParams);

                            foreach (KeyValuePair<string, string> keyValuePair in loopheaders)
                            {
                                looprequest.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                            }
                            using (var loopresponse = await loopclient.SendAsync(looprequest))
                            {
                                loopresponse.EnsureSuccessStatusCode();
                                var loopbody = await loopresponse.Content.ReadAsStringAsync();
                                newArchived = JsonConvert.DeserializeObject<Archived>(loopbody, jsonSerializerSettings);
                            }
                            archived.list.AddRange(newArchived.list);
                            if (!newArchived.hasMore || archived.list.Any(p => p.postedAt < new DateTime(2023, 4, 1)))
                            {
                                break;
                            }
                            GetParams["beforePublishTime"] = newArchived.tailMarker;
                        }
                    }

                    if (archived != null && archived.list.Count > 0)
                    {
                        foreach (Archived.List archivedPost in archived.list)
                        {
                            List<long> previewids = new List<long>();
                            if (archivedPost.preview != null)
                            {
                                for (int i = 0; i < archivedPost.preview.Count; i++)
                                {
                                    if (archivedPost.preview[i]?.ToString() != "poll")
                                    {
                                        if (!previewids.Contains((long)archivedPost.preview[i]))
                                        {
                                            previewids.Add((long)archivedPost.preview[i]);
                                        }
                                    }
                                }
                            }
                            if (archivedPost.canViewMedia && archivedPost.media != null && archivedPost.media.Count > 0)
                            {
                                foreach (Archived.Medium media in archivedPost.media)
                                {
                                    if (media.canView && media.files != null && media.files.drm != null)
                                    {
                                        await dBHelper.AddPost(folder, archivedPost.id, archivedPost.text != null ? archivedPost.text : string.Empty, archivedPost.price != null ? archivedPost.price.ToString() : "0", archivedPost.price != null && archivedPost.isOpened ? true : false, archivedPost.isArchived, archivedPost.postedAt);
                                        if (!archivedCollection.Video_URLS.ContainsKey(archivedPost.id))
                                        {
                                            archivedCollection.Video_URLS.Add(archivedPost.id, new List<string>());
                                        }
                                        archivedCollection.Video_URLS[archivedPost.id].Add($"{media.files.drm.manifest.dash},{media.files.drm.signature.dash.CloudFrontPolicy},{media.files.drm.signature.dash.CloudFrontSignature},{media.files.drm.signature.dash.CloudFrontKeyPairId},{media.id},{archivedPost.id}");
                                        if (!archivedCollection.Archived.ContainsKey(archivedPost.id))
                                        {
                                            archivedCollection.Archived.Add(archivedPost.id, archivedPost.postedAt);
                                        }
                                        await dBHelper.AddMedia(folder, media.id, archivedPost.id, media.files.drm.manifest.dash, null, null, null, "Posts", media.type == "photo" ? "Images" : (media.type == "video" || media.type == "gif" ? "Videos" : (media.type == "audio" ? "Audios" : null)), previewids.Contains(media.id) ? true : false, false, null);
                                    }
                                }
                            }
                        }
                    }
                    return archivedCollection;
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
            return null;
        }
        public async Task<MessagesCollection> GetMessageVideos(string endpoint, string folder, Auth auth)
        {
            try
            {
                Messages messages = new Messages();
                MessagesCollection messagesCollection = new MessagesCollection();
                Dictionary<string, string> GetParams = new Dictionary<string, string>
                {
                    { "limit", "20" }
                };

                string queryParams = "?";
                foreach (KeyValuePair<string, string> kvp in GetParams)
                {
                    if (kvp.Key == GetParams.Keys.Last())
                    {
                        queryParams += $"{kvp.Key}={kvp.Value}";
                    }
                    else
                    {
                        queryParams += $"{kvp.Key}={kvp.Value}&";
                    }
                }

                Dictionary<string, string> headers = await Headers("/api2/v2" + endpoint, queryParams, auth);

                HttpClient client = new HttpClient();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://onlyfans.com/api2/v2" + endpoint + queryParams);

                foreach (KeyValuePair<string, string> keyValuePair in headers)
                {
                    request.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }

                var jsonSerializerSettings = new JsonSerializerSettings();
                jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    messages = JsonConvert.DeserializeObject<Messages>(body, jsonSerializerSettings);
                    if (messages != null && messages.hasMore && !messages.list.Any(p => p.createdAt < new DateTime(2023, 4, 1)))
                    {
                        GetParams["last_id"] = messages.nextLastId.HasValue ? messages.nextLastId.Value.ToString() : "";
                        while (true)
                        {
                            string loopqueryParams = "?";
                            foreach (KeyValuePair<string, string> kvp in GetParams)
                            {
                                if (kvp.Key == GetParams.Keys.Last())
                                {
                                    loopqueryParams += $"{kvp.Key}={kvp.Value}";
                                }
                                else
                                {
                                    loopqueryParams += $"{kvp.Key}={kvp.Value}&";
                                }
                            }
                            Messages newMessages = new Messages();
                            Dictionary<string, string> loopheaders = await Headers("/api2/v2" + endpoint, loopqueryParams, auth);
                            HttpClient loopclient = new HttpClient();

                            HttpRequestMessage looprequest = new HttpRequestMessage(HttpMethod.Get, "https://onlyfans.com/api2/v2" + endpoint + loopqueryParams);

                            foreach (KeyValuePair<string, string> keyValuePair in loopheaders)
                            {
                                looprequest.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                            }
                            using (var loopresponse = await loopclient.SendAsync(looprequest))
                            {
                                loopresponse.EnsureSuccessStatusCode();
                                var loopbody = await loopresponse.Content.ReadAsStringAsync();
                                newMessages = JsonConvert.DeserializeObject<Messages>(loopbody, jsonSerializerSettings);
                            }
                            messages.list.AddRange(newMessages.list);
                            if (!newMessages.hasMore || messages.list.Any(p => p.createdAt < new DateTime(2023, 4, 1)))
                            {
                                break;
                            }
                            GetParams["last_id"] = newMessages.nextLastId.HasValue ? newMessages.nextLastId.Value.ToString() : "";
                        }
                    }

                    if (messages != null && messages.list.Count > 0)
                    {
                        foreach (Messages.List message in messages.list)
                        {
                            List<long> messagePreviewIds = new List<long>();
                            if (message.previews != null && message.previews.Count > 0)
                            {
                                foreach (var id in message.previews)
                                {
                                    if (!messagePreviewIds.Contains((long)id))
                                    {
                                        messagePreviewIds.Add((long)id);
                                    }
                                }
                            }
                            if (message.media != null && message.media.Count > 0 && message.canPurchaseReason != "opened")
                            {
                                foreach (Messages.Medium media in message.media)
                                {
                                    if (media.canView && media.files != null && media.files.drm != null)
                                    {
                                        await dBHelper.AddMessage(folder, message.id, message.text != null ? message.text : string.Empty, message.price != null ? message.price.ToString() : "0", message.canPurchaseReason == "opened" ? true : message.canPurchaseReason != "opened" ? false : (bool?)null ?? false, false, message.createdAt.HasValue ? message.createdAt.Value : DateTime.Now, message.fromUser != null && message.fromUser.id != null ? message.fromUser.id.Value : int.MinValue);
                                        if (!messagesCollection.Video_URLS.ContainsKey(message.id))
                                        {
                                            messagesCollection.Video_URLS.Add(message.id, new List<string>());
                                        }
                                        messagesCollection.Video_URLS[message.id].Add($"{media.files.drm.manifest.dash},{media.files.drm.signature.dash.CloudFrontPolicy},{media.files.drm.signature.dash.CloudFrontSignature},{media.files.drm.signature.dash.CloudFrontKeyPairId},{media.id},{message.id}");
                                        if (!messagesCollection.Messages.ContainsKey(message.id))
                                        {
                                            messagesCollection.Messages.Add(message.id, message.createdAt.HasValue ? message.createdAt.Value : DateTime.Now);
                                        }
                                        await dBHelper.AddMedia(folder, media.id, message.id, media.files.drm.manifest.dash, null, null, null, "Messages", media.type == "photo" ? "Images" : (media.type == "video" || media.type == "gif" ? "Videos" : (media.type == "audio" ? "Audios" : null)), messagePreviewIds.Contains(media.id) ? true : false, false, null);
                                    }
                                }
                            }
                        }
                    }
                    return messagesCollection;
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
            return null;
        }
        public async Task<PaidMessagesCollection> GetPaidMessageVideos(string endpoint, string folder, Auth auth)
        {
            try
            {
                PaidMessages paidMessages = new PaidMessages();
                PaidMessagesCollection paidMessagesCollection = new PaidMessagesCollection();
                Dictionary<string, string> GetParams = new Dictionary<string, string>
                {
                    { "limit", "20" }
                };

                string queryParams = "?";
                foreach (KeyValuePair<string, string> kvp in GetParams)
                {
                    if (kvp.Key == GetParams.Keys.Last())
                    {
                        queryParams += $"{kvp.Key}={kvp.Value}";
                    }
                    else
                    {
                        queryParams += $"{kvp.Key}={kvp.Value}&";
                    }
                }

                Dictionary<string, string> headers = await Headers("/api2/v2" + endpoint, queryParams, auth);

                HttpClient client = new HttpClient();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://onlyfans.com/api2/v2" + endpoint + queryParams);

                foreach (KeyValuePair<string, string> keyValuePair in headers)
                {
                    request.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }

                var jsonSerializerSettings = new JsonSerializerSettings();
                jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    paidMessages = JsonConvert.DeserializeObject<PaidMessages>(body, jsonSerializerSettings);
                    if (paidMessages != null && paidMessages.hasMore && !paidMessages.list.Any(p => p.createdAt < new DateTime(2023, 4, 1)))
                    {
                        GetParams["last_id"] = paidMessages.nextLastId.HasValue ? paidMessages.nextLastId.Value.ToString() : "";
                        while (true)
                        {
                            string loopqueryParams = "?";
                            foreach (KeyValuePair<string, string> kvp in GetParams)
                            {
                                if (kvp.Key == GetParams.Keys.Last())
                                {
                                    loopqueryParams += $"{kvp.Key}={kvp.Value}";
                                }
                                else
                                {
                                    loopqueryParams += $"{kvp.Key}={kvp.Value}&";
                                }
                            }
                            PaidMessages newPaidMessages = new PaidMessages();
                            Dictionary<string, string> loopheaders = await Headers("/api2/v2" + endpoint, loopqueryParams, auth);
                            HttpClient loopclient = new HttpClient();

                            HttpRequestMessage looprequest = new HttpRequestMessage(HttpMethod.Get, "https://onlyfans.com/api2/v2" + endpoint + loopqueryParams);

                            foreach (KeyValuePair<string, string> keyValuePair in loopheaders)
                            {
                                looprequest.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                            }
                            using (var loopresponse = await loopclient.SendAsync(looprequest))
                            {
                                loopresponse.EnsureSuccessStatusCode();
                                var loopbody = await loopresponse.Content.ReadAsStringAsync();
                                newPaidMessages = JsonConvert.DeserializeObject<PaidMessages>(loopbody, jsonSerializerSettings);
                            }
                            paidMessages.list.AddRange(newPaidMessages.list);
                            if (!newPaidMessages.hasMore || paidMessages.list.Any(p => p.createdAt < new DateTime(2023, 4, 1)))
                            {
                                break;
                            }
                            GetParams["last_id"] = newPaidMessages.nextLastId.HasValue ? newPaidMessages.nextLastId.Value.ToString() : "";
                        }
                    }

                    if (paidMessages != null && paidMessages.list.Count > 0)
                    {
                        foreach (PaidMessages.List message in paidMessages.list)
                        {
                            if (message.media != null && message.media.Count > 0 && message.canPurchaseReason == "opened" && message.responseType == "message")
                            {
                                List<long> previewids = new List<long>();
                                if (message.previews != null)
                                {
                                    for (int i = 0; i < message.previews.Count; i++)
                                    {
                                        if (!previewids.Contains((long)message.previews[i]))
                                        {
                                            previewids.Add((long)message.previews[i]);
                                        }
                                    }
                                }

                                foreach (PaidMessages.Medium media in message.media)
                                {
                                    if (media.canView && media.files != null && media.files.drm != null && !previewids.Any(cus => cus.Equals(media.id)))
                                    {
                                        await dBHelper.AddMessage(folder, message.id, message.text != null ? message.text : string.Empty, message.price != null ? message.price : "0", true, false, message.createdAt, message.fromUser.id);
                                        if (!paidMessagesCollection.Video_URLS.ContainsKey(message.id))
                                        {
                                            paidMessagesCollection.Video_URLS.Add(message.id, new List<string>());
                                        }
                                        paidMessagesCollection.Video_URLS[message.id].Add($"{media.files.drm.manifest.dash},{media.files.drm.signature.dash.CloudFrontPolicy},{media.files.drm.signature.dash.CloudFrontSignature},{media.files.drm.signature.dash.CloudFrontKeyPairId},{media.id},{message.id}");
                                        if (!paidMessagesCollection.PaidMessages.ContainsKey(message.id))
                                        {
                                            paidMessagesCollection.PaidMessages.Add(message.id, message.createdAt);
                                        }
                                        await dBHelper.AddMedia(folder, media.id, message.id, media.files.drm.manifest.dash, null, null, null, "Messages", media.type == "photo" ? "Images" : (media.type == "video" || media.type == "gif" ? "Videos" : (media.type == "audio" ? "Audios" : null)), previewids.Contains(media.id) ? true : false, false, null);
                                    }
                                }
                            }
                        }
                    }
                    return paidMessagesCollection;
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
            return null;
        }
        public async Task<string> GetDRMMPDPSSH(string mpdUrl, string policy, string signature, string kvp, Auth auth)
        {
            try
            {
                string pssh = null;
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, mpdUrl);
                request.Headers.Add("user-agent", auth.USER_AGENT);
                request.Headers.Add("Accept", "*/*");
                request.Headers.Add("Cookie", $"CloudFront-Policy={policy}; CloudFront-Signature={signature}; CloudFront-Key-Pair-Id={kvp}; {auth.COOKIE};");
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    XNamespace ns = "urn:mpeg:dash:schema:mpd:2011";
                    XNamespace cenc = "urn:mpeg:cenc:2013";
                    XDocument xmlDoc = XDocument.Parse(body);
                    var psshElements = xmlDoc.Descendants(cenc + "pssh");
                    pssh = psshElements.ElementAt(1).Value;
                }

                return pssh;
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
            return null;
        }
        public async Task<DateTime> GetDRMMPDLastModified(string mpdUrl, string policy, string signature, string kvp, Auth auth)
        {
            try
            {
                DateTime lastmodified;
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, mpdUrl);
                request.Headers.Add("user-agent", auth.USER_AGENT);
                request.Headers.Add("Accept", "*/*");
                request.Headers.Add("Cookie", $"CloudFront-Policy={policy}; CloudFront-Signature={signature}; CloudFront-Key-Pair-Id={kvp}; {auth.COOKIE};");
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    lastmodified = response.Content.Headers.LastModified?.LocalDateTime ?? DateTime.Now;
                }
                return lastmodified;
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
            return DateTime.Now;
        }
        public async Task<string> GetDecryptionKey(Dictionary<string, string> drmHeaders, string licenceURL, string pssh)
        {
            try
            {
                string dcValue = string.Empty;
                string buildInfo = "";
                string proxy = "";
                bool cache = true;

                StringBuilder sb = new StringBuilder();
                sb.Append("{\n");
                sb.AppendFormat("  \"license\": \"{0}\",\n", licenceURL);
                sb.Append("  \"headers\": \"");
                foreach (KeyValuePair<string, string> header in drmHeaders)
                {
                    if (header.Key == "time" || header.Key == "user-id")
                    {
                        sb.AppendFormat("{0}: '{1}'\\n", header.Key, header.Value);
                    }
                    else
                    {
                        sb.AppendFormat("{0}: {1}\\n", header.Key, header.Value);
                    }
                }
                sb.Remove(sb.Length - 2, 2); // remove the last \\n
                sb.Append("\",\n");
                sb.AppendFormat("  \"pssh\": \"{0}\",\n", pssh);
                sb.AppendFormat("  \"buildInfo\": \"{0}\",\n", buildInfo);
                sb.AppendFormat("  \"proxy\": \"{0}\",\n", proxy);
                sb.AppendFormat("  \"cache\": {0}\n", cache.ToString().ToLower());
                sb.Append("}");
                string json = sb.ToString();
                HttpClient client = new HttpClient();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://cdrm-project.com/wv");
                request.Content = new StringContent(json);
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(body);

                    // Find the <li> element containing the Decryption Key using XPath
                    HtmlNode dcElement = htmlDoc.DocumentNode.SelectSingleNode("//li");

                    // Get the text value of the <li> element
                    dcValue = dcElement.InnerText;
                }
                return dcValue;
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
            return null;
        }
        public async Task<Dictionary<string, string>> Headers(string path, string queryParams, Auth auth)
        {
            DynamicRules root = new DynamicRules();
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://raw.githubusercontent.com/deviint/onlyfans-dynamic-rules/main/dynamicRules.json"),
            };
            using (var vresponse = client.Send(request))
            {
                vresponse.EnsureSuccessStatusCode();
                var body = await vresponse.Content.ReadAsStringAsync();
                root = JsonConvert.DeserializeObject<DynamicRules>(body);
            }

            long timestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            string input = $"{root.static_param}\n{timestamp}\n{path + queryParams}\n{auth.USER_ID}";
            string hashString = string.Empty;
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha1.ComputeHash(inputBytes);
                hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }

            int checksum = 0;
            foreach (int number in root.checksum_indexes)
            {
                List<int> test = new List<int>
            {
                hashString[number]
            };
                checksum = checksum + test.Sum();
            }
            checksum = checksum + root.checksum_constant;
            string sign = $"{root.start}:{hashString}:{checksum.ToString("X").ToLower()}:{root.end}";

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "accept", "application/json, text/plain" },
                { "app-token", root.app_token },
                { "cookie", auth.COOKIE },
                { "sign", sign },
                { "time", timestamp.ToString() },
                { "user-id", auth.USER_ID },
                { "user-agent", auth.USER_AGENT },
                { "x-bc", auth.X_BC }
            };
            return headers;
        }
        public static bool IsStringOnlyDigits(string input)
        {
            foreach (char c in input)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
