using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ConvertPackageRef
{
    public sealed class ProjectUtil
    {
        public XmlNamespaceManager Manager { get; }
        public string FilePath { get; }
        public XDocument Document { get; }

        public XNamespace Namespace => SharedUtil.MSBuildNamespace;
        public string ProjectName => Path.GetFileNameWithoutExtension(FilePath);
        public bool IsNewSdk => TryGetTargetFramework(out _) || IsMultiTargeted;
        public bool IsSharedProject => Path.GetExtension(FilePath) == ".shproj";
        public bool IsExe => TryGetOutputType(out var outputType) && StringComparer.OrdinalIgnoreCase.Equals("Exe", outputType);
        public bool IsLibrary => TryGetOutputType(out var outputType) && StringComparer.OrdinalIgnoreCase.Equals("Library", outputType);
        public bool IsMultiTargeted => Document.XPathSelectElements("//mb:TargetFrameworks", Manager).FirstOrDefault() != null;

        public bool IsPclProject
        {
            get
            {
                var elem = Document.XPathSelectElements("//mb:TargetFrameworkIdentifier", Manager).FirstOrDefault();
                if (elem == null)
                {
                    return false;
                }

                return StringComparer.OrdinalIgnoreCase.Equals(elem.Value, ".NETPortable");
            }
        }

        public bool IsDesktopProject
        {
            get
            {
                if (IsPclProject)
                {
                    return false;
                }

                var elem = Document.XPathSelectElements("//mb:TargetFrameworkVersion", Manager).FirstOrDefault();
                return elem != null;
            }
        }

        public ProjectUtil(string filePath)
        {
            Manager = new XmlNamespaceManager(new NameTable());
            Manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
            FilePath = filePath;
            Document = XDocument.Load(FilePath);
        }

        public XElement FindImportWithName(string fileName)
        {
            var all = Document.XPathSelectElements("//mb:Import", Manager);
            foreach (var e in all)
            {
                var project = e.Attribute("Project");
                if (project == null)
                {
                    continue;
                }

                var name = Path.GetFileName(project.Value);
                if (StringComparer.OrdinalIgnoreCase.Equals(name, fileName))
                {
                    return e;
                }
            }

            throw new Exception($"Unable to find Import for {fileName}");
        }

        /// <summary>
        /// Find the last ItemGroup node that has Reference elements in it.
        /// </summary>
        public XElement FindReferenceItemGroup() => FindItemGroupWithElement("Reference");

        public XElement FindPackageReferenceItemGroup() => FindItemGroupWithElement("PackageReference");

        public XElement FindItemGroupWithElement(string localName)
        {
            XElement e = null;

            var groups = Document.XPathSelectElements("//mb:ItemGroup", Manager);
            foreach (var group in groups)
            {
                if (group.Elements().Where(x => x.Name.LocalName == localName).Any())
                {
                    e = group;
                }
            }

            return e;
        }

        public bool TryGetTargetFramework(out string targetFramework) => TryGetElementValue("TargetFramework", out targetFramework);

        public bool TryGetOutputType(out string outputType) => TryGetElementValue("OutputType", out outputType);

        private bool TryGetElementValue(string elementName, out string value)
        {
            var element = Document.XPathSelectElements($"//mb:{elementName}", Manager).FirstOrDefault();
            if (element!= null)
            {
                value = element.Value.Trim();
                return true;
            }

            value = null;
            return false;
        }
    }
}
