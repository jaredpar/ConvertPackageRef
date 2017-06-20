using ConvertPackageRef;
using ConvertPackageRef.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ConvertFromLegacy
{
    internal sealed class ConvertDesktopUtil
    {
        private readonly ProjectUtil _projectUtil;
        private readonly Dictionary<string, string> _packageMap;

        internal XDocument Document => _projectUtil.Document;
        internal XmlNamespaceManager Manager => _projectUtil.Manager;
        internal XNamespace Namespace => _projectUtil.Namespace;

        internal ConvertDesktopUtil(string filePath, Dictionary<string, string> packageMap)
        {
            _projectUtil = new ProjectUtil(filePath);
            _packageMap = packageMap;
        }

        internal void Convert()
        {
            if (!_projectUtil.IsDesktopProject)
            {
                return;
            }

            Console.WriteLine($"Converting {_projectUtil.ProjectName}");
            UpdateTargetFramework();
            UpdatePackageReference();
            UpdateImportSettings();
            Document.Save(_projectUtil.FilePath);
        }

        private void UpdateImportSettings()
        {
            var propsElement = _projectUtil.FindImportWithName("Settings.props");
            var project = propsElement.Attribute("Project");
            project.Value = Path.Combine(Path.GetDirectoryName(project.Value), "SettingsSdk.props");
        }

        private void UpdatePackageReference()
        {
            var versionName = Namespace.GetName("Version");
            var comp = StringComparer.OrdinalIgnoreCase;
            foreach (var elem in Document.XPathSelectElements("//mb:PackageReference", Manager).ToList())
            {
                var versionElement = elem.Element(versionName);
                var version = (versionElement != null ? versionElement.Value : elem.Attribute("Version").Value).Trim();
                var name = elem.Attribute("Include").Value.Trim();
                var newElem = new XElement(elem.Name);
                newElem.Add(new XAttribute("Include", name));
                newElem.Add(new XAttribute("Version", version));
                elem.AddAfterSelf(newElem);
                elem.Remove();
            }
        }

        private void UpdateTargetFramework()
        {
            var elem = Document.XPathSelectElements("//mb:TargetFrameworkVersion", Manager).Single();
            var tf = new XElement(Namespace.GetName("TargetFramework"), "net46");
            elem.AddAfterSelf(tf);
            elem.Remove();
        }
    }
}
