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
            var newArgs = string.Join(' ', [args, .. Environment.GetCommandLineArgs().Skip(1)]);
            Log.LogInfo($"Starting renderer with args: {newArgs}");
            return newArgs;
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
