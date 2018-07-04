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
            converter.ConvertSolutionEntries(@"e:\code\roslyn\SourceBuild.sln");
            converter.ConvertSwrFile(@"E:\code\roslyn\src\Setup\DevDivVsix\CompilersPackage\Microsoft.CodeAnalysis.Compilers.swr");
            converter.ConvertNuSpecs(@"E:\code\roslyn\src\NuGet");
            converter.ConvertSignData(@"e:\code\roslyn\build\config\SignToolData.json");
            converter.ConvertBuildMapFile(@"E:\code\roslyn\src\Setup\DevDivInsertionFiles\BuildDevDivInsertionFiles.vb");
            converter.WriteDiff(@"e:\temp\diff.txt");
        }
    }
}
