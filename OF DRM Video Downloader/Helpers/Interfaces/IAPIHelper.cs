using Entities;
using OF_DRM_Video_Downloader.Entities;

namespace OF_DRM_Video_Downloader.Helpers
{
    public interface IAPIHelper
    {
        Task<Dictionary<string, int>> GetSubscriptions(string endpoint, bool includeExpiredSubscriptions, Auth auth);
        Task<Dictionary<string, int>> GetLists(string endpoint, Auth auth);
        Task<List<string>> GetListUsers(string endpoint, Auth auth);
        Task<User> GetUserInfo(string endpoint, Auth auth);
        Task<PaidPostCollection> GetPaidPostVideos(string endpoint, string username, string folder, Auth auth);
        Task<PostCollection> GetPostVideos(string endpoint, string folder, Auth auth);
        Task<ArchivedCollection> GetArchivedVideos(string endpoint, string folder, Auth auth);
        Task<MessagesCollection> GetMessageVideos(string endpoint, string folder, Auth auth);
        Task<PaidMessagesCollection> GetPaidMessageVideos(string endpoint, string folder, Auth auth);
        Task<string> GetDRMMPDPSSH(string mpdUrl, string policy, string signature, string kvp, Auth auth);
        Task<DateTime> GetDRMMPDLastModified(string mpdUrl, string policy, string signature, string kvp, Auth auth);
        Task<string> GetDecryptionKey(Dictionary<string, string> drmHeaders, string licenceURL, string pssh);
        Task<Dictionary<string, string>> Headers(string path, string queryParams, Auth auth);
    }
}