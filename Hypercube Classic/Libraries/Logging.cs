﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Hypercube_Classic.Libraries {
    public enum LogType {
        Debug,
        Info,
        Warning,
        Error,
        Critical,
        Chat,
        Command,
        NotSet
    }

    public class Logging {
        public bool FileLogging = true, ColoredOutput = false;
        public string LogFile;
        public Hypercube Servercore;
        private short Rotation = 0;

        public Logging(Hypercube Core, string _logFile, bool rotate, bool logging = true) {
            LogFile = _logFile;
            FileLogging = logging;

            if (!Directory.Exists("Logs"))
                Directory.CreateDirectory("Logs");

            if (rotate && File.Exists("Logs\\" + LogFile + ".txt") && logging) 
                RotateLogs();

            Servercore = Core;
        }

        /// <summary>
        /// Prints your message to the console, and if file logging is enabled, to the server's log.
        /// </summary>
        /// <param name="type">The log type. See the LogType Enum.</param>
        /// <param name="module">The module that this log entry is coming from.</param>
        /// <param name="message">The acutal message this log is producing.</param>
        public void _Log(string module, string message, LogType type = LogType.NotSet) {
            if (!ColoredOutput) 
                Console.WriteLine(DateTime.Now.ToShortTimeString() + "> [" + type.ToString() + "] [" + module + "] " + message);
             else {
                switch (type) {
                    case LogType.Debug:
                        ColoredConsole.ColorConvertingConsole.WriteLine(DateTime.Now.ToShortTimeString() + "> " + 
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.DebugConsole) + " " + 
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ConsoleModule) + " " + 
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ConsoleMessage));
                        break;
                    case LogType.Info:
                        ColoredConsole.ColorConvertingConsole.WriteLine(DateTime.Now.ToShortTimeString() + "> " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.InfoConsole) + " " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ConsoleModule) + " " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ConsoleMessage));
                        break;
                    case LogType.Warning:
                        ColoredConsole.ColorConvertingConsole.WriteLine(DateTime.Now.ToShortTimeString() + "> " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.WarningConsole) + " " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ConsoleModule) + " " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ConsoleMessage));
                        break;
                    case LogType.Error:
                        ColoredConsole.ColorConvertingConsole.WriteLine(DateTime.Now.ToShortTimeString() + "> " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ErrorConsole) + " " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ConsoleModule) + " " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ConsoleMessage));
                        break;
                    case LogType.Critical:
                        ColoredConsole.ColorConvertingConsole.WriteLine(DateTime.Now.ToShortTimeString() + "> " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.CriticalConsole) + " " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ConsoleModule) + " " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ConsoleMessage));
                        break;
                    case LogType.Chat:
                        ColoredConsole.ColorConvertingConsole.WriteLine(DateTime.Now.ToShortTimeString() + "> " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ChatConsole) + " " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ConsoleModule) + " " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ConsoleMessage));
                        break;
                    case LogType.Command:
                        ColoredConsole.ColorConvertingConsole.WriteLine(DateTime.Now.ToShortTimeString() + "> " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.CommandConsole) + " " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ConsoleMessage));
                        break;
                    case LogType.NotSet:
                        ColoredConsole.ColorConvertingConsole.WriteLine(DateTime.Now.ToShortTimeString() + "> " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.NotSetConsole) + " " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ConsoleModule) + " " +
                            Text.FormatString(module, type.ToString(), message, Servercore.TextFormats.ConsoleMessage));
                        break;
                }
            }

            if (FileLogging)
                LogWrite(DateTime.Now.ToShortTimeString() + "> [" + type.ToString() + "] [" + module + "] " + message);
        }

        /// <summary>
        /// Rotate the server log file.
        /// </summary>
        public void RotateLogs() {
            string[] files = Directory.GetFiles("Logs");

            foreach (string path in files) {
                string fileName = path.Substring(path.LastIndexOf("\\") + 1, path.Length - (path.LastIndexOf("\\") + 1));

                if (fileName.Substring(0, LogFile.Length + 1) == LogFile + "_") { // -- If the file name ends in _, it is a rotated log.
                    short tempRotation = short.Parse(fileName.Substring(fileName.LastIndexOf("_") + 1, fileName.Length - (fileName.LastIndexOf("_") + 5))); // -- Get the rotation number for that log.

                    if (tempRotation == Rotation)
                        Rotation += 1;
                }
            }

            LogFile = LogFile + "_" + Rotation;
        }

        /// <summary>
        /// Writes a line to the server log file.
        /// </summary>
        /// <param name="line"></param>
        void LogWrite(string line) {
            File.AppendAllText("Logs\\" + LogFile + ".txt", line + "\n");
        }
    }
}
