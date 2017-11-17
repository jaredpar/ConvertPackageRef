using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Immutable;
using ConvertPackageRef;

namespace ConvertPackageRef
{
    public static class ProjectJsonUtil
    {
        /// <summary>
        /// Does the specified project.json file need to be tracked by our repo util? 
        /// </summary>
        public static bool NeedsTracking(string filePath)
        {
            return GetDependencies(filePath).Length > 0;
        }

        public static ImmutableArray<NuGetPackage> GetDependencies(string filePath)
        {
            // Need to track any file that has dependencies
            var obj = JObject.Parse(File.ReadAllText(filePath));
            var dependencies = (JObject)obj["dependencies"];
            if (dependencies == null)
            {
                return ImmutableArray<NuGetPackage>.Empty;
            }

            var builder = ImmutableArray.CreateBuilder<NuGetPackage>();
            foreach (var dependency in dependencies.Properties())
            {
                builder.Add(ParseDependency(dependency));
            }

            return builder.ToImmutable();
        }

        /// <summary
        /// Parse out a dependency entry from the project.json file.
        /// </summary>
        public static NuGetPackage ParseDependency(JProperty prop)
        {
            var name = prop.Name;
            var excludeCompile = false;

            string version;
            if (prop.Value.Type == JTokenType.String)
            {
                version = (string)prop.Value;
            }
            else
            {
                var obj = (JObject)prop.Value;
                version = obj.Value<string>("version");
                var exclude = obj.Property("exclude");
                if (exclude != null && exclude.Value.ToString() == "compile")
                {
                    excludeCompile = true;
                }
            }

            version = version.TrimStart('[').TrimEnd(']');
            return new NuGetPackage(name, version, excludeCompile);
        }

        public static IEnumerable<string> GetProjectJsonFiles(string sourcesPath)
        {
            return Directory.EnumerateFiles(sourcesPath, "*project.json", SearchOption.AllDirectories);
        }
    }
}
