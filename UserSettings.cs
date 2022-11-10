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
        [DisplayName("Filters")]
        [Description("Select only these file types.")]
        [Category("Files")]
        [Browsable(true)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        public List<string> TrackedFileFilters { get; set; } = new();

        [DisplayName("Target Command")]
        [Description("What to execute when file selected - %F replaces file name.")]
        [Browsable(true)]
        public string TargetCommand { get; set; } = "explorer \"%F\"";
        #endregion

        #region Persisted Non-editable Persisted Properties
        [Browsable(false)]
        public List<int> ColumnWidths { get; set; } = new();

        [Browsable(false)]
        public List<string> CurrentTags { get; set; } = new();
        #endregion
    }
}
