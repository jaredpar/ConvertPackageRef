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
        public string FilePath { get; }
        public XDocument Document { get; }
        public MSBuildDocument MSBuildDocument { get; }
        public XNamespace Namespace => MSBuildDocument.Namespace;
        public string ProjectName => Path.GetFileNameWithoutExtension(FilePath);
        public bool IsNewSdk => TryGetTargetFramework(out _) || IsMultiTargeted;
        public bool IsSharedProject => Path.GetExtension(FilePath) == ".shproj";
        public bool IsExe => TryGetOutputType(out var outputType) && StringComparer.OrdinalIgnoreCase.Equals("Exe", outputType);
        public bool IsLibrary => TryGetOutputType(out var outputType) && StringComparer.OrdinalIgnoreCase.Equals("Library", outputType);
        public bool IsMultiTargeted => MSBuildDocument.XPathSelectElements("TargetFrameworks").FirstOrDefault() != null;
        public string AssemblyName => $"{AssemblyNameWithoutExtension}." + (IsExe ? "exe" : "dll");

        public string AssemblyNameWithoutExtension
        {
            get
            {
                var elem = MSBuildDocument.XPathSelectElements("AssemblyName").FirstOrDefault();
                return elem?.Value.ToString() ?? Path.GetFileNameWithoutExtension(FilePath);
            }
        }

        public bool IsPclProject
        {
            get
            {
                var elem = MSBuildDocument.XPathSelectElements("TargetFrameworkIdentifier").FirstOrDefault();
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

                var elem = MSBuildDocument.XPathSelectElements("TargetFrameworkVersion").FirstOrDefault();
                return elem != null;
            }
        }

        public ProjectUtil(string filePath)
        {
            FilePath = filePath;
            Document = XDocument.Load(FilePath);
            MSBuildDocument = new MSBuildDocument(Document);
        }

        public XElement GetOrCreateMainPropertyGroup()
        {
            var element = FindMainPropertyGroup();
            if (element == null)
            {
                element = new XElement(Namespace.GetName("PropertyGroup"));
                Document.Root.AddFirst(element);
            }

            return element;
        }

        public XElement FindImportWithName(string fileName)
        {
            var all = MSBuildDocument.XPathSelectElements("Import");
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

        public XElement FindMainPropertyGroup() => Document.Root.Elements(Namespace.GetName("PropertyGroup")).FirstOrDefault();

        public XElement FindItemGroupWithElement(string localName)
        {
            XElement e = null;

            var groups = MSBuildDocument.XPathSelectElements("ItemGroup");
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
            var element = MSBuildDocument.XPathSelectElements(elementName).FirstOrDefault();
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
