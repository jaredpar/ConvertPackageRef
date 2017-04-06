using BuildBoss;
using RepoUtil;
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
    internal sealed class PackageReferenceConverter
    {
        private readonly XmlNamespaceManager _manager;
        private readonly HashSet<string> _isFixedMap;

        internal PackageReferenceConverter(HashSet<string> isFixedMap)
        {
            _manager = new XmlNamespaceManager(new NameTable());
            _manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
            _isFixedMap = isFixedMap;
        }

        internal void ConvertSolution(string solutionPath)
        {
            var dir = Path.GetDirectoryName(solutionPath);
            foreach (var project in SolutionUtil.ParseProjects(solutionPath))
            {
                var projectPath = Path.Combine(dir, project.RelativeFilePath);
                if (!File.Exists(projectPath))
                {
                    continue;
                }

                var util = new ProjectUtil(projectPath, _isFixedMap);
                util.Convert();
            }
        }
    }
}
