var doorstopVersion = "4.4.1";
var projectPath = "./RenderiteHook/RenderiteHook.csproj";

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var packageVersion = XmlPeek(projectPath, "/Project/PropertyGroup/Version");

var distDir = Directory("./dist");
var extractDir = distDir + Directory("Doorstop");
var downloadUrl = $"https://github.com/NeighTools/UnityDoorstop/releases/download/v{doorstopVersion}/doorstop_win_release_{doorstopVersion}.zip";
var zipFile = distDir + File($"doorstop_win_release_{doorstopVersion}.zip");

Task("Clean")
    .Does(() =>
{
    if (DirectoryExists(distDir))
    {
        CleanDirectory(distDir);
    }
    else
    {
        CreateDirectory(distDir);
    }
});

Task("DownloadDoorstop")
    .IsDependentOn("Clean")
    .Does(() =>
{
    Information($"Downloading Doorstop v{doorstopVersion}...");

    if (!FileExists(zipFile))
    {
        DownloadFile(downloadUrl, zipFile);
        Information($"Downloaded to: {zipFile}");
    }
    else
    {
        Information("File already exists, skipping download.");
    }
});

Task("ExtractDoorstop")
    .IsDependentOn("DownloadDoorstop")
    .Does(() =>
{
    Information($"Extracting Doorstop to {extractDir}...");

    if (!DirectoryExists(extractDir))
    {
        CreateDirectory(extractDir);
    }

    Unzip(zipFile, extractDir);
    Information("Extraction completed.");
});

Task("BuildRenderiteHook")
    .IsDependentOn("ExtractDoorstop")
    .Does(() =>
{
    Information($"Building RenderiteHook version {packageVersion}...");

    // Build RenderiteHook project
    DotNetBuild(projectPath, new DotNetBuildSettings
    {
        Configuration = configuration,
        Verbosity = DotNetVerbosity.Minimal
    });

    Information("RenderiteHook build completed successfully.");
});

Task("Build")
    .IsDependentOn("BuildRenderiteHook")
    .Does(() =>
{
    Information($"Building Thunderstore package with version {packageVersion}...");

    // Run dotnet tcli build command
    var exitCode = StartProcess("dotnet", new ProcessSettings
    {
        Arguments = $"tcli build --package-version {packageVersion}",
        WorkingDirectory = Directory(".")
    });

    if (exitCode != 0)
    {
        throw new Exception($"dotnet tcli build failed with exit code {exitCode}");
    }

    Information("Thunderstore package build completed successfully.");
});

Task("Default")
    .IsDependentOn("Build");

RunTarget(target);