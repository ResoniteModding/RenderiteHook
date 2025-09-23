using BepInEx;
using BepInEx.Logging;
using BepInEx.NET.Common;
using BepInExResoniteShim;
using FrooxEngine;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace RenderiteHook;

[ResonitePlugin(PluginMetadata.GUID, PluginMetadata.NAME, PluginMetadata.VERSION, PluginMetadata.AUTHORS, PluginMetadata.REPOSITORY_URL)]
[BepInDependency(BepInExResoniteShim.PluginMetadata.GUID, BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log = null!;

    public override void Load()
    {
        Log = base.Log;

        HarmonyInstance.PatchAll();
    }

    [HarmonyPatch(typeof(RenderSystem), "StartRenderer", MethodType.Async)]
    public class ArgumentsPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            var patched = false;
            foreach (var code in codes)
            {
                yield return code;
                if (code.operand is MethodInfo mi && mi.Name == "ToStringAndClear")
                {
                    patched = true;
                    yield return new CodeInstruction(OpCodes.Call, typeof(ArgumentsPatch).GetMethod(nameof(OnStartRenderer), BindingFlags.Static | BindingFlags.Public)); // Call our method
                }
            }

            if (!patched)
            {
                Log.LogError("Failed to patch StartRenderer arguments!");
            }
        }

        public static string OnStartRenderer(string args)
        {
            CopyDoorstopFiles(Engine.Current.RenderSystem);
            
            var originalArgs = GetOriginalCommandLineArgs();
            var newArgs = string.IsNullOrEmpty(originalArgs) ? args : args + " " + originalArgs;

            // Try to parse doorstop path, to fix it when running through proton
            TryFixWineDoorstopPath(ref newArgs);


            Log.LogInfo($"Starting renderer with args: {newArgs}");
            return newArgs;
        }

        private static void TryFixWineDoorstopPath(ref string newArgs)
        {
            try
            {
                var targetAsmArg = newArgs.IndexOf("--doorstop-target-assembly");
                if (targetAsmArg < 0) return;

                var nextSpace = newArgs.IndexOf(" ", targetAsmArg);
                if (nextSpace < 0) return;

                var argStart = GetIndexOfFirstNonSpace(newArgs, nextSpace);
                if (argStart < 0) return;

                var argEnd = -1;

                if (newArgs[argStart] == '"')
                {
                    argStart += 1;
                    argEnd = newArgs.IndexOf('"', argStart);
                }
                else
                {
                    argEnd = newArgs.IndexOf(' ', argStart);
                }
                if (argEnd < 0) argEnd = newArgs.Length;

                var path = newArgs.Substring(argStart, argEnd - argStart);

                // TODO: replace this check with a proper check to see if bootstrapper is running inside proton.
                // The current check works assuming all paths passed to doorstop-target-assembly are fully qualified
                // (because then linux paths start with a slash, and windows ones start with a letter)
                // which is always the case when using mod managers.
                if (!path.StartsWith('/')) return;

                Log.LogInfo($"Replacing --doorstop-target-assembly to add Z: drive. Old arguments: {newArgs}");
                newArgs = newArgs.Substring(0, argStart) + "Z:" + path + newArgs.Substring(argEnd);
            }
            catch {}
        }

        private static int GetIndexOfFirstNonSpace(string input, int startIndex)
        {
            for (int i = startIndex; i < input.Length; i++)
            {
                if (!char.IsWhiteSpace(input[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        private static string GetOriginalCommandLineArgs()
        {
            string str = Environment.CommandLine;

            if (str.StartsWith("\"") && str.Length > 1)
            {
                int ix = str.IndexOf("\"", 1);
                if (ix != -1)
                {
                    str = str.Substring(ix + 1).TrimStart();
                }
            }
            else
            {
                int ix = str.IndexOf(" ");
                if (ix != -1)
                {
                    str = str.Substring(ix + 1).TrimStart();
                }
            }

            return str;
        }

        private static void CopyDoorstopFiles(RenderSystem renderSystem)
        {
            try
            {
                var pluginLocation = Assembly.GetExecutingAssembly().Location;
                var pluginDir = Path.GetDirectoryName(pluginLocation);

                if (string.IsNullOrEmpty(pluginDir))
                {
                    Log.LogError("Could not determine plugin directory");
                    return;
                }

                var doorstopSourceDir = Path.Combine(pluginDir, "Doorstop");

                if (!Directory.Exists(doorstopSourceDir))
                {
                    Log.LogWarning($"Doorstop directory not found at: {doorstopSourceDir}");
                    return;
                }

                var rendererPath = renderSystem.RendererPath;
                var rendererDir = Path.GetDirectoryName(rendererPath);

                if (string.IsNullOrEmpty(rendererDir))
                {
                    Log.LogError("Could not determine renderer directory");
                    return;
                }

                Log.LogInfo($"Copying Doorstop files from {doorstopSourceDir} to {rendererDir}");

                foreach (var file in Directory.GetFiles(doorstopSourceDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(doorstopSourceDir, file);
                    var destPath = Path.Combine(rendererDir, relativePath);

                    File.Copy(file, destPath, overwrite: true);
                    Log.LogInfo($"Copied: {relativePath}");
                }

                Log.LogInfo("Doorstop files copied successfully");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to copy Doorstop files: {ex.Message}");
            }
        }
    }
}
