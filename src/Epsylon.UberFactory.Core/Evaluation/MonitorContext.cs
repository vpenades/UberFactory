using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.Logging;

namespace Epsylon.UberFactory.Evaluation
{
    using MSLOGGER = ILoggerFactory;

    public sealed class MonitorContext : SDK.IMonitorContext
    {
        #region lifecycle

        private MonitorContext() { }

        public static MonitorContext CreateNull()
        {
            return new MonitorContext()
            {
                _Cancelator = System.Threading.CancellationToken.None,
                _Progress = null,
                _Logger = null
            };
        }

        public static MonitorContext Create(MSLOGGER logger, System.Threading.CancellationToken cancelToken, IProgress<float> progressAgent)
        {
            return new MonitorContext()
            {
                _Cancelator = cancelToken,
                _Progress = progressAgent,
                _Logger = logger
            };
        }

        public SDK.IMonitorContext GetProgressPart(int part, int total)
        {
            return new MonitorContext()
            {
                _Cancelator = this._Cancelator,
                _Progress = this._Progress.CreatePart(part, total),
                _Logger = this._Logger
            };
        }

        #endregion

        #region data        

        private System.Threading.CancellationToken _Cancelator;
        private IProgress<float> _Progress;
        private MSLOGGER _Logger;

        #endregion

        #region API

        public void SetLogger(MSLOGGER logger) { _Logger = logger; }

        public bool IsCancelRequested => _Cancelator.IsCancellationRequested;

        public void Report(float value)
        {
            if (_Progress == null) return;
            _Progress.Report(value.Clamp(0, 1));
        }

        private ILogger CreateLogger(string name)
        {
            return _Logger == null ? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance : _Logger.CreateLogger(name);
        }

        public void LogTrace(string name, string message) { CreateLogger(name).LogTrace(message); }
        public void LogDebug(string name, string message) { CreateLogger(name).LogDebug(message); }
        public void LogInfo(string name, string message) { CreateLogger(name).LogInformation(message); }
        public void LogWarning(string name, string message) { CreateLogger(name).LogWarning(message); }
        public void LogError(string name, string message) { CreateLogger(name).LogError(message); }
        public void LogCritical(string name, string message) { CreateLogger(name).LogCritical(message); }

        #endregion
    }
}
