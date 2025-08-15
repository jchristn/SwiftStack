namespace SwiftStack
{
    using SerializationHelper;
    using SwiftStack.Rest;
    using SyslogLogging;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// SwiftStack application.
    /// </summary>
    public class SwiftStackApp
    {
        #region Public-Members

        /// <summary>
        /// Application name.
        /// </summary>
        public string Name { get; set; } = "My SwiftStack application";

        /// <summary>
        /// Header to include in emitted log messages.  
        /// Default is [SwiftStackApp].
        /// </summary>
        public string Header
        {
            get
            {
                return _Header;
            }
            set
            {
                if (!String.IsNullOrEmpty(value) && !value.EndsWith(" ")) value += " ";
                if (String.IsNullOrEmpty(value)) _Header = "";
                else _Header = value;
            }
        }

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LoggingSettings LoggingSettings
        {
            get
            {
                return _LoggingSettings;
            }
            set
            {
                if (value == null) value = new LoggingSettings();
                _LoggingSettings = value;
            }
        }

        /// <summary>
        /// Logging servers.
        /// </summary>
        public List<SyslogServer> LoggingServers
        {
            get
            {
                return _LoggingServers;
            }
        }

        /// <summary>
        /// Logger.
        /// </summary>
        public LoggingModule Logging
        {
            get
            {
                return _Logging;
            }
            set
            {
                _Logging = value;
            }
        }

        /// <summary>
        /// JSON serializer.
        /// </summary>
        public Serializer Serializer
        {
            get
            {
                return _Serializer;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Serializer));
                _Serializer = value;
            }
        }

        /// <summary>
        /// REST application.
        /// </summary>
        public RestApp Rest { get; private set; } = null;

        #endregion

        #region Private-Members

        private string _Header = "[SwiftStackApp] ";
        private LoggingModule _Logging = null;
        private Serializer _Serializer = new Serializer();
        private LoggingSettings _LoggingSettings = new LoggingSettings();
        private List<SyslogServer> _LoggingServers = new List<SyslogServer>
        {
            new SyslogServer("127.0.0.1", 514)
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// SwiftStack application.
        /// </summary>
        /// <param name="name">The name of the application.</param>
        /// <param name="quiet">Set to true to disable log messages on startup.</param>
        public SwiftStackApp(string name = "My SwiftStack App", bool quiet = false)
        {
            if (!quiet)
            { 
                Console.WriteLine(
                    Environment.NewLine + Constants.Logo +
                    Environment.NewLine + Constants.Copyright +
                    Environment.NewLine);
            }

            if (!String.IsNullOrEmpty(name)) Name = name;

            _Logging = new LoggingModule(_LoggingServers, _LoggingSettings.EnableConsole);
            _Logging.Settings = _LoggingSettings;

            Rest = new RestApp(this);
            Rest.QuietStartup = quiet;

            _Logging.Info(_Header + "started application " + Name);
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
