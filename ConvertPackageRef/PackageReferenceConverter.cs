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
        private readonly Dictionary<string, string> _packageMap;
        private readonly HashSet<string> _isFixedMap;

        internal PackageReferenceConverter(Dictionary<string, string> packageMap, HashSet<string> isFixedMap)
        {
            _isFixedMap = isFixedMap;
            _packageMap = packageMap;
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

                var util = new ProjectConverterUtil(projectPath, _packageMap, _isFixedMap);
                util.Convert();
            }
        }
    }
}
