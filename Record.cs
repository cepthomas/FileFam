using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Text.Json.Serialization;


namespace Ephemera.FileFam
{
    /// <summary>The persisted record template.</summary>
    [Serializable]
    public class Record
    {
        [Ordinal(0)]
        public string FullName { get; set; } = "???";

        [Ordinal(1)]
        public string Id { get; set; } = "iii";

        [Ordinal(2)]
        public DateTime LastAccess { get; set; } = DateTime.Now;

        [Ordinal(3)]
        public string Tags { get; set; } = "TAGS";

        [Ordinal(4)]
        public string Info { get; set; } = "XXX";

        /// <summary>Unique id.</summary>
        public int UID { get; private set; } = -1;
        static int _uids = 0;


        // #region API convenience - must match Record TODO1 better way?
        // public const int FullNameOrdinal = 0;
        // public const int IdOrdinal = 1;
        // public const int LastAccessOrdinal = 2;
        // public const int TagsOrdinal = 3;
        // public const int InfoOrdinal = 4;
        // #endregion


        /// <summary>Suitable for adding to listview subitems. In ordinal order.</summary>
        public object[] Values { get { return new object[] { FullName, Id, LastAccess, Tags, Info }; } }

        /// <summary>Suitable for adding to listview subitems. In ordinal order.</summary>
        public static string[] ColumnNames { get { return new string[] {"FullName", "Id", "LastAccess", "Tags", "Info" }; } }

        /// <summary>Suitable for adding to listview columns. In ordinal order. TODO2 or just use Values?</summary>
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
                0 => FullName.ToString(),
                1 => Id,
                2 => LastAccess.ToString(),
                3 => Tags,
                4 => Info,
                _ => throw new InvalidOperationException($"ordinal:{ordinal}"),
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="ordinal"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool Parse(string s, int ordinal)
        {
            bool ok = false;

            switch (ordinal)
            {
                case 0:
                    FullName = s;
                    ok = true;
                    break;

                case 1:
                    Id = s;
                    ok = true;
                    break;

                case 2:
                    if (DateTime.TryParse(s, out DateTime dt))
                    {
                        LastAccess = dt;
                        ok = true;
                    }
                    break;

                case 3:
                    Tags = s;
                    ok = true;
                    break;

                case 4:
                    Info = s;
                    ok = true;
                    break;

                default:
                    throw new InvalidOperationException($"ordinal:{ordinal}");
            }
            return ok;
        }

        /// <summary>
        /// Constructor assigns id.
        /// </summary>
        public Record()
        {
            // Next id.
            UID = ++_uids;
        }
    }
}