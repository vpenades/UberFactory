using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberFactory
{
    using Serialization;
    using System.Linq;
    using System.Xml.Linq;

    public static class ConfigDOM
    {
        private static readonly Version _CurrentVersion = new Version(1, 0);

        public static Version CurrentVersion => _CurrentVersion;

        internal static ObjectBase _Factory(Unknown unk)
        {
            var name = unk.ClassName;            

            if (name == typeof(Config).Name) return new Config(unk, _Factory);
            if (name == typeof(PluginReference).Name) return new PluginReference(unk, _Factory);            

            return unk;
        }

        public static Config LoadConfigFrom(string filePath)
        {
            var body = System.IO.File.ReadAllText(filePath);

            return ParseConfig(body);
        }

        public static Config ParseConfig(string projectBody)
        {            
            if (string.IsNullOrWhiteSpace(projectBody)) return new Config();

            var root = XElement.Parse(projectBody);
            return _ParseConfig(root);
        }

        private static Config _ParseConfig(XElement root)
        {
            var ver = root.Attribute("Version").Value;

            if (!Version.TryParse(ver, out Version docVer)) throw new System.IO.FileLoadException();
            if (docVer > CurrentVersion) throw new System.IO.FileLoadException("Config Version " + docVer + " not supported");

            return Unknown.ParseXml(root, _Factory) as Config;
        }

        public partial class Config : ObjectBase
        {
            #region lifecycle

            internal Config()
            {
                Attributes["Version"] = CurrentVersion.ToString();
            }

            internal Config(Unknown s, ObjectFactoryDelegate factory) : base(s, factory)
            {
                Attributes["Version"] = CurrentVersion.ToString();
            }

            #endregion

            #region serialization                

            public String GetBody(bool useCurrentTime = true)
            {
                var root = new Unknown(this).ToXml();
                return root.ToString(SaveOptions.OmitDuplicateNamespaces);
            }

            public void SaveTo(string filePath)
            {
                var body = GetBody();
                System.IO.File.WriteAllText(filePath, body);
            }

            #endregion

            #region properties

            public IReadOnlyList<PluginReference> PackageReferences => GetLogicalChildren<PluginReference>().ToArray();

            #endregion            
        }
        
        public partial class PluginReference : ObjectBase
        {
            private const String PROP_INCLUDE = "Include";
            private const String PROP_VERSION = "Version";

            #region lifecycle            

            internal PluginReference(Unknown s, ObjectFactoryDelegate factory) : base(s, factory) { }            

            #endregion

            #region properties           

            public String IncludePath
            {
                get { return Properties.GetValue(PROP_INCLUDE, null); }
                set { Properties.SetValue(PROP_INCLUDE, value); }
            }            

            #endregion            
        }
    }
}
