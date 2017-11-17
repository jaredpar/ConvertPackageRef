using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ConvertPackageRef
{
    public static class RepoUtil
    {
        public static string PackageNameToKey(string name) => $"{name.Replace(".", "")}Version";

        public static string PackageNameToFixedKey(string name) => $"{name.Replace(".", "")}FixedVersion";

        /// <summary>
        /// Read out the package versions from the repo.  The keys will be in NameVersion / NameFixedVersion format.
        /// </summary>
        public static Dictionary<string, string> ReadPackageVersionMap(string repoPath)
        {
            var packageVersionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ReadPackageVersionMapCore(packageVersionMap, Path.Combine(repoPath, @"build\Targets\Packages.props"));
            ReadPackageVersionMapCore(packageVersionMap, Path.Combine(repoPath, @"build\Targets\FixedPackages.props"));
            return packageVersionMap;
        }

        public static Dictionary<string, string> ReadPackageVersionMapCore(Dictionary<string, string> packageVersionMap, string path)
        {
            var doc = XDocument.Load(path);
            var manager = new XmlNamespaceManager(new NameTable());
            manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
            var prop = doc.XPathSelectElements("//mb:PropertyGroup", manager).Single();
            foreach (var element in prop.Elements())
            {
                packageVersionMap.Add(element.Name.LocalName, element.Value.Trim());
            }

            return packageVersionMap;
        }
    }
}
