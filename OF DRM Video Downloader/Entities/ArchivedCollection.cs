using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OF_DRM_Video_Downloader.Entities
{
    public class ArchivedCollection
    {
        public Dictionary<long, List<string>> Video_URLS = new Dictionary<long, List<string>>();
        public Dictionary<long, DateTime> Archived = new Dictionary<long, DateTime>();
    }
}
