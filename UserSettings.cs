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
    public sealed class UserSettings : SettingsCore
    {
        #region Persisted Editable Properties
        [DisplayName("Control Color")]
        [Description("Pick what you like.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.MediumOrchid;

        [DisplayName("Filters")]
        [Description("Select only these file types.")]
        [Category("Files")]
        [Browsable(true)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))]
        public List<string> FileFilters { get; set; } = new();

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("File Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;
        #endregion

        #region Persisted Non-editable Persisted Properties
        [Browsable(false)]
        public int FullNameWidth { get; set; } = 300;

        [Browsable(false)]
        public int IdWidth { get; set; } = 200;

        [Browsable(false)]
        public int InfoWidth { get; set; } = 200;
        #endregion
    }
}
