using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Epsylon.UberFactory.Evaluation
{
    /// <summary>
    /// holds the persistent state of a given pipeline
    /// </summary>
    public class PipelineState : INotifyPropertyChanged
    {
        private PipelineState(ProjectDOM.Pipeline pipeline) { _Pipeline = pipeline; }

        private readonly ProjectDOM.Pipeline _Pipeline;
        
        private struct _FileInfo
        {            
            public DateTime Time;
            public long Length;
        }

        private readonly Dictionary<string, _FileInfo> _InputFiles = new Dictionary<string, _FileInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, _FileInfo> _OutputFiles = new Dictionary<string, _FileInfo>(StringComparer.OrdinalIgnoreCase);

        public event PropertyChangedEventHandler PropertyChanged;

        public class Dictionary : IReadOnlyDictionary<ProjectDOM.Pipeline,PipelineState>
        {
            #region data

            private readonly Dictionary<ProjectDOM.Pipeline, PipelineState> _InternalDict = new Dictionary<ProjectDOM.Pipeline, PipelineState>(ReferenceComparer<ProjectDOM.Pipeline>.GetInstance());

            #endregion

            #region API

            public PipelineState this[ProjectDOM.Pipeline key] => _InternalDict[key];

            public IEnumerable<ProjectDOM.Pipeline> Keys => _InternalDict.Keys;

            public IEnumerable<PipelineState> Values => _InternalDict.Values;

            public int Count => _InternalDict.Count;

            public bool ContainsKey(ProjectDOM.Pipeline key) { return _InternalDict.ContainsKey(key); }            

            public bool TryGetValue(ProjectDOM.Pipeline key, out PipelineState value) { return _InternalDict.TryGetValue(key, out value); }

            public IEnumerator<KeyValuePair<ProjectDOM.Pipeline, PipelineState>> GetEnumerator() { return _InternalDict.GetEnumerator(); }

            IEnumerator IEnumerable.GetEnumerator() { return _InternalDict.GetEnumerator(); }

            public void Clear() { _InternalDict.Clear(); }

            public void Update(IEnumerable<ProjectDOM.Pipeline> items)
            {
                var toInsert = items
                    .Where(item => !_InternalDict.ContainsKey(item))
                    .ToArray();

                var toRemove = _InternalDict
                    .Keys
                    .Where(item => !items.Contains(item, ReferenceComparer<ProjectDOM.Pipeline>.GetInstance()))
                    .ToArray();

                foreach (var tr in toRemove) _InternalDict.Remove(tr);
                foreach (var ta in toInsert) _InternalDict.Add(ta, new PipelineState(ta));
            }

            public void Update(IReadOnlyDictionary<Guid, PipelineFileManager> buildResults)
            {

            }

            #endregion
        }
    }
}
