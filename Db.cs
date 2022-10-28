using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfTricks;

// TODO1 should Tags and Id be case insensitive?

namespace Ephemera.FileFam
{
    /// <summary>The persisted record template.</summary>
    [Serializable]
    public class TrackedFile
    {
        public string FullName { get; set; } = "???";
        public string Id { get; set; } = "";
        public string Info { get; set; } = "";
        public string Tags { get; set; } = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TrackedFile()
        {
        }

        /// <summary>
        /// Full constructor
        /// </summary>
        /// <param name="fullName"></param>
        /// <param name="id"></param>
        /// <param name="info"></param>
        /// <param name="tags"></param>
        public TrackedFile(string fullName, string id, string info, string tags)
        {
            FullName = fullName;
            Id = id;
            Info = info;
            Tags = tags;
        }
    }

    /// <summary>
    /// Storage for tracked files and tags. Could be sqlite also.
    /// </summary>
    public sealed class Db
    {
        #region Fields
        /// <summary>The filename if available.</summary>
        string _fn = "";

        /// <summary>API convenience. Must match TrackedFile. TODO1 smarter way?</summary>
        public const int FullNameOrdinal = 0;
        public const int IdOrdinal = 1;
        public const int InfoOrdinal = 2;
        public const int TagsOrdinal = 3;
        #endregion

        #region Properties
        public List<TrackedFile> Files { get; set; } = new();

        /// <summary>All tags in order from most common. Cached, not persisted.</summary>
        public List<string> AllTags { get; set; } = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Load db from file.
        /// </summary>
        /// <param name="fn">Where the data lives.</param>
        /// <returns>Success.</returns>
        public bool Load(string fn)
        {
            bool ok = true;

            try
            {
                JsonSerializerOptions opts = new() { AllowTrailingCommas = true };
                string json = File.ReadAllText(fn);
                var records = (List<TrackedFile>)JsonSerializer.Deserialize(json, typeof(List<TrackedFile>), opts)!;
                records.ForEach(r => Files.Add(r));
                UpdateTags();
            }
            catch
            {
                // Let's just assume it's a new file.
                ok = false;
            }

            _fn = fn;

            return ok;
        }

        /// <summary>
        /// Save object to file or original if empty.
        /// </summary>
        /// <param name="fn">Where the data lives.</param>
        public void Save(string fn = "")
        {
            _fn = fn == "" ? _fn : fn;
            JsonSerializerOptions opts = new() { WriteIndented = true };
            var recs = Files;
            string json = JsonSerializer.Serialize(recs, typeof(List<TrackedFile>), opts);
            File.WriteAllText(_fn, json);
        }
        #endregion

        #region Public API
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ordinal">Ordinal to sort on.</param>
        /// <param name="asc">Which way boss?</param>
        public void Sort(int ordinal, bool asc)
        {
            int dir = asc ? 1 : -1;

            switch (ordinal)
            {
                case FullNameOrdinal:
                    Files.Sort((a, b) => dir * a.FullName.CompareTo(b.FullName));
                    break;

                case IdOrdinal:
                    Files.Sort((a, b) => dir * a.Id.CompareTo(b.Id));
                    break;

                case InfoOrdinal:
                    Files.Sort((a, b) => dir * a.Info.CompareTo(b.Info));
                    break;

                case TagsOrdinal:
                    Files.Sort((a, b) => dir * a.Tags.CompareTo(b.Tags));
                    break;

                default:
                    break;
            }
        }
        #endregion

        #region Internals
        /// <summary>
        /// Helper.
        /// </summary>
        void UpdateTags()
        {
            AllTags.Clear();
            Dictionary<string, int> _tags = new();

            foreach (var file in Files)
            {
                foreach (var tag in file.Tags.SplitByToken(" "))
                {
                    if (!_tags.ContainsKey(tag))
                    {
                        _tags.Add(tag, 0);
                    }
                    _tags[tag]++;
                }
            }
            // I could scrunch this on one line:
            //Files.ForEach(f => f.Tags.SplitByToken(" ").ForEach(t => { if (!_tags.ContainsKey(t)) _tags.Add(t, 0); _tags[t]++; }));
            // but cleverness can be hard to read and understand.

            foreach (var tag in _tags.OrderByDescending(t => t.Value))
            {
                AllTags.Add(tag.Key);
            }
            // _tags.OrderByDescending(t => t.Value).ForEach(t => AllTags.Add(t.Key));
        }

        /// <summary>
        /// Debug stuff.
        /// </summary>
        void FillFake(int num = 10)
        {
            Files.Clear();
            AllTags.Clear();

            for (int i = 1; i < num; i++)
            {
                var e = new TrackedFile($"FullName{i}", $"Id{i}", $"Info{i}", "TAG1 TAG2 TAG3");
                Files.Add(e);
            }
        }
        #endregion
    }
}
