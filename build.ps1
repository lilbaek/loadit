$version = "1.0.6";
Get-ChildItem -Path ./nupkg -Include *.* -File -Recurse | foreach { $_.Delete()}

dotnet pack ./src/loadit.interprocess /p:VersionPrefix="$version";
dotnet pack ./src/loadit /p:VersionPrefix="$version";
dotnet pack ./src/loadit.analyzer /p:VersionPrefix="$version";
dotnet pack ./src/loadit.visualstudio /p:VersionPrefix="$version";
dotnet pack ./src/loadit.lib /p:VersionPrefix="$version";
