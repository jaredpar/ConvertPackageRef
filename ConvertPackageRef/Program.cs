using BuildBoss;
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
    public static class Program
    {
        public static void Main(string[] args)
        {
            var baseDir = @"e:\code\roslyn-internal";
            var solutions = new[]
            {
                @"Roslyn.sln",
                @"Closed\Tools\Source\Test\RoslynBuildAndTest\RoslynBuildAndTest.sln",
                @"Closed\Tools\Source\RoslynInsertionTool\RoslynInsertionTool.sln"
            };

            var openDir = Path.Combine(baseDir, "Open");
            var converter = new PackageReferenceConverter(ParsePackages(openDir), ParseFixedSet(openDir));
            foreach (var solution in solutions)
            {
                var path = Path.Combine(baseDir, solution);
                converter.ConvertSolution(path);
            }
        }

        private static Dictionary<string, string> ParsePackages(string baseDir)
        {
            var doc = XDocument.Load(Path.Combine(baseDir, @"build\Targets\Packages.props"));
            var list = ParsePackages(doc);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var e in list)
            {
                map[e.name] = e.version;
            }

            return map;
        }

        private static HashSet<string> ParseFixedSet(string baseDir)
        {
            var doc = XDocument.Load(Path.Combine(baseDir, @"build\Targets\FixedPackages.props"));
            var list = ParsePackages(doc);
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in list)
            {
                var key = $"{e.name}:{e.version}";
                set.Add(key);
            }

            return set;
        }

        private static List<(string name, string version)> ParsePackages(XDocument doc)
        {
            var manager = new XmlNamespaceManager(new NameTable());
            manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
            var group = doc.XPathSelectElements("//mb:PropertyGroup", manager).Single();

            var list = new List<(string name, string version)>();
            foreach (var e in group.Elements())
            {
                list.Add((e.Name.LocalName, e.Value.Trim()));
            }

            return list;
        }
    }
}
