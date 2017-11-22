using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Epsylon.UberFactory
{
    public static partial class ProjectVIEW
    {
        public class SettingsView : BindableBase, IPipelineViewServices
        {
            #region lifecycle

            public static SettingsView Create(Project d, ProjectDOM.Settings c)
            {
                if (d == null || c == null) return null;

                return new SettingsView(d, c);
            }

            private SettingsView(Project d, ProjectDOM.Settings c)
            {
                _Parent = d;
                _Source = c;
            }

            #endregion

            #region data

            private readonly Project _Parent;
            private readonly ProjectDOM.Settings _Source;

            private Pipeline _PipelineView;

            #endregion

            #region properties

            public ProjectDOM.Settings  Source          => _Source;

            public Project              ParentProject   => _Parent;

            public String               InferredTitle   => Pipeline?._PipelineInstance.InferredTitle;

            public String               DisplayTitle    => $"Settings {Title}";

            public String Title
            {
                get { return InferredTitle; }
                set { }
            }

            public Pipeline Pipeline
            {
                get
                {
                    if (_PipelineView == null) _PipelineView = Pipeline.Create(this, _Source.Pipeline);
                    return _PipelineView;
                }
            }            

            #endregion

            #region API - Pipeline services

            public Factory.Collection GetPluginManager() { return _Parent._Plugins; }

            public Evaluation.BuildContext GetBuildSettings() { return _Parent.GetBuildSettings(); }            

            public ProjectDOM.Settings GetSharedSettings(Type t) { return _Parent.GetSharedSettings(t); }
            
            public Type GetRootOutputType() { return null; }                       

            #endregion
        }


        public class Task : BindableBase, IPipelineViewServices
        {
            #region lifecycle

            public static Task Create(Project d, ProjectDOM.Task c)
            {
                if (d == null || c == null) return null;

                return new Task(d, c);
            }

            private Task(Project d, ProjectDOM.Task c)
            {
                _Parent = d;
                _Source = c;
            }

            #endregion

            #region data

            private readonly Project _Parent;
            private readonly ProjectDOM.Task _Source;

            private Pipeline _PipelineView;

            #endregion

            #region properties

            public ProjectDOM.Task  Source          => _Source;

            public Project          ParentProject   => _Parent;            

            public String           InferredTitle   => Pipeline?._PipelineInstance.InferredTitle;

            public String           DisplayTitle    => $"Task {Title}";

            public Boolean Enabled { get { return _Source.Enabled; } set { _Source.Enabled = value; } }

            public String Title
            {
                get { return _Source.Title; }
                set { _Source.Title = value; RaiseChanged(nameof(Title), nameof(DisplayTitle)); }
            }

            public Pipeline Pipeline
            {
                get
                {
                    if (_PipelineView == null) _PipelineView = Pipeline.Create(this, _Source.Pipeline);
                    return _PipelineView;
                }
            }
            
            public IEnumerable<string> ProcessedInputFiles => _Parent.GetProcessingResultsFor(this)?.InputFiles;

            public IEnumerable<string> ProcessedOutputFiles => _Parent.GetProcessingResultsFor(this)?.OutputFiles;

            #endregion

            #region API

            public Factory.Collection GetPluginManager() { return _Parent._Plugins; }

            public Evaluation.BuildContext GetBuildSettings() { return _Parent.GetBuildSettings(); }            

            public ProjectDOM.Settings GetSharedSettings(Type t) { return _Parent.GetSharedSettings(t); }            

            public Type GetRootOutputType() { return null; }

            #endregion
        }        

    }
}
