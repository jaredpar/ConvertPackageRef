ConvertPackageRef


===

Tool used to convert the [dotnet/roslyn](https://github.com/dotnet/roslyn) repo to the new SDK / csproj format.  

There are roughly three types of projects in Roslyn that had to be considered:

1. PCL: Fully converted to new SDK / csproj format with this tool.
1. 4.6 Desktop: Converted to use PackageReference elements with this tool.
1. 2.0 Desktop: Hand ported to use PackageReference elements.
