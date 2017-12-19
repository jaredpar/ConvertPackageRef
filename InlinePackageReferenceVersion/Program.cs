using ConvertPackageRef;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace InlinePackageReferenceVersion
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            foreach (var solution in args)
            {
                foreach (var project in SolutionUtil.ParseProjects(solution))
                {
                    if (project.ProjectType != ProjectFileType.CSharp && project.ProjectType != ProjectFileType.Basic)
                    {
                        continue;
                    }

                    var filePath = Path.Combine(Path.GetDirectoryName(solution), project.RelativeFilePath);
                    Convert(filePath, new Dictionary<string, string>());
                }
            }
        }

        internal static void Convert(string projectFilePath, Dictionary<string, string> packageMap)
        {
            var doc = XDocument.Load(projectFilePath);
            var msbuildDoc = new MSBuildDocument(doc);
            var tf = msbuildDoc.XPathSelectElements("TargetFramework").FirstOrDefault();
            if (tf != null)
            {
                return;
            }

            Console.WriteLine($"Processing {Path.GetFileName(projectFilePath)}");

            var versionName = msbuildDoc.Namespace.GetName("Version");
            foreach (var packageRef in msbuildDoc.XPathSelectElements("PackageReference"))
            {
                var version = packageRef.Element(versionName);
                var value = version.Value.Trim();
                if (value.StartsWith("$("))
                {
                    value = value.Substring(2, value.Length - 3);
                    var rawVersion = packageMap[value];
                    version.Value = rawVersion;
                }
            }

            doc.Save(projectFilePath);
        }
    }
}
