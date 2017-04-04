using System;

namespace ConvertPackageRef
{
    internal struct NuGetPackage : IEquatable<NuGetPackage>
    {
        internal string Name { get; }
        internal string Version { get; }

        internal NuGetPackage(string name, string version)
        { 
            Name = name;
            Version = version;
        }

        public static bool operator ==(NuGetPackage left, NuGetPackage right) =>
            SharedUtil.NugetPackageNameComparer.Equals(left.Name, right.Name) &&
            SharedUtil.NugetPackageVersionComparer.Equals(left.Version, right.Version);
        public static bool operator !=(NuGetPackage left, NuGetPackage right) => !(left == right);
        public override bool Equals(object obj) => obj is NuGetPackage && Equals((NuGetPackage)obj);
        public override int GetHashCode() => Name?.GetHashCode() ?? 0;
        public override string ToString() => $"{Name}-{Version}";
        public bool Equals(NuGetPackage other) => this == other;
    }
}
