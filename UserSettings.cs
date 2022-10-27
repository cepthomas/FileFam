using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json.Serialization;
using System.Drawing.Design;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.NBagOfTricks.Slog;


namespace Ephemera.NotrApp
{
    [Serializable]
    public sealed class UserSettings : SettingsCore // TODO1 scrub.
    {
        #region Persisted Editable Properties
        [DisplayName("Control Color")]
        [Description("Pick what you like.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.MediumOrchid;

        // [DisplayName("Root Paths")]
        // [Description("Your favorite places.")]
        // [Category("Files")]
        // [Browsable(true)]
        // [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        // public List<string> RootDirs { get; set; } = new();

        [DisplayName("Filters")]
        [Description("Show only these file types.")]
        [Category("Files")]
        [Browsable(true)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        public List<string> FilterExts { get; set; } = new();

        ///// <summary></summary>
        //[DisplayName("Ignore Dirs")]
        //[Description("Ignore these noisy directories.")]
        //[Category("Files")]
        //[Browsable(true)]
        //[Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        //public List<string> IgnoreDirs { get; set; } = new();

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

        //[DisplayName("Single Click Select")]
        //[Description("Generate event with single or double click.")]
        //[Browsable(true)]
        //public bool SingleClickSelect { get; set; } = false;
        #endregion

        #region Persisted Non-editable Persisted Properties
        //[Browsable(false)]
        //[Range(10, 80)]
        //public int SplitterPosition { get; set; } = 30;

        [Browsable(false)]
        public int FullNameWidth { get; set; } = 300;

        [Browsable(false)]
        public int IdWidth { get; set; } = 300;

        [Browsable(false)]
        public int InfoWidth { get; set; } = 200;
        #endregion
    }
}
