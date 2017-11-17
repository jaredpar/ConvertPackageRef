using System;

namespace ConvertPackageRef
{
    public struct NuGetPackage
    {
        public string Name { get; }
        public string Version { get; }
        public bool ExcludeCompile { get; }

        public NuGetPackage(string name, string version, bool excludeCompile = false)
        { 
            Name = name;
            Version = version;
            ExcludeCompile = excludeCompile;
        }
    }
}
