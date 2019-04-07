using System;
using System.Web;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.Collections.Specialized;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KitsuLibraryBackup
{
    class KitsuApi {
        private const int LIMIT_AMOUNT = 20;
        private static HttpClient client;
        public static async Task<List<LibraryEntry>> GetUserLibrary( int UserId ) {
            client = new HttpClient();
            string library_url = "https://kitsu.io/api/edge/library-entries?";

            /* Filter Query Parameters */
            NameValueCollection queryString = HttpUtility.ParseQueryString( string.Empty );
            queryString["page[limit]"] = LIMIT_AMOUNT.ToString();
            queryString["filter[kind]"] = "anime";
            queryString["filter[userId]"] = UserId.ToString();

            /* Fetch the library data */
            string content = await client.GetStringAsync( library_url + queryString.ToString() );

            dynamic response = JObject.Parse( content );
            int library_entry_count = response.meta.count;
            int offset = 0;
            int remaining = library_entry_count - LIMIT_AMOUNT;
            List<Task<string>> tasks = new List<Task<string>>();
            do {
                offset += LIMIT_AMOUNT;
                queryString["page[offset]"] = offset.ToString();
                tasks.Add( client.GetStringAsync( library_url + queryString.ToString() ) );
            } while ( offset < remaining );

            /* Wait for the remaining entries to download */
            string[] responses = await Task.WhenAll( tasks );

            List<LibraryEntry> entries = BuildLibraryEntries( response.data );
            foreach ( string json in responses ) {
                response = JObject.Parse( json );
                List<LibraryEntry> new_entries = BuildLibraryEntries( response.data );
                entries.AddRange( new_entries );
            }

            List<Task> title_tasks = new List<Task>();
            foreach ( LibraryEntry entry in entries ) {
                title_tasks.Add( FetchAnimeAttributes( entry ) );
            }
            Task.WhenAll( title_tasks ).Wait();

            return entries;
        }

        private static async Task FetchAnimeAttributes( LibraryEntry entry ) {
            string content = await client.GetStringAsync( entry.AnimeUrl );
            dynamic response = JObject.Parse( content );
            Dictionary<string, string> titles = JsonConvert.DeserializeObject<Dictionary<string, string>>( response.data.attributes.titles.ToString() );
            
            string title = "";
            if ( titles.ContainsKey( "en" ) ) {
                title = titles["en"];
            } else if ( titles.ContainsKey( "en_us" ) ) {
                title = titles["en_us"];
            } else if ( titles.ContainsKey( "en_jp" ) ) {
                title = titles["en_jp"];
            } else if ( titles.ContainsKey( "ja_jp" ) ) {
                title = titles["ja_jp"];
            } else {
                title = response.data.attributes.canonicalTitle;
            }

            entry.Title = title;
            entry.Total = 0;
            if ( response.data.attributes.episodeCount != null ) {
                entry.Total = response.data.attributes.episodeCount;
            }
        }

        private static List<LibraryEntry> BuildLibraryEntries( dynamic entries ) {
            List<LibraryEntry> new_entries = new List<LibraryEntry>();
            foreach ( dynamic entry in entries ) {
                dynamic attributes = entry.attributes;
                LibraryEntry new_entry = new LibraryEntry()
                {
                    Id = entry.id,
                    Rating = attributes.rating,
                    Status = attributes.status,
                    Progress = attributes.progress,
                    Started = attributes.createdAt,
                    LastUpdated = attributes.updatedAt,
                    AnimeUrl = entry.relationships.anime.links.related
                };

                new_entries.Add( new_entry );
            }

            return new_entries;
        }
    }
}