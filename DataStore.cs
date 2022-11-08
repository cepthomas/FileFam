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


namespace Ephemera.FileFam
{
    /// <summary>
    /// Storage for tracked files and tags. Could be sqlite also.
    /// </summary>
    public sealed class DataStore
    {
        #region Fields
        /// <summary>The filename if available.</summary>
        string _fn = "";
        #endregion

        public List<TrackedFile> TrackedFiles { get; private set; } = new(); // raw

        // //public List<ColumnSpec> ColumnSpecs { get; private set; } = new();


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
                var records = (List<TrackedFile>)JsonSerializer.Deserialize(json, typeof(List<TrackedFile>), opts)!;
                records.ForEach(r => TrackedFiles.Add(r));
                //UpdateTags();
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
            string json = JsonSerializer.Serialize(TrackedFiles, typeof(List<TrackedFile>), opts);
            File.WriteAllText(_fn, json);
        }

        #endregion
    }

    //////////////////////////////// TODO1 maybe stuff /////////////////////////////////////////

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
