using System.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;
using SmartFormat;

namespace MSWSupport
{
    public class ConsoleTextWriter : TextWriter
    {
        // Singleton instance
        private static ConsoleTextWriter? m_instance;
        public static ConsoleTextWriter Instance
        {
            get {
                return m_instance ??= new ConsoleTextWriter();
            }
        }

        private const string DEFAULT_MESSAGE_FORMAT = "{message}";
        private string m_messageFormat = DEFAULT_MESSAGE_FORMAT;
        private readonly TextWriter m_originalOut;
        private readonly Dictionary<string, object> m_messageParameters = new();
        private readonly Dictionary<string, object> m_oneTimeMessageParameters = new();

        private ConsoleTextWriter()
        {
            m_originalOut = Console.Out;
        }

        public string GetMessageFormat()
        {
            return m_messageFormat;
        }

        public void SetMessageFormat(string messageFormat)
        {
            if (!messageFormat.Contains("{message}"))
            {
                throw new ArgumentException("Message format must contain '{message}' parameter.");
            }
            m_messageFormat = messageFormat;
        }

        public object? GetMessageParameter(string parameterName)
        {
            return !m_messageParameters.ContainsKey(parameterName) ? null : m_messageParameters[parameterName];
        }

        public void SetMessageParameter(string parameterName, object? value)
        {
            if (value == null)
            {
                m_messageParameters.Remove(parameterName);
                return;
            }
            m_messageParameters[parameterName] = value;
        }

        public Dictionary<string, object> GetMessageParameters()
        {
            return m_messageParameters;
        }

        public void SetMessageParameters(Dictionary<string, object> parameters)
        {
            foreach (KeyValuePair<string, object> kvp in parameters)
            {
                m_messageParameters[kvp.Key] = kvp.Value;
            }
        }

        public object? GetOneTimeMessageParameter(string parameterName)
        {
            return !m_oneTimeMessageParameters.ContainsKey(parameterName) ? null : m_oneTimeMessageParameters[parameterName];
        }

        public void SetOneTimeMessageParameter(string parameterName, object? value)
        {
            if (value == null)
            {
                m_oneTimeMessageParameters.Remove(parameterName);
                return;
            }
            m_oneTimeMessageParameters[parameterName] = value;
        }

        public Dictionary<string, object> GetOneTimeMessageParameters()
        {
            return m_oneTimeMessageParameters;
        }

        public void SetOneTimeMessageParameters(Dictionary<string, object> parameters)
        {
            foreach (KeyValuePair<string, object> kvp in parameters)
            {
                m_oneTimeMessageParameters[kvp.Key] = kvp.Value;
            }
        }

        public override Encoding Encoding
        {
            get { return new ASCIIEncoding(); }
        }

        public override void WriteLine(string? message)
        {
            Write(message);
            m_originalOut.WriteLine();
        }

        public override void WriteLine(string format, params object?[] arg)
        {
            Write(format, arg);
            m_originalOut.WriteLine();
        }

        public override void Write(string? message)
        {
            WriteFormattedMessage(message);
        }

        public override void Write(string format, params object?[] arg)
        {
            WriteFormattedMessage(string.Format(format, arg));
        }

        private void WriteFormattedMessage(string? message)
        {
            Dictionary<string, object> messageParameters = m_messageParameters.Concat(m_oneTimeMessageParameters).
                ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            foreach (var p in m_oneTimeMessageParameters)
            {
                m_oneTimeMessageParameters[p.Key] = ""; // set one-time insertions to empty
            }
            if (message != null)
            {
                messageParameters["message"] = message;
            }

            m_originalOut.Write(Smart.Format(m_messageFormat, CreateAnonymousObject(messageParameters)));
        }

        private static object CreateAnonymousObject(Dictionary<string, object> dictionary)
        {
            var anonymousObject = new ExpandoObject();

            foreach (var kvp in dictionary)
            {
                ((IDictionary<string, object>)anonymousObject)[kvp.Key] = kvp.Value;
            }

            return anonymousObject;
        }
    }
}
