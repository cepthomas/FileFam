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
using System.Linq.Expressions;
using System.Collections;
using System.ComponentModel;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfTricks;




// https://www.linqpad.net/WhyLINQBeatsSQL.aspx
// 
// 
// var query =
//    from c in db.Customers
//    where c.Name.StartsWith ("A")
//    orderby c.Name
//    select c.Name.ToUpper();
// 
// var thirdPage = query.Skip(20).Take(10);
// 
// 
// You might have noticed another more subtle (but important) benefit of the LINQ approach. We chose to compose the query in two steps—and this allows us to // generalize the second step into a reusable method as follows:
// 
// IQueryable<T> Paginate<T> (this IQueryable<T> query, int skip, int take)
// {
//    return query.Skip(skip).Take(take);
// }
// We can then do this:
// 
// var query = ...
// var thirdPage = query.Paginate (20, 10);
// The important thing, here, is that we can apply our Paginate method to any query. In other words, with LINQ you can break down a query into parts, and then re-use // some of those parts across your application.
// 
// Another benefit of LINQ is that you can query across relationships without having to join. For instance, suppose we want to list all purchases of $1000 or greater // made by customers who live in Washington. To make it interesting, we'll assume purchases are itemized (the classic Purchase / PurchaseItem scenario) and that we // also want to include cash sales (with no customer). This requires querying across four tables (Purchase, Customer, Address and PurchaseItem). In LINQ, the query // is effortless:
// 
// from p in db.Purchases
// where p.Customer.Address.State == "WA" || p.Customer == null
// where p.PurchaseItems.Sum (pi => pi.SaleAmount) > 1000
// select p




namespace Ephemera.FileFam
{
    /// <summary>The persisted record template.</summary>
    [Serializable]
    public class TrackedFile
    {
        public string FullName { get; set; } = "???";
        public string Id { get; set; } = "iii";
        public string Tags { get; set; } = "ttt";
        public DateTime LastAccess { get; set; } = DateTime.Now;
        public string Info { get; set; } = "---";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TrackedFile()
        {
        }

        ///// <summary>
        ///// Full constructor
        ///// </summary>
        ///// <param name="fullName"></param>
        ///// <param name="id"></param>
        ///// <param name="info"></param>
        ///// <param name="tags"></param>
        //public TrackedFile(string fullName, string id, string info, string tags)
        //{
        //    FullName = fullName;
        //    Id = id;
        //    Info = info;
        //    Tags = tags;
        //}
    }



    /// <summary>
    /// Storage for tracked files and tags. Could be sqlite also.
    /// </summary>
    public sealed class Db : IEnumerable<TrackedFile>
    {
        #region Fields
        /// <summary>The filename if available.</summary>
        string _fn = "";
        #endregion

        List<TrackedFile> _files = new();

        //SortableBindingList<TrackedFile> _files = new();

        //SortableBindingList<CalDataPoint> CalDataSet = new SortableBindingList<CalDataPoint>();

        #region API convenience - must match TrackedFile TODO1 better way?
        public const int FullNameOrdinal = 0;
        public const int IdOrdinal = 1;
        public const int TagsOrdinal = 2;
        public const int LastAccessOrdinal = 3;
        public const int InfoOrdinal = 4;
        #endregion


        #region IEnumerable implementation
        public IEnumerator<TrackedFile> GetEnumerator()
        {
            return _files.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _files.GetEnumerator();
        }
        #endregion



        #region Helpers TODO1 client do these? or aka Stored procedures?
        /// <summary>All tags in order from most common.</summary>
        public List<string> GetAllTags()
        {
            List<string> allTags = new();

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
                .ForEach(t => allTags.Add(t.Key));

            return allTags;
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

                case LastAccessOrdinal:
                    _files.Sort((a, b) => dir * a.LastAccess.CompareTo(b.LastAccess));
                    break;

                default:
                    break;
            }
        }

        ///// <summary>
        ///// Helper.
        ///// </summary>
        //void UpdateTags()
        //{
        //    AllTags.Clear();
        //    Dictionary<string, int> _tags = new();

        //    _files
        //        .ForEach(f => f.Tags.SplitByToken(" ")
        //        .ForEach(t =>
        //        {
        //            if (!_tags.ContainsKey(t))
        //            {
        //                _tags.Add(t, 0);
        //            }
        //            _tags[t]++;
        //        }
        //    ));

        //    _tags
        //        .OrderByDescending(t => t.Value)
        //        .ForEach(t => AllTags.Add(t.Key));
        //}

        //void UpdateTags_simple()
        //{
        //    AllTags.Clear();
        //    Dictionary<string, int> _tags = new();

        //    foreach (var file in _files)
        //    {
        //        foreach (var tag in file.Tags.SplitByToken(" "))
        //        {
        //            if (!_tags.ContainsKey(tag))
        //            {
        //                _tags.Add(tag, 0);
        //            }
        //            _tags[tag]++;
        //        }
        //    }
        //    // I could scrunch this on one line:
        //    //Files.ForEach(f => f.Tags.SplitByToken(" ").ForEach(t => { if (!_tags.ContainsKey(t)) _tags.Add(t, 0); _tags[t]++; }));
        //    // but cleverness can be hard to read and understand and change.

        //    foreach (var tag in _tags.OrderByDescending(t => t.Value))
        //    {
        //        AllTags.Add(tag.Key);
        //    }
        //    // _tags.OrderByDescending(t => t.Value).ForEach(t => AllTags.Add(t.Key));
        //}

        /// <summary>
        /// Debug stuff.
        /// </summary>
        void FillFake(int num = 10)
        {
            _files.Clear();
            //            AllTags.Clear();

            for (int i = 1; i < num; i++)
            {
                var e = new TrackedFile()
                {
                    FullName = $"FullName{i}",
                    Id = $"Id{i}",
                    Tags = "TAG1 TAG2 TAG3",
                    LastAccess = DateTime.Now,
                    Info = $"Info{i}",
                };
                _files.Add(e);
            }
        }

        #endregion



        #region Public API
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
 //               UpdateTags();
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
    }
}
