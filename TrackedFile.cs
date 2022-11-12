using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;


namespace Ephemera.FileFam
{
    /// <summary>The persisted record definition. Analagous to one db row.</summary>
    [Serializable]
    public class TrackedFile
    {
        #region Fields
        /// <summary>Unique id incremented.</summary>
        static int _uids = 0;
        #endregion

        #region API convenience
        public const int FullNameOrdinal = 0;
        public const int IdOrdinal = 1;
        public const int LastAccessOrdinal = 2;
        public const int TagsOrdinal = 3;
        public const int InfoOrdinal = 4;
        #endregion

        #region Properties
        /// <summary>The full path to the file.</summary>
        public string FullName { get; set; } = "???";

        /// <summary>Optional identifier for application use.</summary>
        public string Id { get; set; } = "";

        /// <summary>Last time file was opened/clicked in UI. Not the same as file write/read time.</summary>
        public DateTime LastAccess { get; set; } = DateTime.Now;

        /// <summary>Space delimited filter tags.</summary>
        public string Tags { get; set; } = "";

        /// <summary>Optional free form text.</summary>
        public string Info { get; set; } = "";

        /// <summary>Unique id for internal use.</summary>
        [JsonIgnore]
        public int UID { get; private set; } = -1;

        /// <summary>Convenience property.</summary>
        [JsonIgnore]
        public List<string> TagsList { get { return Tags.SplitByToken(" "); } set { Tags = string.Join(" ", value); } }
        #endregion

        #region public API
        /// <summary>Constructor assigns id.</summary>
        public TrackedFile()
        {
            // Next id.
            UID = _uids++;
        }

        /// <summary>Suitable for adding to listview subitems. In ordinal order.</summary>
        [JsonIgnore]
        public object[] Values { get { return new object[] { FullName, Id, LastAccess, Tags, Info }; } }

        /// <summary>Suitable for adding to listview subitems. In ordinal order.</summary>
        [JsonIgnore]
        public static string[] ColumnNames { get { return new string[] {"FullName", "Id", "LastAccess", "Tags", "Info" }; } }

        /// <summary>Suitable for adding to listview columns. In ordinal order.</summary>
        [JsonIgnore]
        public string[] ValueStrings { get { return new string[] { FullName.ToString(), Id, LastAccess.ToString(), Tags, Info }; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ordinal"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public string StringAt(int ordinal)
        {
            return ordinal switch
            {
                FullNameOrdinal => FullName.ToString(),
                IdOrdinal => Id,
                LastAccessOrdinal => LastAccess.ToString(),
                TagsOrdinal => Tags,
                InfoOrdinal => Info,
                _ => throw new InvalidOperationException($"ordinal:{ordinal}"),
            };
        }

        /// <summary>
        /// Parse a string into a property.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="ordinal"></param>
        /// <returns>True if successful parse.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool Parse(string s, int ordinal)
        {
            bool ok = false;

            switch (ordinal)
            {
                case FullNameOrdinal:
                    FullName = s;
                    ok = true;
                    break;

                case IdOrdinal:
                    Id = s;
                    ok = true;
                    break;

                case LastAccessOrdinal:
                    if (DateTime.TryParse(s, out DateTime dt))
                    {
                        LastAccess = dt;
                        ok = true;
                    }
                    break;

                case TagsOrdinal:
                    Tags = s;
                    ok = true;
                    break;

                case InfoOrdinal:
                    Info = s;
                    ok = true;
                    break;

                default:
                    throw new InvalidOperationException($"ordinal:{ordinal}");
            }
            return ok;
        }
        /// <summary>Unique id for internal use.</summary>
        #endregion
    }
}