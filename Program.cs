using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KitsuLibraryBackup
{
    class Program
    {
        static ProgramConfig config;
        const string CONFIG_PATH = "./config.json";
        const string OUT_FILE_NAME = "kitsu_anime_library.csv";
        static string[] CSV_HEADERS = { "Title", "Status", "Rating", "Progress", "Total", "Started", "LastUpdated" };

        static void Main(string[] args)
        {
            string file_contents = File.ReadAllText( CONFIG_PATH );
            config = JsonConvert.DeserializeObject<ProgramConfig>( file_contents );

            Console.WriteLine( "Downloading Kitsu library . . ." );
            Task<List<LibraryEntry>> task = KitsuApi.GetUserLibrary( config.UserId );
            List<LibraryEntry> entries = task.Result;

            if ( entries.Count == 0 ) {
                Console.WriteLine( "No library entries to back up." );
                return;
            }

            try {
                WriteCsvFile( entries );
            } catch ( Exception e ) {
                Console.WriteLine( e.Message );
                return;
            }

            string entry_text = entries.Count > 1 ? "entries" : "entry";
            Console.WriteLine( "Backed up " + entries.Count + " Kitsu library " + entry_text + "." );
        }

        private static void WriteCsvFile( List<LibraryEntry> entries ) {
            StringBuilder csv = new StringBuilder();
            csv.AppendLine( String.Join( ",", CSV_HEADERS ) );
            foreach ( LibraryEntry entry in entries ) {
                string[] values = {
                    "\"" + entry.Title + "\"",
                    entry.Status,
                    entry.Rating,
                    entry.Progress.ToString(),
                    entry.Total.ToString(),
                    entry.Started.ToString(),
                    entry.LastUpdated.ToString()
                };
                csv.AppendLine( String.Join( ",", values ) );
            }
            File.WriteAllText( config.BackupDirectory + OUT_FILE_NAME, csv.ToString() );
        } 
    }
}
