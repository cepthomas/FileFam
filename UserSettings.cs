using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json.Serialization;
using System.Drawing.Design;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.NBagOfTricks.Slog;


namespace Ephemera.FileFam
{
    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        #region Persisted Editable Properties
        [DisplayName("Tracked File Types")]
        [Description("All the supported file types.")]
        [Browsable(true)]
       // [TypeConverter(typeof(ExpandableObjectConverter))]
        public List<FileCategory> TrackedFileTypes { get; set; } = new();
        #endregion

        #region Persisted Non-editable Persisted Properties
        [Browsable(false)]
        public List<int> ColumnWidths { get; set; } = new();

        [Browsable(false)]
        public List<string> CurrentTags { get; set; } = new();
        #endregion
    }

    [Serializable]
    public sealed class FileCategory
    {
        #region Persisted Editable Properties
        [DisplayName("Category Name")]
        [Description("What appears in the file selector.")]
        [Browsable(true)]
        public string CategoryName { get; set; } = "???";

        [DisplayName("Extensions")]
        [Description("Select only these file types.")]
        [Browsable(true)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        public List<string> Extensions { get; set; } = new();

        [DisplayName("Target Command")]
        [Description("What to execute when file selected - %F is replaced with file name.")]
        [Browsable(true)]
        public string TargetCommand { get; set; } = "%F";
        #endregion
    }
}
