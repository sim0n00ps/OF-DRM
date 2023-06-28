using Entities;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OF_DRM_Video_Downloader.Helpers
{
    public class DownloadHelper : IDownloadHelper
    {
        private static IDBHelper dBHelper;

        public DownloadHelper()
        {
            dBHelper = new DBHelper();
        }
        public async Task<bool> DownloadPostDRMVideo(string ytdlppath, string mp4decryptpath, string ffmpegpath, string user_agent, string policy, string signature, string kvp, string sess, string url, string decryptionKey, string folder, DateTime lastModified, long media_id, ProgressTask task)
        {
            try
            {
                Uri uri = new Uri(url);
                string filename = System.IO.Path.GetFileName(uri.LocalPath).Split(".")[0];
                string path = "/Posts/Free/Videos";
                if (!Directory.Exists(folder + path)) // check if the folder already exists
                {
                    Directory.CreateDirectory(folder + path); // create the new folder
                }
                if (!await dBHelper.CheckDownloaded(folder, media_id))
                {
                    if (!File.Exists(folder + path + "/" + filename + "_source.mp4"))
                    {
                        //Use ytdl-p to download the MPD as a M4A and MP4 file
                        ProcessStartInfo ytdlpstartInfo = new ProcessStartInfo();
                        ytdlpstartInfo.FileName = ytdlppath;
                        ytdlpstartInfo.Arguments = $"--allow-u --no-part --restrict-filenames -N 4 --user-agent \"{user_agent}\" --add-header \"Cookie:CloudFront-Policy={policy}; CloudFront-Signature={signature}; CloudFront-Key-Pair-Id={kvp}; {sess}\" --referer \"https://onlyfans.com/\" -o \"{folder + path + "/"}%(title)s.%(ext)s\" --format \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best[ext=m4a]\" \"{url}\"";
                        ytdlpstartInfo.CreateNoWindow = true;

                        Process ytdlpprocess = new Process();
                        ytdlpprocess.StartInfo = ytdlpstartInfo;
                        ytdlpprocess.Start();
                        ytdlpprocess.WaitForExit();

                        //Remove .fx from filenames
                        if (File.Exists(folder + path + "/" + filename + ".f1.mp4"))
                        {
                            File.Move(folder + path + "/" + filename + ".f1.mp4", folder + path + "/" + filename + ".mp4");
                        }
                        else if (File.Exists(folder + path + "/" + filename + ".f2.mp4"))
                        {
                            File.Move(folder + path + "/" + filename + ".f2.mp4", folder + path + "/" + filename + ".mp4");
                        }
                        else if (File.Exists(folder + path + "/" + filename + ".f3.mp4"))
                        {
                            File.Move(folder + path + "/" + filename + ".f3.mp4", folder + path + "/" + filename + ".mp4");
                        }

                        if (File.Exists(folder + path + "/" + filename + ".f3.m4a"))
                        {
                            File.Move(folder + path + "/" + filename + ".f3.m4a", folder + path + "/" + filename + ".m4a");
                        }
                        else if (File.Exists(folder + path + "/" + filename + ".f4.m4a"))
                        {
                            File.Move(folder + path + "/" + filename + ".f4.m4a", folder + path + "/" + filename + ".m4a");
                        }

                        //Use mp4decrypt to decrypt the MP4 and M4A files
                        ProcessStartInfo mp4decryptStartInfoVideo = new ProcessStartInfo();
                        mp4decryptStartInfoVideo.FileName = mp4decryptpath;
                        mp4decryptStartInfoVideo.Arguments = $"--key {decryptionKey} {folder + path + "/" + filename}.mp4 {folder + path + "/" + filename}_vdec.mp4";
                        mp4decryptStartInfoVideo.CreateNoWindow = true;

                        Process mp4decryptVideoProcess = new Process();
                        mp4decryptVideoProcess.StartInfo = mp4decryptStartInfoVideo;
                        mp4decryptVideoProcess.Start();
                        mp4decryptVideoProcess.WaitForExit();

                        ProcessStartInfo mp4decryptStartInfoAudio = new ProcessStartInfo();
                        mp4decryptStartInfoAudio.FileName = mp4decryptpath;
                        mp4decryptStartInfoAudio.Arguments = $"--key {decryptionKey} {folder + path + "/" + filename}.m4a {folder + path + "/" + filename}_adec.mp4";
                        mp4decryptStartInfoAudio.CreateNoWindow = true;

                        Process mp4decryptAudioProcess = new Process();
                        mp4decryptAudioProcess.StartInfo = mp4decryptStartInfoAudio;
                        mp4decryptAudioProcess.Start();
                        mp4decryptAudioProcess.WaitForExit();

                        //Finally use FFMPEG to merge the 2 together
                        ProcessStartInfo ffmpegStartInfo = new ProcessStartInfo();
                        ffmpegStartInfo.FileName = ffmpegpath;
                        ffmpegStartInfo.Arguments = $"-i {folder + path + "/" + filename}_vdec.mp4 -i {folder + path + "/" + filename}_adec.mp4 -c copy {folder + path + "/" + filename}_source.mp4";
                        ffmpegStartInfo.CreateNoWindow = true;

                        Process ffmpegProcess = new Process();
                        ffmpegProcess.StartInfo = ffmpegStartInfo;
                        ffmpegProcess.Start();
                        ffmpegProcess.WaitForExit();
                        File.SetLastWriteTime($"{folder + path + "/" + filename}_source.mp4", lastModified);

                        //Cleanup Files
                        long fileSizeInBytes = new FileInfo(folder + path + "/" + filename + "_source.mp4").Length;
                        task.Increment(fileSizeInBytes);
                        await dBHelper.UpdateMedia(folder, media_id, folder + path, filename + "_source.mp4", fileSizeInBytes, true, lastModified);
                        File.Delete($"{folder + path + "/" + filename}.mp4");
                        File.Delete($"{folder + path + "/" + filename}.m4a");
                        File.Delete($"{folder + path + "/" + filename}_adec.mp4");
                        File.Delete($"{folder + path + "/" + filename}_vdec.mp4");

                        return true;
                    }
                    else
                    {
                        long fileSizeInBytes = new FileInfo(folder + path + "/" + filename + "_source.mp4").Length;
                        task.Increment(fileSizeInBytes);
                        await dBHelper.UpdateMedia(folder, media_id, folder + path, filename + "_source.mp4", fileSizeInBytes, true, lastModified);
                    }
                }
                else
                {
                    long size = await dBHelper.GetFileSize(folder, media_id);
                    task.Increment(size);
                }
                return false;
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
            return false;
        }
        public async Task<bool> DownloadPurchasedPostDRMVideo(string ytdlppath, string mp4decryptpath, string ffmpegpath, string user_agent, string policy, string signature, string kvp, string sess, string url, string decryptionKey, string folder, DateTime lastModified, long media_id, ProgressTask task)
        {
            try
            {
                Uri uri = new Uri(url);
                string filename = System.IO.Path.GetFileName(uri.LocalPath).Split(".")[0];
                string path = "/Posts/Paid/Videos";
                if (!Directory.Exists(folder + path)) // check if the folder already exists
                {
                    Directory.CreateDirectory(folder + path); // create the new folder
                }
                if (!await dBHelper.CheckDownloaded(folder, media_id))
                {
                    if (!File.Exists(folder + path + "/" + filename + "_source.mp4"))
                    {
                        //Use ytdl-p to download the MPD as a M4A and MP4 file
                        ProcessStartInfo ytdlpstartInfo = new ProcessStartInfo();
                        ytdlpstartInfo.FileName = ytdlppath;
                        ytdlpstartInfo.Arguments = $"--allow-u --no-part --restrict-filenames -N 4 --user-agent \"{user_agent}\" --add-header \"Cookie:CloudFront-Policy={policy}; CloudFront-Signature={signature}; CloudFront-Key-Pair-Id={kvp}; {sess}\" --referer \"https://onlyfans.com/\" -o \"{folder + path + "/"}%(title)s.%(ext)s\" --format \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best[ext=m4a]\" \"{url}\"";
                        ytdlpstartInfo.CreateNoWindow = true;

                        Process ytdlpprocess = new Process();
                        ytdlpprocess.StartInfo = ytdlpstartInfo;
                        ytdlpprocess.Start();
                        ytdlpprocess.WaitForExit();

                        //Remove .fx from filenames
                        if (File.Exists(folder + path + "/" + filename + ".f1.mp4"))
                        {
                            File.Move(folder + path + "/" + filename + ".f1.mp4", folder + path + "/" + filename + ".mp4");
                        }
                        else if (File.Exists(folder + path + "/" + filename + ".f2.mp4"))
                        {
                            File.Move(folder + path + "/" + filename + ".f2.mp4", folder + path + "/" + filename + ".mp4");
                        }
                        else if (File.Exists(folder + path + "/" + filename + ".f3.mp4"))
                        {
                            File.Move(folder + path + "/" + filename + ".f3.mp4", folder + path + "/" + filename + ".mp4");
                        }

                        if (File.Exists(folder + path + "/" + filename + ".f3.m4a"))
                        {
                            File.Move(folder + path + "/" + filename + ".f3.m4a", folder + path + "/" + filename + ".m4a");
                        }
                        else if (File.Exists(folder + path + "/" + filename + ".f4.m4a"))
                        {
                            File.Move(folder + path + "/" + filename + ".f4.m4a", folder + path + "/" + filename + ".m4a");
                        }

                        //Use mp4decrypt to decrypt the MP4 and M4A files
                        ProcessStartInfo mp4decryptStartInfoVideo = new ProcessStartInfo();
                        mp4decryptStartInfoVideo.FileName = mp4decryptpath;
                        mp4decryptStartInfoVideo.Arguments = $"--key {decryptionKey} {folder + path + "/" + filename}.mp4 {folder + path + "/" + filename}_vdec.mp4";
                        mp4decryptStartInfoVideo.CreateNoWindow = true;

                        Process mp4decryptVideoProcess = new Process();
                        mp4decryptVideoProcess.StartInfo = mp4decryptStartInfoVideo;
                        mp4decryptVideoProcess.Start();
                        mp4decryptVideoProcess.WaitForExit();

                        ProcessStartInfo mp4decryptStartInfoAudio = new ProcessStartInfo();
                        mp4decryptStartInfoAudio.FileName = mp4decryptpath;
                        mp4decryptStartInfoAudio.Arguments = $"--key {decryptionKey} {folder + path + "/" + filename}.m4a {folder + path + "/" + filename}_adec.mp4";
                        mp4decryptStartInfoAudio.CreateNoWindow = true;

                        Process mp4decryptAudioProcess = new Process();
                        mp4decryptAudioProcess.StartInfo = mp4decryptStartInfoAudio;
                        mp4decryptAudioProcess.Start();
                        mp4decryptAudioProcess.WaitForExit();

                        //Finally use FFMPEG to merge the 2 together
                        ProcessStartInfo ffmpegStartInfo = new ProcessStartInfo();
                        ffmpegStartInfo.FileName = ffmpegpath;
                        ffmpegStartInfo.Arguments = $"-i {folder + path + "/" + filename}_vdec.mp4 -i {folder + path + "/" + filename}_adec.mp4 -c copy {folder + path + "/" + filename}_source.mp4";
                        ffmpegStartInfo.CreateNoWindow = true;

                        Process ffmpegProcess = new Process();
                        ffmpegProcess.StartInfo = ffmpegStartInfo;
                        ffmpegProcess.Start();
                        ffmpegProcess.WaitForExit();
                        File.SetLastWriteTime($"{folder + path + "/" + filename}_source.mp4", lastModified);

                        //Cleanup Files
                        long fileSizeInBytes = new FileInfo(folder + path + "/" + filename + "_source.mp4").Length;
                        task.Increment(fileSizeInBytes);
                        await dBHelper.UpdateMedia(folder, media_id, folder + path, filename + "_source.mp4", fileSizeInBytes, true, lastModified);
                        File.Delete($"{folder + path + "/" + filename}.mp4");
                        File.Delete($"{folder + path + "/" + filename}.m4a");
                        File.Delete($"{folder + path + "/" + filename}_adec.mp4");
                        File.Delete($"{folder + path + "/" + filename}_vdec.mp4");

                        return true;
                    }
                    else
                    {
                        long fileSizeInBytes = new FileInfo(folder + path + "/" + filename + "_source.mp4").Length;
                        task.Increment(fileSizeInBytes);
                        await dBHelper.UpdateMedia(folder, media_id, folder + path, filename + "_source.mp4", fileSizeInBytes, true, lastModified);
                    }
                }
                else
                {
                    long size = await dBHelper.GetFileSize(folder, media_id);
                    task.Increment(size);
                }
                return false;
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
            return false;
        }
        public async Task<bool> DownloadArchivedDRMVideo(string ytdlppath, string mp4decryptpath, string ffmpegpath, string user_agent, string policy, string signature, string kvp, string sess, string url, string decryptionKey, string folder, DateTime lastModified, long media_id, ProgressTask task)
        {
            try
            {
                Uri uri = new Uri(url);
                string filename = System.IO.Path.GetFileName(uri.LocalPath).Split(".")[0];
                string path = "/Archived/Posts/Free/Videos";
                if (!Directory.Exists(folder + path)) // check if the folder already exists
                {
                    Directory.CreateDirectory(folder + path); // create the new folder
                }
                if (!await dBHelper.CheckDownloaded(folder, media_id))
                {
                    if (!File.Exists(folder + path + "/" + filename + "_source.mp4"))
                    {
                        //Use ytdl-p to download the MPD as a M4A and MP4 file
                        ProcessStartInfo ytdlpstartInfo = new ProcessStartInfo();
                        ytdlpstartInfo.FileName = ytdlppath;
                        ytdlpstartInfo.Arguments = $"--allow-u --no-part --restrict-filenames -N 4 --user-agent \"{user_agent}\" --add-header \"Cookie:CloudFront-Policy={policy}; CloudFront-Signature={signature}; CloudFront-Key-Pair-Id={kvp}; {sess}\" --referer \"https://onlyfans.com/\" -o \"{folder + path + "/"}%(title)s.%(ext)s\" --format \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best[ext=m4a]\" \"{url}\"";
                        ytdlpstartInfo.CreateNoWindow = true;

                        Process ytdlpprocess = new Process();
                        ytdlpprocess.StartInfo = ytdlpstartInfo;
                        ytdlpprocess.Start();
                        ytdlpprocess.WaitForExit();

                        //Remove .fx from filenames
                        if (File.Exists(folder + path + "/" + filename + ".f1.mp4"))
                        {
                            File.Move(folder + path + "/" + filename + ".f1.mp4", folder + path + "/" + filename + ".mp4");
                        }
                        else if (File.Exists(folder + path + "/" + filename + ".f2.mp4"))
                        {
                            File.Move(folder + path + "/" + filename + ".f2.mp4", folder + path + "/" + filename + ".mp4");
                        }
                        else if (File.Exists(folder + path + "/" + filename + ".f3.mp4"))
                        {
                            File.Move(folder + path + "/" + filename + ".f3.mp4", folder + path + "/" + filename + ".mp4");
                        }

                        if (File.Exists(folder + path + "/" + filename + ".f3.m4a"))
                        {
                            File.Move(folder + path + "/" + filename + ".f3.m4a", folder + path + "/" + filename + ".m4a");
                        }
                        else if (File.Exists(folder + path + "/" + filename + ".f4.m4a"))
                        {
                            File.Move(folder + path + "/" + filename + ".f4.m4a", folder + path + "/" + filename + ".m4a");
                        }

                        //Use mp4decrypt to decrypt the MP4 and M4A files
                        ProcessStartInfo mp4decryptStartInfoVideo = new ProcessStartInfo();
                        mp4decryptStartInfoVideo.FileName = mp4decryptpath;
                        mp4decryptStartInfoVideo.Arguments = $"--key {decryptionKey} {folder + path + "/" + filename}.mp4 {folder + path + "/" + filename}_vdec.mp4";
                        mp4decryptStartInfoVideo.CreateNoWindow = true;

                        Process mp4decryptVideoProcess = new Process();
                        mp4decryptVideoProcess.StartInfo = mp4decryptStartInfoVideo;
                        mp4decryptVideoProcess.Start();
                        mp4decryptVideoProcess.WaitForExit();

                        ProcessStartInfo mp4decryptStartInfoAudio = new ProcessStartInfo();
                        mp4decryptStartInfoAudio.FileName = mp4decryptpath;
                        mp4decryptStartInfoAudio.Arguments = $"--key {decryptionKey} {folder + path + "/" + filename}.m4a {folder + path + "/" + filename}_adec.mp4";
                        mp4decryptStartInfoAudio.CreateNoWindow = true;

                        Process mp4decryptAudioProcess = new Process();
                        mp4decryptAudioProcess.StartInfo = mp4decryptStartInfoAudio;
                        mp4decryptAudioProcess.Start();
                        mp4decryptAudioProcess.WaitForExit();

                        //Finally use FFMPEG to merge the 2 together
                        ProcessStartInfo ffmpegStartInfo = new ProcessStartInfo();
                        ffmpegStartInfo.FileName = ffmpegpath;
                        ffmpegStartInfo.Arguments = $"-i {folder + path + "/" + filename}_vdec.mp4 -i {folder + path + "/" + filename}_adec.mp4 -c copy {folder + path + "/" + filename}_source.mp4";
                        ffmpegStartInfo.CreateNoWindow = true;

                        Process ffmpegProcess = new Process();
                        ffmpegProcess.StartInfo = ffmpegStartInfo;
                        ffmpegProcess.Start();
                        ffmpegProcess.WaitForExit();
                        File.SetLastWriteTime($"{folder + path + "/" + filename}_source.mp4", lastModified);

                        //Cleanup Files
                        long fileSizeInBytes = new FileInfo(folder + path + "/" + filename + "_source.mp4").Length;
                        task.Increment(fileSizeInBytes);
                        await dBHelper.UpdateMedia(folder, media_id, folder + path, filename + "_source.mp4", fileSizeInBytes, true, lastModified);
                        File.Delete($"{folder + path + "/" + filename}.mp4");
                        File.Delete($"{folder + path + "/" + filename}.m4a");
                        File.Delete($"{folder + path + "/" + filename}_adec.mp4");
                        File.Delete($"{folder + path + "/" + filename}_vdec.mp4");

                        return true;
                    }
                    else
                    {
                        long fileSizeInBytes = new FileInfo(folder + path + "/" + filename + "_source.mp4").Length;
                        task.Increment(fileSizeInBytes);
                        await dBHelper.UpdateMedia(folder, media_id, folder + path, filename + "_source.mp4", fileSizeInBytes, true, lastModified);
                    }
                }
                else
                {
                    long size = await dBHelper.GetFileSize(folder, media_id);
                    task.Increment(size);
                }
                return false;
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
            return false;
        }
        public async Task<bool> DownloadMessageDRMVideo(string ytdlppath, string mp4decryptpath, string ffmpegpath, string user_agent, string policy, string signature, string kvp, string sess, string url, string decryptionKey, string folder, DateTime lastModified, long media_id, ProgressTask task)
        {
            try
            {
                Uri uri = new Uri(url);
                string filename = System.IO.Path.GetFileName(uri.LocalPath).Split(".")[0];
                string path = "/Messages/Free/Videos";
                if (!Directory.Exists(folder + path)) // check if the folder already exists
                {
                    Directory.CreateDirectory(folder + path); // create the new folder
                }
                if (!await dBHelper.CheckDownloaded(folder, media_id))
                {
                    if (!File.Exists(folder + path + "/" + filename + "_source.mp4"))
                    {
                        //Use ytdl-p to download the MPD as a M4A and MP4 file
                        ProcessStartInfo ytdlpstartInfo = new ProcessStartInfo();
                        ytdlpstartInfo.FileName = ytdlppath;
                        ytdlpstartInfo.Arguments = $"--allow-u --no-part --restrict-filenames -N 4 --user-agent \"{user_agent}\" --add-header \"Cookie:CloudFront-Policy={policy}; CloudFront-Signature={signature}; CloudFront-Key-Pair-Id={kvp}; {sess}\" --referer \"https://onlyfans.com/\" -o \"{folder + path + "/"}%(title)s.%(ext)s\" --format \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best[ext=m4a]\" \"{url}\"";
                        ytdlpstartInfo.CreateNoWindow = true;

                        Process ytdlpprocess = new Process();
                        ytdlpprocess.StartInfo = ytdlpstartInfo;
                        ytdlpprocess.Start();
                        ytdlpprocess.WaitForExit();

                        //Remove .fx from filenames
                        if (File.Exists(folder + path + "/" + filename + ".f1.mp4"))
                        {
                            File.Move(folder + path + "/" + filename + ".f1.mp4", folder + path + "/" + filename + ".mp4");
                        }
                        else if (File.Exists(folder + path + "/" + filename + ".f2.mp4"))
                        {
                            File.Move(folder + path + "/" + filename + ".f2.mp4", folder + path + "/" + filename + ".mp4");
                        }
                        else if (File.Exists(folder + path + "/" + filename + ".f3.mp4"))
                        {
                            File.Move(folder + path + "/" + filename + ".f3.mp4", folder + path + "/" + filename + ".mp4");
                        }

                        if (File.Exists(folder + path + "/" + filename + ".f3.m4a"))
                        {
                            File.Move(folder + path + "/" + filename + ".f3.m4a", folder + path + "/" + filename + ".m4a");
                        }
                        else if (File.Exists(folder + path + "/" + filename + ".f4.m4a"))
                        {
                            File.Move(folder + path + "/" + filename + ".f4.m4a", folder + path + "/" + filename + ".m4a");
                        }

                        //Use mp4decrypt to decrypt the MP4 and M4A files
                        ProcessStartInfo mp4decryptStartInfoVideo = new ProcessStartInfo();
                        mp4decryptStartInfoVideo.FileName = mp4decryptpath;
                        mp4decryptStartInfoVideo.Arguments = $"--key {decryptionKey} {folder + path + "/" + filename}.mp4 {folder + path + "/" + filename}_vdec.mp4";
                        mp4decryptStartInfoVideo.CreateNoWindow = true;

                        Process mp4decryptVideoProcess = new Process();
                        mp4decryptVideoProcess.StartInfo = mp4decryptStartInfoVideo;
                        mp4decryptVideoProcess.Start();
                        mp4decryptVideoProcess.WaitForExit();

                        ProcessStartInfo mp4decryptStartInfoAudio = new ProcessStartInfo();
                        mp4decryptStartInfoAudio.FileName = mp4decryptpath;
                        mp4decryptStartInfoAudio.Arguments = $"--key {decryptionKey} {folder + path + "/" + filename}.m4a {folder + path + "/" + filename}_adec.mp4";
                        mp4decryptStartInfoAudio.CreateNoWindow = true;

                        Process mp4decryptAudioProcess = new Process();
                        mp4decryptAudioProcess.StartInfo = mp4decryptStartInfoAudio;
                        mp4decryptAudioProcess.Start();
                        mp4decryptAudioProcess.WaitForExit();

                        //Finally use FFMPEG to merge the 2 together
                        ProcessStartInfo ffmpegStartInfo = new ProcessStartInfo();
                        ffmpegStartInfo.FileName = ffmpegpath;
                        ffmpegStartInfo.Arguments = $"-i {folder + path + "/" + filename}_vdec.mp4 -i {folder + path + "/" + filename}_adec.mp4 -c copy {folder + path + "/" + filename}_source.mp4";
                        ffmpegStartInfo.CreateNoWindow = true;

                        Process ffmpegProcess = new Process();
                        ffmpegProcess.StartInfo = ffmpegStartInfo;
                        ffmpegProcess.Start();
                        ffmpegProcess.WaitForExit();
                        File.SetLastWriteTime($"{folder + path + "/" + filename}_source.mp4", lastModified);

                        //Cleanup Files
                        long fileSizeInBytes = new FileInfo(folder + path + "/" + filename + "_source.mp4").Length;
                        task.Increment(fileSizeInBytes);
                        await dBHelper.UpdateMedia(folder, media_id, folder + path, filename + "_source.mp4", fileSizeInBytes, true, lastModified);
                        File.Delete($"{folder + path + "/" + filename}.mp4");
                        File.Delete($"{folder + path + "/" + filename}.m4a");
                        File.Delete($"{folder + path + "/" + filename}_adec.mp4");
                        File.Delete($"{folder + path + "/" + filename}_vdec.mp4");

                        return true;
                    }
                    else
                    {
                        long fileSizeInBytes = new FileInfo(folder + path + "/" + filename + "_source.mp4").Length;
                        task.Increment(fileSizeInBytes);
                        await dBHelper.UpdateMedia(folder, media_id, folder + path, filename + "_source.mp4", fileSizeInBytes, true, lastModified);
                    }
                }
                else
                {
                    long size = await dBHelper.GetFileSize(folder, media_id);
                    task.Increment(size);
                }
                return false;
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
            return false;
        }
        public async Task<bool> DownloadPaidMessageDRMVideo(string ytdlppath, string mp4decryptpath, string ffmpegpath, string user_agent, string policy, string signature, string kvp, string sess, string url, string decryptionKey, string folder, DateTime lastModified, long media_id, ProgressTask task)
        {
            try
            {
                Uri uri = new Uri(url);
                string filename = System.IO.Path.GetFileName(uri.LocalPath).Split(".")[0];
                string path = "/Messages/Paid/Videos";
                if (!Directory.Exists(folder + path)) // check if the folder already exists
                {
                    Directory.CreateDirectory(folder + path); // create the new folder
                }
                if (!await dBHelper.CheckDownloaded(folder, media_id))
                {
                    if (!File.Exists(folder + path + "/" + filename + "_source.mp4"))
                    {
                        //Use ytdl-p to download the MPD as a M4A and MP4 file
                        ProcessStartInfo ytdlpstartInfo = new ProcessStartInfo();
                        ytdlpstartInfo.FileName = ytdlppath;
                        ytdlpstartInfo.Arguments = $"--allow-u --no-part --restrict-filenames -N 4 --user-agent \"{user_agent}\" --add-header \"Cookie:CloudFront-Policy={policy}; CloudFront-Signature={signature}; CloudFront-Key-Pair-Id={kvp}; {sess}\" --referer \"https://onlyfans.com/\" -o \"{folder + path + "/"}%(title)s.%(ext)s\" --format \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best[ext=m4a]\" \"{url}\"";
                        ytdlpstartInfo.CreateNoWindow = true;

                        Process ytdlpprocess = new Process();
                        ytdlpprocess.StartInfo = ytdlpstartInfo;
                        ytdlpprocess.Start();
                        ytdlpprocess.WaitForExit();

                        //Remove .fx from filenames
                        if (File.Exists(folder + path + "/" + filename + ".f1.mp4"))
                        {
                            File.Move(folder + path + "/" + filename + ".f1.mp4", folder + path + "/" + filename + ".mp4");
                        }
                        else if (File.Exists(folder + path + "/" + filename + ".f2.mp4"))
                        {
                            File.Move(folder + path + "/" + filename + ".f2.mp4", folder + path + "/" + filename + ".mp4");
                        }
                        else if (File.Exists(folder + path + "/" + filename + ".f3.mp4"))
                        {
                            File.Move(folder + path + "/" + filename + ".f3.mp4", folder + path + "/" + filename + ".mp4");
                        }

                        if (File.Exists(folder + path + "/" + filename + ".f3.m4a"))
                        {
                            File.Move(folder + path + "/" + filename + ".f3.m4a", folder + path + "/" + filename + ".m4a");
                        }
                        else if (File.Exists(folder + path + "/" + filename + ".f4.m4a"))
                        {
                            File.Move(folder + path + "/" + filename + ".f4.m4a", folder + path + "/" + filename + ".m4a");
                        }

                        //Use mp4decrypt to decrypt the MP4 and M4A files
                        ProcessStartInfo mp4decryptStartInfoVideo = new ProcessStartInfo();
                        mp4decryptStartInfoVideo.FileName = mp4decryptpath;
                        mp4decryptStartInfoVideo.Arguments = $"--key {decryptionKey} {folder + path + "/" + filename}.mp4 {folder + path + "/" + filename}_vdec.mp4";
                        mp4decryptStartInfoVideo.CreateNoWindow = true;

                        Process mp4decryptVideoProcess = new Process();
                        mp4decryptVideoProcess.StartInfo = mp4decryptStartInfoVideo;
                        mp4decryptVideoProcess.Start();
                        mp4decryptVideoProcess.WaitForExit();

                        ProcessStartInfo mp4decryptStartInfoAudio = new ProcessStartInfo();
                        mp4decryptStartInfoAudio.FileName = mp4decryptpath;
                        mp4decryptStartInfoAudio.Arguments = $"--key {decryptionKey} {folder + path + "/" + filename}.m4a {folder + path + "/" + filename}_adec.mp4";
                        mp4decryptStartInfoAudio.CreateNoWindow = true;

                        Process mp4decryptAudioProcess = new Process();
                        mp4decryptAudioProcess.StartInfo = mp4decryptStartInfoAudio;
                        mp4decryptAudioProcess.Start();
                        mp4decryptAudioProcess.WaitForExit();

                        //Finally use FFMPEG to merge the 2 together
                        ProcessStartInfo ffmpegStartInfo = new ProcessStartInfo();
                        ffmpegStartInfo.FileName = ffmpegpath;
                        ffmpegStartInfo.Arguments = $"-i {folder + path + "/" + filename}_vdec.mp4 -i {folder + path + "/" + filename}_adec.mp4 -c copy {folder + path + "/" + filename}_source.mp4";
                        ffmpegStartInfo.CreateNoWindow = true;

                        Process ffmpegProcess = new Process();
                        ffmpegProcess.StartInfo = ffmpegStartInfo;
                        ffmpegProcess.Start();
                        ffmpegProcess.WaitForExit();
                        File.SetLastWriteTime($"{folder + path + "/" + filename}_source.mp4", lastModified);

                        //Cleanup Files
                        long fileSizeInBytes = new FileInfo(folder + path + "/" + filename + "_source.mp4").Length;
                        task.Increment(fileSizeInBytes);
                        await dBHelper.UpdateMedia(folder, media_id, folder + path, filename + "_source.mp4", fileSizeInBytes, true, lastModified);
                        File.Delete($"{folder + path + "/" + filename}.mp4");
                        File.Delete($"{folder + path + "/" + filename}.m4a");
                        File.Delete($"{folder + path + "/" + filename}_adec.mp4");
                        File.Delete($"{folder + path + "/" + filename}_vdec.mp4");

                        return true;
                    }
                    else
                    {
                        long fileSizeInBytes = new FileInfo(folder + path + "/" + filename + "_source.mp4").Length;
                        task.Increment(fileSizeInBytes);
                        await dBHelper.UpdateMedia(folder, media_id, folder + path, filename + "_source.mp4", fileSizeInBytes, true, lastModified);
                    }
                }
                else
                {
                    long size = await dBHelper.GetFileSize(folder, media_id);
                    task.Increment(size);
                }
                return false;
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
            return false;
        }
        public async Task<long> CalculateTotalFileSize(List<string> urls, Auth auth)
        {
            long totalFileSize = 0;
            if (urls.Count > 250)
            {
                int batchSize = 250;

                var tasks = new List<Task<long>>();

                for (int i = 0; i < urls.Count; i += batchSize)
                {
                    var batchUrls = urls.Skip(i).Take(batchSize).ToList();

                    var batchTasks = batchUrls.Select(url => GetFileSizeAsync(url, auth));
                    tasks.AddRange(batchTasks);

                    await Task.WhenAll(batchTasks);

                    await Task.Delay(5000);
                }

                long[] fileSizes = await Task.WhenAll(tasks);
                foreach (long fileSize in fileSizes)
                {
                    totalFileSize += fileSize;
                }
            }
            else
            {
                var tasks = new List<Task<long>>();

                foreach (string url in urls)
                {
                    tasks.Add(GetFileSizeAsync(url, auth));
                }

                long[] fileSizes = await Task.WhenAll(tasks);
                foreach (long fileSize in fileSizes)
                {
                    totalFileSize += fileSize;
                }
            }

            return totalFileSize;
        }

        private async Task<long> GetFileSizeAsync(string url, Auth auth)
        {
            long fileSize = 0;

            try
            {
                Uri uri = new Uri(url);
                if (uri.Host == "cdn3.onlyfans.com" && uri.LocalPath.Contains("/dash/files"))
                {
                    string[] messageUrlParsed = url.Split(',');
                    string mpdURL = messageUrlParsed[0];
                    string policy = messageUrlParsed[1];
                    string signature = messageUrlParsed[2];
                    string kvp = messageUrlParsed[3];

                    mpdURL = mpdURL.Replace(".mpd", "_source.mp4");

                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("Cookie", $"CloudFront-Policy={policy}; CloudFront-Signature={signature}; CloudFront-Key-Pair-Id={kvp}; {auth.COOKIE}");
                        client.DefaultRequestHeaders.Add("User-Agent", auth.USER_AGENT);

                        using (HttpResponseMessage response = await client.GetAsync(mpdURL, HttpCompletionOption.ResponseHeadersRead))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                fileSize = response.Content.Headers.ContentLength ?? 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting file size for URL '{url}': {ex.Message}");
            }

            return fileSize;
        }
    }
}
