using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfTricks;


namespace Ephemera.NotrApp
{

    [Serializable]
    public sealed class Db //TODO1 in filtree
    {
        string _fp = "???";

        [Serializable]
        public sealed class Entry
        {
            public string Path { get; set; } = "???";
            public bool IsDir { get; set; } = false;
            public List<string> Tags { get; set; } = new();
        }

        /// <summary>The records.</summary>
        public List<Entry> Entries { get; set; } = new();

        /// <summary>Cache.</summary>
        [JsonIgnore]
        public HashSet<string> AllTags { get; set; } = new();

        /// <summary>
        /// Load.
        /// </summary>
        /// <param name="appDir">Where the file lives.</param>
        /// <returns></returns>
        public static Db Load(string appDir)
        {
            Db? db = null;

            string fp = Path.Combine(appDir, "db.json");
            if (File.Exists(fp))
            {
                JsonSerializerOptions opts = new() { AllowTrailingCommas = true };
                string json = File.ReadAllText(fp);
                db = (Db?)JsonSerializer.Deserialize(json, typeof(Db), opts);
            }

            if (db is null)
            {
                // Doesn't exist, create a new one.
                db = new();
            }
            else
            {
                foreach (var entry in db.Entries)
                {
                    entry.Tags.ForEach(t => db.AllTags.Add(t));
                }
            }

            db._fp = fp;
            return db!;
        }

        /// <summary>
        /// Save object to file.
        /// </summary>
        public void Save()
        {
            JsonSerializerOptions opts = new() { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, typeof(Db), opts);
            File.WriteAllText(_fp, json);
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
}
