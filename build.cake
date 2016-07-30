var target = Argument("target", "Default");

Task("Default")
  .Does(() =>
{
  var solution = "./DbSync.sln"; 
  NuGetRestore(solution);
  XBuild(solution);
  Information("Hello World!");
});

RunTarget(target);