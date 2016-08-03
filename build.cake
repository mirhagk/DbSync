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
});

Task("Publish")
  .Does(()=>
  {
    NuGetPack("./DbSync.Core/DbSync.Core.csproj", new NuGetPackSettings());
    var assemblyPaths = GetFiles("./DbSync/bin/Release/**/*.dll");
    ILMerge("./DbSync/bin/Release/DbSync.exe","./DbSync/bin/Release/DbSync.exe", assemblyPaths);
  });

RunTarget(target);