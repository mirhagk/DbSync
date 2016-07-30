var target = Argument("target", "Default");

Task("Default")
  .Does(() =>
{
  var solution = "./DbSync.sln"; 
  NuGetRestore(solution);
  XBuild(solution, new XBuildSettings {
    Configuration = "Release"
    });
  var assemblyPaths = GetFiles("./DbSync/bin/Release/**/*.dll");
  //ILMerge("./DbSync/bin/Release/DbSync.exe","./DbSync/bin/Release/DbSync.exe", assemblyPaths);
  Information("Project successfully built!");
});

RunTarget(target);