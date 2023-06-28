using Entities;
using Spectre.Console;

namespace OF_DRM_Video_Downloader.Helpers
{
    public interface IDownloadHelper
    {
        Task<bool> DownloadPostDRMVideo(string ytdlppath, string mp4decryptpath, string ffmpegpath, string user_agent, string policy, string signature, string kvp, string sess, string url, string decryptionKey, string folder, DateTime lastModified, long media_id, ProgressTask task);
        Task<bool> DownloadPurchasedPostDRMVideo(string ytdlppath, string mp4decryptpath, string ffmpegpath, string user_agent, string policy, string signature, string kvp, string sess, string url, string decryptionKey, string folder, DateTime lastModified, long media_id, ProgressTask task);
        Task<bool> DownloadArchivedDRMVideo(string ytdlppath, string mp4decryptpath, string ffmpegpath, string user_agent, string policy, string signature, string kvp, string sess, string url, string decryptionKey, string folder, DateTime lastModified, long media_id, ProgressTask task);
        Task<bool> DownloadMessageDRMVideo(string ytdlppath, string mp4decryptpath, string ffmpegpath, string user_agent, string policy, string signature, string kvp, string sess, string url, string decryptionKey, string folder, DateTime lastModified, long media_id, ProgressTask task);
        Task<bool> DownloadPaidMessageDRMVideo(string ytdlppath, string mp4decryptpath, string ffmpegpath, string user_agent, string policy, string signature, string kvp, string sess, string url, string decryptionKey, string folder, DateTime lastModified, long media_id, ProgressTask task);
        Task<long> CalculateTotalFileSize(List<string> urls, Auth auth);
    }
}