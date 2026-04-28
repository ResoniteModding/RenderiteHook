var doorstopVersion = "4.5.0";
var dotnetRuntimeVersion = "10.0.7";
var projectPath = "./RenderiteHook/RenderiteHook.csproj";

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var packageVersion = XmlPeek(projectPath, "/Project/PropertyGroup/Version");

var distDir = Directory("./dist");
var cacheDir = Directory("./downloads");

var doorstopUrl = $"https://github.com/NeighTools/UnityDoorstop/releases/download/v{doorstopVersion}/doorstop_win_release_{doorstopVersion}.zip";
var doorstopZip = cacheDir + File($"doorstop_win_release_{doorstopVersion}.zip");
var doorstopExtractDir = distDir + Directory("Doorstop");

var dotnetRuntimeUrl = $"https://builds.dotnet.microsoft.com/dotnet/Runtime/{dotnetRuntimeVersion}/dotnet-runtime-{dotnetRuntimeVersion}-win-x64.zip";
var dotnetRuntimeZip = cacheDir + File($"dotnet-runtime-{dotnetRuntimeVersion}-win-x64.zip");
var dotnetExtractDir = distDir + Directory("dotnet_extract");
var dotnetDestDir = distDir + Directory("dotnet");

Task("Clean")
	.Does(() =>
{
	EnsureDirectoryExists(distDir);
	CleanDirectory(distDir);
});

Task("DownloadDoorstop")
	.Does(() =>
{
	EnsureDirectoryExists(cacheDir);
	if (FileExists(doorstopZip))
	{
		Information("Doorstop zip cached, skipping download.");
		return;
	}
	Information($"Downloading Doorstop v{doorstopVersion}...");
	DownloadFile(doorstopUrl, doorstopZip);
	Information($"Downloaded to: {doorstopZip}");
});

Task("DownloadDotnetRuntime")
	.Does(() =>
{
	EnsureDirectoryExists(cacheDir);
	if (FileExists(dotnetRuntimeZip))
	{
		Information(".NET Runtime zip cached, skipping download.");
		return;
	}
	Information($"Downloading .NET Runtime v{dotnetRuntimeVersion}...");
	DownloadFile(dotnetRuntimeUrl, dotnetRuntimeZip);
	Information($"Downloaded to: {dotnetRuntimeZip}");
});

Task("ExtractDotnetRuntime")
	.IsDependentOn("DownloadDotnetRuntime")
	.Does(() =>
{
	Information($"Extracting .NET Runtime to {dotnetDestDir}...");

	EnsureDirectoryExists(dotnetExtractDir);
	CleanDirectory(dotnetExtractDir);
	Unzip(dotnetRuntimeZip, dotnetExtractDir);

	var sharedFrameworkDir = dotnetExtractDir + Directory("shared") + Directory("Microsoft.NETCore.App") + Directory(dotnetRuntimeVersion);

	EnsureDirectoryExists(dotnetDestDir);

	// Flatten: copy files directly into dist/dotnet/ (no subdirectory nesting)
	var globPath = (DirectoryPath)sharedFrameworkDir + "/**/*.*";
	foreach (var file in GetFiles(globPath))
	{
		var fileName = System.IO.Path.GetFileName(file.FullPath);
		var destPath = dotnetDestDir + File(fileName);
		CopyFile(file, destPath);
	}
	Information("Dotnet runtime extraction completed.");

	// Clean up temp extract dir
	if (DirectoryExists(dotnetExtractDir))
	{
		DeleteDirectory(dotnetExtractDir, new DeleteDirectorySettings { Recursive = true });
	}
});

Task("ExtractDoorstop")
	.IsDependentOn("DownloadDoorstop")
	.Does(() =>
{
	Information($"Extracting Doorstop to {doorstopExtractDir}...");

	EnsureDirectoryExists(doorstopExtractDir);
	CleanDirectory(doorstopExtractDir);

	Unzip(doorstopZip, doorstopExtractDir);
	Information("Extraction completed.");

	// Update doorstop_config.ini to point to BepInEx
	var configFile = doorstopExtractDir + File("x64/doorstop_config.ini");
	if (FileExists(configFile))
	{
		Information("Updating doorstop_config.ini...");
		var content = System.IO.File.ReadAllText(configFile);
		content = content.Replace("target_assembly=Doorstop.dll", "target_assembly=BepInEx\\core\\BepInEx.Unity.IL2CPP.dll");
		content = content.Replace("coreclr_path=", "coreclr_path=dotnet\\coreclr.dll");
		content = content.Replace("corlib_dir=", "corlib_dir=dotnet");
		System.IO.File.WriteAllText(configFile, content);
		Information("Config file updated successfully.");
	}
	else
	{
		Warning($"Config file not found at: {configFile}");
	}
});

Task("BuildRenderiteHook")
	.IsDependentOn("ExtractDoorstop")
	.IsDependentOn("ExtractDotnetRuntime")
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