using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.UberFactory.Evaluation
{
    using SETTINGSINSTANCES = HashSet<SDK.ContentObject>;

    public class PipelineEvaluator : IDisposable
    {
        #region lifecycle

        public static PipelineEvaluator Create
            (
            SDK.IMonitorContext monitor,
            PipelineFileManager fileManager,
            Guid rootId,
            Guid[] nodeIds,
            Func<Guid, SDK.ContentObject> instanceFunc,
            Func<Type, SDK.ContentObject> settingsFunc,
            Func<Guid, IPropertyProvider> propertiesFunc            
            )
        {
            if (monitor == null) throw new ArgumentNullException(nameof(monitor));
            if (fileManager == null) throw new ArgumentNullException(nameof(fileManager));

            if (nodeIds == null) throw new ArgumentNullException(nameof(nodeIds));
            if (!nodeIds.Contains(rootId)) throw new ArgumentNullException(nameof(rootId));

            if (instanceFunc == null) throw new ArgumentNullException(nameof(instanceFunc));
            if (settingsFunc == null) throw new ArgumentNullException(nameof(settingsFunc));
            if (propertiesFunc == null) throw new ArgumentNullException(nameof(propertiesFunc));

            foreach(var id in nodeIds)
            {
                if (id == Guid.Empty) throw new ArgumentException("Empty Guid found in collection",nameof(nodeIds));

                var instance = instanceFunc(id);
                if (instance == null || instance is _UnknownNode) throw new KeyNotFoundException($"Instance {id} not found");

                var properties = propertiesFunc(id);
                if (properties == null) throw new KeyNotFoundException($"Properties {id} not found");
            }

            return new PipelineEvaluator(monitor, fileManager, rootId, nodeIds, instanceFunc, settingsFunc, propertiesFunc);
        }

        private PipelineEvaluator
            (SDK.IMonitorContext monitor,
            PipelineFileManager fileManager,
            Guid rootId,
            Guid[] nodeIds,
            Func<Guid, SDK.ContentObject> instanceFunc,
            Func<Type, SDK.ContentObject> settingsFunc,
            Func<Guid, IPropertyProvider> propertyFunc            
            )
        {
            _Monitor = monitor;
            _FileManager = fileManager;

            _NodeOrder = nodeIds;
            _RootIdentifier = rootId;
            _InstanceFunc = instanceFunc;
            _SettingsFunc = settingsFunc;
            _PropertiesFunc = propertyFunc;           

            _AcquireInstances();            
        }

        public void Dispose() { _ReleaseInstances(); }

        private void _AcquireInstances()
        {
            _SettingsInstancesCache.Clear();

            foreach (var id in _NodeOrder)
            {
                var instance = _InstanceFunc(id);

                instance.BeginProcessing(_FileManager, _GetSharedSettings);
            }
        }

        private void _ReleaseInstances()
        {
            foreach (var id in _NodeOrder)
            {
                var instance = _InstanceFunc(id);

                instance.EndProcessing();
            }

            foreach(var si in _SettingsInstancesCache)
            {
                si.EndProcessing();
            }

            _SettingsInstancesCache.Clear();

            foreach(var byProduct in _ByProducts)
            {
                try { byProduct.Dispose(); }    // some objects might throw an "AlreadyDisposedException"
                catch { }
            }

            _ByProducts.Clear();
        }

        #endregion

        #region data

        private readonly SDK.IMonitorContext _Monitor;
        private readonly PipelineFileManager _FileManager;

        private readonly Guid _RootIdentifier;

        private readonly Guid[] _NodeOrder;        

        private readonly Func<Guid, SDK.ContentObject> _InstanceFunc;
        private readonly Func<Type, SDK.ContentObject> _SettingsFunc;
        private readonly Func<Guid, IPropertyProvider> _PropertiesFunc;

        private readonly SETTINGSINSTANCES _SettingsInstancesCache = new SETTINGSINSTANCES();

        private readonly HashSet<IDisposable> _ByProducts = new HashSet<IDisposable>();

        #endregion

        #region properties

        public PipelineFileManager FileManager => _FileManager;

        #endregion

        #region API

        public Object EvaluateRoot()
        {            
            if (_Monitor.IsCancelRequested) return null;

            return EvaluateNode(_RootIdentifier);
        }

        public Object EvaluateNode(Guid nodeId)
        {            
            if (!_NodeOrder.Contains(nodeId)) throw new ArgumentException("Not found", nameof(nodeId));

            if (_Monitor.IsCancelRequested) return null;                        

            var result = _EvaluateNode(nodeId, false);

            if (result is IDisposable disposable) _ByProducts.Add(disposable);

            return result;
        }

        public IPreviewResult PreviewNode(Guid nodeId)
        {
            if (!_NodeOrder.Contains(nodeId)) throw new ArgumentException("Not found", nameof(nodeId));            

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
            var nodeInst = _InstanceFunc(nodeId);
            if (nodeInst == null) return null;
            if (nodeInst is _UnknownNode) return null;            

            // Next, we retrieve the property values for this node from the DOM
            var nodeProps = _PropertiesFunc(nodeId).AsReadOnly();
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

            si = _SettingsFunc(t);

            _SettingsInstancesCache.Add(si);

            si.BeginProcessing(_FileManager, _GetSharedSettings);

            return si;
        }

        #endregion        
    }   
}
