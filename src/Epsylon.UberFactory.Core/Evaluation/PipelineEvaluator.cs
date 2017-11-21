using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Evaluation
{
    using SETTINGSINSTANCES = HashSet<SDK.ContentObject>;



    public class PipelineEvaluator
    {
        #region lifecycle

        public static PipelineEvaluator Create
            (
            SDK.IMonitorContext monitor,
            SDK.IBuildContext bsettings,
            Guid rootId,
            Guid[] nodeIds,
            Func<Guid, SDK.ContentObject> instanceFunc,
            Func<Type, SDK.ContentObject> settingsFunc,
            Func<Guid, IPropertyProvider> propertiesFunc
            )
        {
            if (monitor == null) throw new ArgumentNullException(nameof(monitor));
            if (bsettings == null) throw new ArgumentNullException(nameof(bsettings));

            if (nodeIds == null) throw new ArgumentNullException(nameof(nodeIds));
            if (!nodeIds.Contains(rootId)) throw new ArgumentNullException(nameof(rootId));

            if (instanceFunc == null) throw new ArgumentNullException(nameof(instanceFunc));
            if (settingsFunc == null) throw new ArgumentNullException(nameof(settingsFunc));
            if (propertiesFunc == null) throw new ArgumentNullException(nameof(propertiesFunc));

            bool allInstancesReady = !nodeIds
                .Select(item => instanceFunc(item))
                .OfType<_UnknownNode>()
                .Any();

            if (!allInstancesReady) throw new ArgumentException("Some filters couldn't be instantiated.");

            var tracker = new _TaskFileIOTracker(bsettings);

            return new PipelineEvaluator(monitor, tracker, rootId, nodeIds, instanceFunc, settingsFunc, propertiesFunc);
        }

        private PipelineEvaluator
            (SDK.IMonitorContext monitor,
            _TaskFileIOTracker tracker,
            Guid rootId,
            Guid[] nodeIds,
            Func<Guid, SDK.ContentObject> instanceFunc,
            Func<Type, SDK.ContentObject> settingsFunc,
            Func<Guid, IPropertyProvider> propertyFunc
            )
        {
            _Monitor = monitor;
            _FileIOTracker = tracker;

            _NodeOrder = nodeIds;
            _RootIdentifier = rootId;
            _NodeInstanceFunc = instanceFunc;
            _SettingsInstanceFunc = settingsFunc;
            _NodePropertiesFunc = propertyFunc;

            _AcquireInstances();            
        }

        // public void Dispose() { _ReleaseInstances(); }

        private void _AcquireInstances()
        {
            _SettingsInstancesCache.Clear();

            foreach (var id in _NodeOrder)
            {
                var instance = _NodeInstanceFunc(id);

                instance.BeginProcessing(_GetSharedSettings, _FileIOTracker);
            }
        }

        private void _ReleaseInstances()
        {
            foreach (var id in _NodeOrder)
            {
                var instance = _NodeInstanceFunc(id);

                instance.EndProcessing();
            }

            foreach(var si in _SettingsInstancesCache)
            {
                si.EndProcessing();
            }

            _SettingsInstancesCache.Clear();
        }

        #endregion

        #region data

        private readonly SDK.IMonitorContext _Monitor;
        private readonly _TaskFileIOTracker _FileIOTracker;

        private readonly Guid _RootIdentifier;

        private readonly Guid[] _NodeOrder;        

        private readonly Func<Guid, SDK.ContentObject> _NodeInstanceFunc;
        private readonly Func<Type, SDK.ContentObject> _SettingsInstanceFunc;
        private readonly Func<Guid, IPropertyProvider> _NodePropertiesFunc;

        private readonly SETTINGSINSTANCES _SettingsInstancesCache = new SETTINGSINSTANCES();

        #endregion

        #region API

        public Object EvaluateRoot()
        {            
            if (_Monitor.IsCancelRequested) return null;            

            _FileIOTracker?.Clear();

            return EvaluateNode(_RootIdentifier);
        }

        public Object EvaluateNode(Guid nodeId)
        {            
            if (!_NodeOrder.Contains(nodeId)) throw new ArgumentException("Not found", nameof(nodeId));

            if (_Monitor.IsCancelRequested) return null;                        

            return _EvaluateNode(nodeId, false);
        }

        public IPreviewResult PreviewNode(Guid nodeId)
        {
            if (!_NodeOrder.Contains(nodeId)) throw new ArgumentException("Not found", nameof(nodeId));

            _FileIOTracker?.Clear();            

            var previewObject = _EvaluateNode( nodeId, true);

            if (previewObject is _DictionaryExportContext expDict)
            {
                return expDict;
            }

            if (previewObject is IConvertible convertible)
            {
                var text = convertible.ToString();
                var data = Encoding.UTF8.GetBytes(text);

                var dict = _DictionaryExportContext.Create("preview.txt", null);

                dict.WriteAllBytes(data);

                return dict;
            }

            return null;
        }

        private Object _EvaluateNode(Guid nodeId, bool previewMode)
        {
            if (_Monitor.IsCancelRequested) throw new OperationCanceledException();            

            // Get the current node being evaluated
            var nodeInst = _NodeInstanceFunc(nodeId);
            if (nodeInst == null) return null;
            if (nodeInst is _UnknownNode) return null;

            _FileIOTracker.RegisterAssemblyFile(nodeInst.GetType().Assembly.Location);

            // Next, we retrieve the property values for this node from the DOM
            var nodeProps = _NodePropertiesFunc(nodeId);
            if (nodeProps == null) return null;

            // Assign values and node dependencies. Dependecies are evaluated to its values.
            nodeInst.EvaluateBindings(nodeProps, xid => _EvaluateNode(xid, previewMode));

            if (_Monitor.IsCancelRequested) throw new OperationCanceledException();

            // evaluate the current node            

            try
            {
                var localMonitor = _Monitor?.GetProgressPart(_NodeOrder.IndexOf(item=> item == nodeId), _NodeOrder.Length);

                System.Diagnostics.Debug.Assert(localMonitor != null);

                if (nodeInst is SDK.ContentFilter filterInst)
                {
                    if (previewMode) return SDK.PreviewNode(filterInst, localMonitor);
                    else
                    {
                        if (System.Diagnostics.Debugger.IsAttached) return SDK.DebugNode(filterInst, localMonitor);
                        else return SDK.EvaluateNode(filterInst, localMonitor);
                    }
                }
                else if (nodeInst is SDK.ContentObject) return nodeInst;
            }
            catch (Exception ex)
            {
                throw new PluginException(ex);
            }

            throw new NotImplementedException();
        }

        private SDK.ContentObject _GetSharedSettings(Type t)
        {
            if (t == null) return null;

            var si = _SettingsInstancesCache.FirstOrDefault(item => item.GetType() == t);

            if (si != null) return si;

            si = _SettingsInstanceFunc(t);

            _SettingsInstancesCache.Add(si);

            si.BeginProcessing(_GetSharedSettings, _FileIOTracker);

            return si;
        }

        #endregion

    }

    class _TaskFileIOTracker : SDK.ITaskFileIOTracker
    {
        public _TaskFileIOTracker(SDK.IBuildContext bc)
        {

        }


        private readonly HashSet<String> _AssemblyFiles = new HashSet<String>(StringComparer.OrdinalIgnoreCase);// assembly files used by this pipeline
        private readonly HashSet<String> _InputFiles = new HashSet<String>(StringComparer.OrdinalIgnoreCase);   // files read from Source directory
        private readonly HashSet<String> _OutputFiles = new HashSet<String>(StringComparer.OrdinalIgnoreCase);  // files written to temporary directory

        public IEnumerable<String> InputFiles
        {
            get
            {
                return _InputFiles;
            }
        }

        public IEnumerable<String> OutputFiles
        {
            get
            {
                return _OutputFiles;
            }
        }

        public void Clear()
        {
            _AssemblyFiles.Clear();
            _InputFiles.Clear();
            _OutputFiles.Clear();
        }

        public void RegisterAssemblyFile(string filePath) { _AssemblyFiles.Add(filePath); }

        void SDK.ITaskFileIOTracker.RegisterInputFile(string filePath, string parentFilePath)
        {
            _InputFiles.Add(filePath);
        }

        void SDK.ITaskFileIOTracker.RegisterOutputFile(string filePath, string parentFilePath)
        {
            _OutputFiles.Add(filePath);
        }
    }
}
