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
    internal static new ManualLogSource Log;

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
                if(code.operand is MethodInfo mi && mi.Name == "ToStringAndClear")
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
            var newArgs = string.Join(' ', [args, ..Environment.GetCommandLineArgs().Skip(1).Select(x=> '"' + x + '"')]);
            Log.LogInfo($"Starting renderer with args: {newArgs}");
            return newArgs;
        }
    }
}
