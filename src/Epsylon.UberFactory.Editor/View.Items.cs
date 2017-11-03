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
        internal sealed class NULL_Type { private NULL_Type() { } }



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

            public ProjectDOM.Settings Source => _Source;

            public Project ParentProject => _Parent;

            public String InferredTitle => _PipelineView?._PipelineEvaluator.InferredTitle;

            public String DisplayTitle => "Task " + Title;            

            public String Title
            {
                get { return _Source.ClassName; }
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

            public ProjectDOM.Settings GetSettings(Type t)
            {
                return _Parent.GetSharedSettings(t);
            }
            
            public Type GetRootOutputType() { return null; }

            /// <summary>
            /// Template Available return types
            /// </summary>
            public IEnumerable<Type> AvailableReturnTypes => Enumerable.Empty<Type>();

            /// <summary>
            /// Template current return type
            /// </summary>
            public Type ActiveReturnType { get { return null; } set { } }

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

            public String           InferredTitle   => _PipelineView?._PipelineEvaluator.InferredTitle;

            public String           DisplayTitle    => "Task " + Title;

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

            /// <summary>
            /// Template Available return types
            /// </summary>
            public IEnumerable<Type> AvailableReturnTypes=> Enumerable.Empty<Type>();

            /// <summary>
            /// Template current return type
            /// </summary>
            public Type ActiveReturnType { get { return null; } set { } }

            #endregion

            #region API - Pipeline services

            public Factory.Collection GetPluginManager() { return _Parent._Plugins; }

            public Evaluation.BuildContext GetBuildSettings() { return _Parent.GetBuildSettings(); }            

            public ProjectDOM.Settings GetSettings(Type t)
            {
                return _Parent.GetSharedSettings(t);
            }            

            public Type GetRootOutputType() { return null; }

            #endregion
        }        

    }
}
