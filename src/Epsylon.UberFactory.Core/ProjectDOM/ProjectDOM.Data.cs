using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;


namespace Epsylon.UberFactory
{
    using Serialization;

    // TODO: add Author data, modification date, etc

    public static partial class ProjectDOM
    {
        private static readonly Version _CurrentVersion = new Version(1, 0);

        public static Version CurrentVersion => _CurrentVersion;

        public partial class Configuration : ObjectBase
        {
            internal Configuration(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }
        }        

        public partial class Node : ObjectBase
        {
            internal Node(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }
        }

        public partial class Pipeline : ObjectBase
        {
            internal Pipeline(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }            
        }

        public abstract partial class Item : ObjectBase
        {
            internal Item(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }

            protected Item() {}
        }        

        public partial class Settings : Item
        {
            internal Settings(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }
        }

        public partial class Task : Item
        {
            internal Task(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }

            internal Task() {}
        }        

        public partial class PluginReference : Item
        {
            internal PluginReference(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }
        }

        public partial class DocumentInfo : ObjectBase
        {
            internal DocumentInfo(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }
        }

        public partial class Project : ObjectBase
        {
            #region lifecycle

            internal Project(Unknown s, ObjectFactoryDelegate factory) : base(s, factory)
            {
                Attributes["Version"] = CurrentVersion.ToString();
            }

            #endregion

            #region serialization                

            public String GetBody(bool useCurrentTime = true)
            {
                var assembly = System.Reflection.Assembly.GetEntryAssembly();
                if (assembly != null)
                {
                    this.Generator = $"{assembly.GetName().Name} {assembly.GetName().Version.ToString()}";
                }

                this.Date = useCurrentTime ? DateTime.Now : DateTime.Now.Date;

                var root = new Unknown(this).ToXml();
                return root.ToString(SaveOptions.OmitDuplicateNamespaces);
            }

            public void SaveTo(string filePath)
            {
                var body = GetBody();
                System.IO.File.WriteAllText(filePath, body);
            }            

            #endregion
        }

    }
}