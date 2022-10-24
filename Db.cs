using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfTricks;



namespace Ephemera.NotrApp
{
    /// <summary>The persisted record template.</summary>
    [Serializable]
    //public record TrackedFile(string FullName, string Id, string Info, string Tags);
    //public record TrackedEntity(string FullName, string Id, string Info, List<string> Tags);
    // public record TrackedEntity(string Path, bool IsDir, List<string> Tags);

    public class TrackedFile
    {
        public string FullName { get; set; } = "???";
        public string Id { get; set; } = "???";
        public string Info { get; set; } = "???";
        public string Tags { get; set; } = "???";

        public TrackedFile()
        {
        }

        public TrackedFile(string fullName, string id, string info, string tags)
        {
            FullName = fullName;
            Id = id;
            Info = info;
            Tags = tags;
        }
    }

    //Name, FullName

    /// <summary>
    /// Storage for tracked files and tags. Could be sqlite also.
    /// </summary>
    public sealed class Db
    {
        #region Fields
        /// <summary>The filename if available.</summary>
        string _fn = "";

        ///// <summary>The records. Key is file path.</summary>
        //readonly Dictionary<string, TrackedFile> _files = new();

        public List<TrackedFile> Files { get; set; } = new();

        /// <summary>Cache - not persisted.</summary>
        readonly List<string> _allTags = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Load db from file.
        /// </summary>
        /// <param name="fn">Where the data lives.</param>
        /// <returns></returns>
        public void Load(string fn)
        {
            JsonSerializerOptions opts = new() { AllowTrailingCommas = true };
            string json = File.ReadAllText(fn);
            var records = (List<TrackedFile>)JsonSerializer.Deserialize(json, typeof(List<TrackedFile>), opts)!;

            foreach (var rec in records)
            {
   //             _files.Add(rec.FullName, rec);
                Files.Add(rec);
            }

            UpdateTags();

            _fn = fn;
        }

        /// <summary>
        /// Save object to file or original if empty.
        /// </summary>
        /// <param name="fn">Where the data lives.</param>
        public void Save(string fn = "")
        {
            _fn = fn == "" ? _fn : fn;
            JsonSerializerOptions opts = new() { WriteIndented = true }; // TODO1 indent for debug only.
            var recs = Files;
            string json = JsonSerializer.Serialize(recs, typeof(List<TrackedFile>), opts);
            File.WriteAllText(_fn, json);
        }
        #endregion

        #region Public API
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prop">Property name to sort on.</param>
        /// <param name="asc">Which way boss?</param>
        public void Sort(string prop, bool asc)
        {
            int dir = asc ? 1 : -1;

            switch (prop)
            {
                case "FullName":
                    Files.Sort((a, b) => dir * a.FullName.CompareTo(b.FullName));
                    break;

                case "Id":
                    Files.Sort((a, b) => dir * a.Id.CompareTo(b.Id));
                    break;

                case "Info":
                    Files.Sort((a, b) => dir * a.Info.CompareTo(b.Info));
                    break;

                case "Tags":
                    Files.Sort((a, b) => dir * a.Tags.CompareTo(b.Tags));
                    break;

                default:
                    break;
            }
        }

        ///// <summary>
        ///// Get tags associated with the path.
        ///// </summary>
        ///// <param name="path"></param>
        ///// <returns></returns>
        //public List<string>? GetTags(string path)
        //{
        //    var rec = _entities.GetValueOrDefault(path);
        //    //return rec is not null ? rec.Tags : null;
        //    return rec is not null ? rec.Tags : null;
        //}

        ///// <summary>
        ///// Get string of tags associated with the path.
        ///// </summary>
        ///// <param name="path"></param>
        ///// <returns></returns>
        //public string GetTagsString(string path)
        //{
        //    string s = ""; // default
        //    var tags = GetTags(path);
        //    if (tags is not null && tags.Count > 0)
        //    {
        //        s = $" [{string.Join(" ", tags)}]";
        //    }

        //    return s;
        //}

        ///// <summary>
        ///// Set tags associated with the name.
        ///// </summary>
        ///// <param name="fullName"></param>
        ///// <param name="tags"></param>
        //public void SetTags(string fullName, string id, string info, List<string> tags)
        //{
        //    _entities[fullName] = new(fullName, id, info, tags ?? new());
        //}

        ///// <summary>
        ///// Remove a record.
        ///// </summary>
        ///// <param name="path"></param>
        //public void Remove(string path)
        //{
        //    _files.Remove(path);
        //}
        #endregion

        #region Internals
        /// <summary>
        /// Helper.
        /// </summary>
        void UpdateTags()
        {
            _allTags.Clear();
            Dictionary<string, int> _tags = new();
            Files.ForEach(f => f.Tags.SplitByToken(" ").ForEach(t => { if (!_tags.ContainsKey(t)) _tags.Add(t, 0); _tags[t]++; }));
            _tags.OrderByDescending(t => t.Value).ForEach(t => _allTags.Add(t.Key));
        }

        /// <summary>
        /// Debug stuff.
        /// </summary>
        public void FillFake()
        {
            Files.Clear();
            _allTags.Clear();

            var rnd = new Random(DateTime.Now.Millisecond);

            for (int i = 1; i < rnd.Next(5, 10); i++)
            {
                var e = new TrackedFile($"FullName{i}", $"Id{i}", $"Info{i}", "TAG1 TAG2 TAG3");
                //var e = new TrackedFile(FullName: $"FullName{i}", Id: $"Id{i}", Info: $"Info{i}", Tags: "TAG1 TAG2 TAG3");
                Files.Add(e);
            }
        }
        #endregion
    }
}
