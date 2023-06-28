using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OF_DRM_Video_Downloader.Entities
{
    public class PaidPostCollection
    {
        public Dictionary<long, List<string>> Video_URLS = new Dictionary<long, List<string>>();
        public Dictionary<long, DateTime> PaidPosts = new Dictionary<long, DateTime>();
    }
}
