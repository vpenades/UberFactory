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
            BuildContext buildSettings,
            Guid rootId,
            Guid[] nodeIds,
            Func<Guid, SDK.ContentObject> instanceFunc,
            Func<Type, SDK.ContentObject> settingsFunc,
            Func<Guid, IPropertyProvider> propertiesFunc,
            SDK.IMonitorContext monitor = null
            )
        {            
            if (buildSettings == null) throw new ArgumentNullException(nameof(buildSettings));

            if (!buildSettings.CanBuild) throw new ArgumentException(buildSettings.CurrentError, nameof(buildSettings));

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

            var fileManager = PipelineFileManager.Create(buildSettings.SourceDirectory, buildSettings.TargetDirectory, buildSettings.IsSimulation);                       

            return new PipelineEvaluator(fileManager, rootId, nodeIds, instanceFunc, settingsFunc, propertiesFunc, monitor);
        }

        private PipelineEvaluator
            (
            PipelineFileManager fileManager,
            Guid rootId,
            Guid[] nodeIds,
            Func<Guid, SDK.ContentObject> instanceFunc,
            Func<Type, SDK.ContentObject> settingsFunc,
            Func<Guid, IPropertyProvider> propertyFunc,
            SDK.IMonitorContext monitor
            )
        {
            
            _FileManager = fileManager;

            _NodeOrder = nodeIds;
            _RootIdentifier = rootId;
            _InstanceFunc = instanceFunc;
            _SettingsFunc = settingsFunc;
            _PropertiesFunc = propertyFunc;

            _Monitor = monitor;            

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

            
        }

        #endregion

        #region data

        private readonly SingleThreadAffinity _ThreadAffinity = new SingleThreadAffinity();

        private readonly SDK.IMonitorContext _Monitor;        

        private readonly PipelineFileManager _FileManager;

        private readonly Guid _RootIdentifier;

        private readonly Guid[] _NodeOrder;        

        private readonly Func<Guid, SDK.ContentObject> _InstanceFunc;
        private readonly Func<Type, SDK.ContentObject> _SettingsFunc;
        private readonly Func<Guid, IPropertyProvider> _PropertiesFunc;

        private readonly SETTINGSINSTANCES _SettingsInstancesCache = new SETTINGSINSTANCES();        

        #endregion        

        #region API

        private void CheckCancellation()
        {
            _ThreadAffinity.Check();

            if (_Monitor != null && _Monitor.IsCancelRequested) throw new OperationCanceledException();
        }

        private SDK.IMonitorContext GetLocalProgressMonitor(Guid nodeId)
        {
            _ThreadAffinity.Check();

            if (_Monitor == null) return _Monitor;

            var idx = _NodeOrder.IndexOf(item => item == nodeId);
            if (idx < 0) return null;

            return _Monitor?.CreatePart(idx, _NodeOrder.Length);
        }

        public EvaluationResult EvaluateRoot() { return EvaluateNode(_RootIdentifier); }

        public EvaluationResult EvaluateNode(Guid nodeId)
        {
            _ThreadAffinity.Check();

            if (!_NodeOrder.Contains(nodeId)) throw new ArgumentException("Not found", nameof(nodeId));

            CheckCancellation();

            var estate = new EvaluationResult(_FileManager);
            
            var r = _EvaluateNode(estate.Logger, nodeId, false);

            estate._SetResult(r);

            return estate;
        }

        public EvaluationResult PreviewNode(Guid nodeId)
        {
            _ThreadAffinity.Check();

            if (!_NodeOrder.Contains(nodeId)) throw new ArgumentException("Not found", nameof(nodeId));

            var estate = new EvaluationResult(_FileManager);

            var r = _EvaluateNode(estate.Logger, nodeId, true);

            estate._SetResult(r);

            return estate;
        }

        private Object _EvaluateNode(SDK.ILogger logger, Guid nodeId, bool previewMode)
        {
            _ThreadAffinity.Check();

            CheckCancellation();

            logger?.LogInfo(nodeId.ToString(), "Begin Evaluation...");            

            // Get the current node being evaluated
            var nodeInst = _InstanceFunc(nodeId);
            if (nodeInst == null) return null;
            if (nodeInst is _UnknownNode) return null;            

            // Next, we retrieve the property values for this node from the DOM
            var nodeProps = _PropertiesFunc(nodeId).AsReadOnly();
            if (nodeProps == null) return null;

            // Assign values and node dependencies. Dependecies are evaluated to its values.
            nodeInst.EvaluateBindings(nodeProps, xid => _EvaluateNode(logger, xid, previewMode));

            CheckCancellation();

            // evaluate the current node            

            try
            {
                var localMonitor = GetLocalProgressMonitor(nodeId);

                if (nodeInst is SDK.ContentFilter filterInst)
                {
                    if (previewMode) return SDK.PreviewNode(filterInst, localMonitor, logger);
                    else
                    {
                        if (System.Diagnostics.Debugger.IsAttached) return SDK.DebugNode(filterInst, localMonitor, logger);
                        else return SDK.EvaluateNode(filterInst, localMonitor, logger);
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
            _ThreadAffinity.Check();

            if (t == null) return null;

            var si = _SettingsInstancesCache.FirstOrDefault(item => item.GetType() == t);

            if (si != null) return si;

            si = _SettingsFunc(t);

            _SettingsInstancesCache.Add(si);

            si.BeginProcessing(_FileManager, _GetSharedSettings); // si.EndProcessing is done at _ReleaseInstances

            return si;
        }

        #endregion        
    }

    public class EvaluationResult
    {
        #region lifecycle

        internal EvaluationResult(PipelineFileManager files) { _Files = files; }

        public void Dispose()
        {
            foreach (var byProduct in _ByProducts)
            {
                try { byProduct.Dispose(); }    // some objects might throw an "AlreadyDisposedException"
                catch { }
            }

            _ByProducts.Clear();
        }

        #endregion

        #region data

        private readonly SingleThreadAffinity _ThreadAffinity = new SingleThreadAffinity();

        private readonly PipelineFileManager _Files;

        private readonly BasicLogger _Logger = new BasicLogger();        

        private readonly HashSet<IDisposable> _ByProducts = new HashSet<IDisposable>();

        private Object _Result;

        #endregion

        #region properties

        public PipelineFileManager FileManager => _Files;

        public BasicLogger Logger => _Logger;

        public Object Result => _Result;

        public IPreviewResult PreviewResult
        {
            get
            {
                if (_Result is _MemoryExportContext expDict) return expDict;

                if (_Result is IConvertible convertible)
                {
                    var text = convertible.ToString();
                    var data = Encoding.UTF8.GetBytes(text);

                    var dict = _MemoryExportContext.Create("preview.txt");

                    dict.WriteAllBytes(data);

                    return dict;
                }

                return _Result as IPreviewResult;
            }
        }

        #endregion

        #region API        

        internal void _SetResult(Object result)
        {
            _ThreadAffinity.Check();

            _Result = result;
        }

        internal void _AddByProduct(Object instance)
        {
            _ThreadAffinity.Check();

            if (instance is IDisposable disposable)
            {
                _ByProducts.Add(disposable);
            }

        }

        #endregion
    }
}
