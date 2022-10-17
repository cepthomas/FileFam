using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json.Serialization;
using System.Drawing.Design;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.NBagOfTricks.Slog;


namespace Ephemera.Notr
{
    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        #region Persisted Editable Properties
        [DisplayName("Control Color")]
        [Description("Pick what you like.")]
        [Category("\t\tCosmetics")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.MediumOrchid;

        [DisplayName("Root Paths")]
        [Description("Your favorite places.")]
        [Category("Files")]
        [Browsable(true)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        public List<string> RootDirs { get; set; } = new();

        [DisplayName("Filters")]
        [Description("Show only these file types. Empty is valid for files without extensions.")]
        [Category("Files")]
        [Browsable(true)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        public List<string> FilterExts { get; set; } = new();

        /// <summary></summary>
        [DisplayName("Ignore Paths")]
        [Description("Ignore these noisy directories.")]
        [Category("Files")]
        [Browsable(true)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        public List<string> IgnoreDirs { get; set; } = new();

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Category("\tFunctionality")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("File Log Level")]
        [Description("Log level for UI notification.")]
        [Category("\tFunctionality")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;

        [DisplayName("Single Click Select")]
        [Description("Generate event with single or double click.")]
        [Browsable(true)]
        public bool SingleClickSelect { get; set; } = false;

        #endregion

        #region Persisted Non-editable Persisted Properties
        [Browsable(false)]
        public int SplitterPosition { get; set; } = 30;
        #endregion
    }
}
