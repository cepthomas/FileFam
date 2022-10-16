using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data.SQLite;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfTricks;

namespace Ephemera.Notr
{

    [Serializable]
    public sealed class Db : SettingsCore
    {
        /// <summary>The records.</summary>
        public List<Entry> Entries { get; set; } = new();

        /// <summary>Cache.</summary>
        public HashSet<string> AllTags { get; set; } = new();

        /// <summary>
        /// Load.
        /// </summary>
        /// <param name="appDir"></param>
        /// <returns></returns>
        public static Db Load(string appDir)
        {
            Db db = (Db)SettingsCore.Load(appDir, typeof(Db), "db.json");

            foreach (var entry in db.Entries)
            {
                entry.Tags.ForEach(t => db.AllTags.Add(t));
            }

            return db;
        }


        /// <summary>
        /// 
        /// </summary>
        public void FillFake()
        {
            Entries.Clear();
            AllTags.Clear();

            var rnd = new Random(DateTime.Now.Millisecond);

            for (int i = 1; i < rnd.Next(5, 10); i++)
            {
                var e = new Entry()
                {
                    Path = $"Path{i}",
                    IsDir = i % 3 == 0,
                    Tags = new List<string> { "TAG1", "TAG2", "TAG3" }

                };
                Entries.Add(e);
            }
        }
    }

    [Serializable]
    public sealed class Entry
    {
        public string Path { get; set; } = "???";
        public bool IsDir { get; set; } = false;
        public List<string> Tags { get; set; } = new();
    }
}
