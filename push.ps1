$version = "1.0.6";
$loadit = "./nupkg/Loadit." + "$version" + ".nupkg";
$loaditAnalyzer = "./nupkg/Loadit.Analyzer." + "$version" + ".nupkg";
$loaditVisualStudio = "./nupkg/Loadit.VisualStudio." + "$version" + ".nupkg";
$loaditApis = "./nupkg/Loadit.Apis." + "$version" + ".nupkg";
$loaditInterprocess = "./nupkg/Loadit.Interprocess." + "$version" + ".nupkg";

dotnet nuget push "$loadit" -k $Env:LoaditApiKey -s https://api.nuget.org/v3/index.json
dotnet nuget push "$loaditAnalyzer" -k $Env:LoaditApiKey -s https://api.nuget.org/v3/index.json
dotnet nuget push "$loaditVisualStudio" -k $Env:LoaditApiKey -s https://api.nuget.org/v3/index.json
dotnet nuget push "$loaditApis" -k $Env:LoaditApiKey -s https://api.nuget.org/v3/index.json
dotnet nuget push "$loaditInterprocess" -k $Env:LoaditApiKey -s https://api.nuget.org/v3/index.json