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
    public record TrackedEntity(string Path, bool IsDir, List<string> Tags);

    /// <summary>
    /// Storage for tracked files and tags. Could be sqlite also.
    /// </summary>
    public sealed class Db
    {
        #region Fields
        /// <summary>The filename if available.</summary>
        string _fn = "";

        /// <summary>The records. Key is file path.</summary>
        readonly Dictionary<string, TrackedEntity> _entities = new();

        /// <summary>Cache - not persisted.</summary>
        readonly HashSet<string> _allTags = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Load from file.
        /// </summary>
        /// <param name="fn">Where the data lives.</param>
        /// <returns></returns>
        public void Load(string fn)
        {
            JsonSerializerOptions opts = new() { AllowTrailingCommas = true };
            string json = File.ReadAllText(fn);
            var records = (List<TrackedEntity>)JsonSerializer.Deserialize(json, typeof(List<TrackedEntity>), opts)!;

            _allTags.Clear();
            foreach (var rec in records)
            {
                _entities.Add(rec.Path, rec);
                rec.Tags.ForEach(t => _allTags.Add(t));
            }

            _fn = fn;
        }

        /// <summary>
        /// Save object to file or original if empty.
        /// </summary>
        /// <param name="fn">Where the data lives.</param>
        public void Save(string fn = "")
        {
            _fn = fn == "" ? _fn : fn;
            JsonSerializerOptions opts = new() { WriteIndented = false };
            var recs = _entities.Values.ToList();
            string json = JsonSerializer.Serialize(recs, typeof(List<TrackedEntity>), opts);
            File.WriteAllText(_fn, json);
        }
        #endregion

        #region Public API
        /// <summary>
        /// Get tags associated with the path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public List<string>? GetTags(string path)
        {
            var rec = _entities.GetValueOrDefault(path);
            return rec is not null ? rec.Tags : null;
        }

        /// <summary>
        /// Get string of tags associated with the path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string GetTagsString(string path)
        {
            string s = ""; // default
            var tags = GetTags(path);
            if (tags is not null && tags.Count > 0)
            {
                s = $" [{string.Join(" ", tags)}]";
            }

            return s;
        }

        /// <summary>
        /// Set tags associated with the path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="tags"></param>
        public void SetTags(string path, List<string> tags)
        {
            _entities[path] = new(path, true, tags ?? new());
        }

        /// <summary>
        /// Remove a record.
        /// </summary>
        /// <param name="path"></param>
        public void Remove(string path)
        {
            _entities.Remove(path);
        }
        #endregion

        #region Internals
        /// <summary>
        /// Debug stuff.
        /// </summary>
        public void FillFake()
        {
            _entities.Clear();
            _allTags.Clear();

            var rnd = new Random(DateTime.Now.Millisecond);

            for (int i = 1; i < rnd.Next(5, 10); i++)
            {
                var e = new TrackedEntity(Path: $"Path{i}", IsDir: i % 3 == 0, Tags: new List<string> { "TAG1", "TAG2", "TAG3" });
                _entities.Add(e.Path, e);
            }
        }
        #endregion
    }
}
