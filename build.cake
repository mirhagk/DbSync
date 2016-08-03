#tool "nuget:?package=NUnit.ConsoleRunner"
var target = Argument("target", "Default");

Task("Default")
  .Does(() =>
{
  var solution = "./DbSync.sln"; 
  NuGetRestore(solution);
  XBuild(solution, new XBuildSettings {
    Configuration = "Release"
    });
  Information("Project successfully built!");
  NUnit3("./**/bin/Release/DbSync.Tests.dll");
  Information("Tests completed");
});

Task("Publish")
  .Does(()=>
  {
    NuGetPack("./DbSync.Core/DbSync.Core.csproj", new NuGetPackSettings());
    var assemblyPaths = GetFiles("./DbSync/bin/Release/**/*.dll");
    ILMerge("./DbSync/bin/Release/DbSync.exe","./DbSync/bin/Release/DbSync.exe", assemblyPaths);
  });

RunTarget(target);