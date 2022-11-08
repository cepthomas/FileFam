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
// var query =
//    from c in db.Customers
//    where c.Name.StartsWith ("A") || "B"
//    orderby c.Name
//    select c.Name.ToUpper();
// var thirdPage = query.Skip(20).Take(10);
// You might have noticed another more subtle (but important) benefit of the LINQ approach. We chose to compose the query in two steps—and this allows us to 
// generalize the second step into a reusable method as follows:
// IQueryable<T> Paginate<T> (this IQueryable<T> query, int skip, int take)
// {
//    return query.Skip(skip).Take(take);
// }


//DataStore.Records.Remove(rec!);
//DataStore.Records.Where(rec => rec.Tags.SplitByToken(" ").Intersect(filterTags).Any()) :
//                DataStore.Records;
//DataStore.Records.Clear();
//DataStore.Records.Add(new()


namespace Ephemera.FileFam
{
    /// <summary>
    /// Storage for tracked files and tags. Could be sqlite also.
    /// </summary>
    public sealed class DataStore : IEnumerable<Record>, IList<Record>
    {
        #region Fields
        /// <summary>The filename if available.</summary>
        string _fn = "";
        #endregion

        List<Record> _records = new();

        //public static List<Record> Records { get; private set; } = new(); // raw

        // //public static List<ColumnSpec> ColumnSpecs { get; private set; } = new();


        #region IEnumerable implementation
        public IEnumerator<Record> GetEnumerator()
        {
            return _records.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _records.GetEnumerator();
        }
        #endregion


        #region IList implementation
        public int Count => _records.Count;

        public bool IsReadOnly => false;

        public Record this[int index] { get => _records[index]; set => _records[index] = value; }

        public int IndexOf(Record item) { return _records.IndexOf(item); }

        public void Insert(int index, Record item) { _records.Insert(index, item); }

        public void RemoveAt(int index) { _records.RemoveAt(index); }

        public void Add(Record item) { _records.Add(item); }

        public void Clear() { _records.Clear(); }

        public bool Contains(Record item) { return _records.Contains(item); }

        public void CopyTo(Record[] array, int arrayIndex) { _records.CopyTo(array, arrayIndex); }

        public bool Remove(Record item) { return _records.Remove(item); }
        #endregion

        /*

        #region Helpers TODO1 client do these? or aka Stored procedures?
        /// <summary>All tags in order from most common.</summary>
        public List<string> GetAllTags()
        {
            List<string> allTags = new();

            Dictionary<string, int> _tags = new();

            _records
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
            if (index < _records.Count && index >= 0)
            {
                fn = _records[index].FullName;
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
                    _records.Sort((a, b) => dir * a.FullName.CompareTo(b.FullName));
                    break;

                case IdOrdinal:
                    _records.Sort((a, b) => dir * a.Id.CompareTo(b.Id));
                    break;

                case InfoOrdinal:
                    _records.Sort((a, b) => dir * a.Info.CompareTo(b.Info));
                    break;

                case TagsOrdinal:
                    _records.Sort((a, b) => dir * a.Tags.CompareTo(b.Tags));
                    break;

                case LastAccessOrdinal:
                    _records.Sort((a, b) => dir * a.LastAccess.CompareTo(b.LastAccess));
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

        //    _records
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

        //    foreach (var file in _records)
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
            _records.Clear();
            //            AllTags.Clear();

            for (int i = 1; i < num; i++)
            {
                var e = new Record()
                {
                    FullName = $"FullName{i}",
                    Id = $"Id{i}",
                    Tags = "TAG1 TAG2 TAG3",
                    LastAccess = DateTime.Now,
                    Info = $"Info{i}",
                };
                _records.Add(e);
            }
        }

        #endregion
        */


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
                var records = (List<Record>)JsonSerializer.Deserialize(json, typeof(List<Record>), opts)!;
                records.ForEach(r => _records.Add(r));
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
            var recs = _records;
            string json = JsonSerializer.Serialize(recs, typeof(List<Record>), opts);
            File.WriteAllText(_fn, json);
        }

        #endregion
    }



    //https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/attributes/creating-custom-attributes
    [AttributeUsage(AttributeTargets.Property)]
    public class OrdinalAttribute : Attribute
    {
        public int Value { get; set; }
        public OrdinalAttribute(int value) { Value = value; }
    }

    //// Columns.
    //ColumnSpecs.Clear();

    //// These match the order in the object.
    //ColumnSpecs.Add(new("Number", typeof(int)));
    //ColumnSpecs.Add(new("Name", typeof(string)));
    //ColumnSpecs.Add(new("Touch", typeof(DateTime)));
    //ColumnSpecs.Add(new("Tags", typeof(string)));
    //ColumnSpecs.Add(new("Info", typeof(string)));

    //var t = typeof(Record);
    //var tps = t.GetProperties();
    //foreach(var tp in tps)
    //{
    //    var pname = tp.Name;
    //    var ptype = tp.GetType();

    //    foreach (var pa in tp.GetCustomAttributes(false))
    //    {
    //        if (pa is OrdinalAttribute)
    //        {
    //            int ordinal = (pa as OrdinalAttribute)!.Value;
    //            var name = tp.Name;
    //            var type = tp.PropertyType;
    //            ColumnSpecs.Add(new(name, type));//, ordinal);//);
    //        }
    //    }
    //}
    //? ColumnSpecs.Sort((a, b) => a.Ordinal.CompareTo(b.Ordinal));

    public class ColumnSpec
    {
        //public int Ordinal { get; private set; } = -1;

        public string Name { get; private set; } = "???";
        public Type Type { get; private set; } = typeof(object);
        public int Width { get; set; } = 0;

        public ColumnSpec(string name, Type type)//, int ordinal)
        {
            Name = name;
            Type = type;
            Width = 0;
            //Ordinal = ordinal;
        }
    }

}
