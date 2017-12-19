using ConvertPackageRef;
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
        internal MSBuildDocument MSBuildDocument => _projectUtil.MSBuildDocument;
        internal XNamespace Namespace => _projectUtil.MSBuildDocument.Namespace;

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
            foreach (var elem in MSBuildDocument.XPathSelectElements("PackageReference").ToList())
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
            var elem = MSBuildDocument.XPathSelectElements("TargetFrameworkVersion").Single();
            string newTf;
            switch (elem.Value.Trim())
            {
                case "v4.6":
                case "v4.6.0":
                    newTf = "net46";
                    break;
                case "v4.6.1":
                    newTf = "net461";
                    break;
                default:
                    throw new Exception($"Did not recognize the TargetFrameworkVersion: {elem.Value}");
            }
            var tf = new XElement(Namespace.GetName("TargetFramework"), newTf);
            elem.AddAfterSelf(tf);
            elem.Remove();
        }
    }
}
