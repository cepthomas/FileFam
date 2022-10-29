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
using System.Linq.Expressions;
using System.Collections;

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
    public sealed class Db : IEnumerable<TrackedFile> //: IQueryable<TrackedFile>
    {
        #region Fields
        /// <summary>The filename if available.</summary>
        string _fn = "";
        #endregion

        #region API convenience - must match TrackedFile
        public const int FullNameOrdinal = 0;
        public const int IdOrdinal = 1;
        public const int InfoOrdinal = 2;
        public const int TagsOrdinal = 3;
        #endregion

        //IQueryable<T> Paginate<T>(this IQueryable<T> query, int skip, int take)
        //{
        //    return query.Skip(skip).Take(take);
        //}


        #region Properties
        /*public*/
        List<TrackedFile> _files = new();

        /// <summary>All tags in order from most common. Cached, not persisted.</summary>
        public List<string> AllTags { get; set; } = new();
        #endregion


        //public Type ElementType => throw new NotImplementedException();//typeof(TrackedFile);

        //public Expression Expression => throw new NotImplementedException();

        //public IQueryProvider Provider => throw new NotImplementedException();



        public IEnumerator<TrackedFile> GetEnumerator()
        {
            return _files.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _files.GetEnumerator();
        }


        /// <summary>
        /// Get the full name at the row index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>The name or an empty string if invalid.</returns>
        public string GetFullName(int index)
        {
            var fn = "";
            if (index < _files.Count && index >= 0)
            {
                fn = _files[index].FullName;
            }
            return fn;
        }


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
                records.ForEach(r => _files.Add(r));
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
            var recs = _files;
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
                    _files.Sort((a, b) => dir * a.FullName.CompareTo(b.FullName));
                    break;

                case IdOrdinal:
                    _files.Sort((a, b) => dir * a.Id.CompareTo(b.Id));
                    break;

                case InfoOrdinal:
                    _files.Sort((a, b) => dir * a.Info.CompareTo(b.Info));
                    break;

                case TagsOrdinal:
                    _files.Sort((a, b) => dir * a.Tags.CompareTo(b.Tags));
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

            _files
                .ForEach(f => f.Tags.SplitByToken(" ")
                .ForEach(t =>
                {
                    if (!_tags.ContainsKey(t))
                    {
                        _tags.Add(t, 0);
                    }
                    _tags[t]++;
                }
            ));

            _tags
                .OrderByDescending(t => t.Value)
                .ForEach(t => AllTags.Add(t.Key));
        }

        void UpdateTags_simple()
        {
            AllTags.Clear();
            Dictionary<string, int> _tags = new();

            foreach (var file in _files)
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
            // but cleverness can be hard to read and understand and change.

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
            _files.Clear();
            AllTags.Clear();

            for (int i = 1; i < num; i++)
            {
                var e = new TrackedFile($"FullName{i}", $"Id{i}", $"Info{i}", "TAG1 TAG2 TAG3");
                _files.Add(e);
            }
        }
        #endregion
    }
}
