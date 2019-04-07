using System;

namespace KitsuLibraryBackup
{
    class LibraryEntry
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Rating { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
        public int Total { get; set; }
        public DateTime Started { get; set; }
        public DateTime LastUpdated { get; set; }
        public string AnimeUrl { get; set; }
    }
}