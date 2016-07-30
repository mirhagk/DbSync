var target = Argument("target", "Default");

Task("Default")
  .Does(() =>
{
  XBuild("./DbSync.sln");
  Information("Hello World!");
});

RunTarget(target);