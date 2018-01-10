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

namespace QuickChange
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            var converter = new Converter(@"e:\code\roslyn\Roslyn.sln");
            converter.ConvertProjectFiles(@"e:\code\roslyn\Roslyn.sln");
            converter.ConvertSolutionEntries(@"e:\code\roslyn\Roslyn.sln");
            converter.ConvertSolutionEntries(@"e:\code\roslyn\Compilers.sln");
        }
    }
}
