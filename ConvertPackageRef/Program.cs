﻿using BuildBoss;
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
            var baseDir = @"e:\code\roslyn";
            var solutions = new[]
            {
                @"Roslyn.sln",
                @"src\Samples\Samples.sln",
                @"src\Setup\Templates\Templates.sln",
                @"src\Setup\DevDivInsertionFiles\DevDivInsertionFiles.sln",
            };
            var converter = new PackageReferenceConverter(ParseFixedSet());
            foreach (var solution in solutions)
            {
                var path = Path.Combine(baseDir, solution);
                converter.ConvertSolution(path);
            }
        }

        private static HashSet<string> ParseFixedSet()
        {
            var doc = XDocument.Load(@"e:\code\roslyn\build\Targets\FixedPackages.props");
            var manager = new XmlNamespaceManager(new NameTable());
            manager.AddNamespace("mb", SharedUtil.MSBuildNamespaceUriRaw);
            var group = doc.XPathSelectElements("//mb:PropertyGroup", manager).Single();

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in group.Elements())
            {
                var key = $"{e.Name.LocalName}:{e.Value}";
                set.Add(key);
            }

            return set;
        }
    }
}
