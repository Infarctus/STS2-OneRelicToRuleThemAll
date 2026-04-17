using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;

namespace OneRelicToRuleThemAll;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    private const string ModId = "OneRelicToRuleThemAll"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId); 

        harmony.PatchAll();
        Logger.Info("Patching Complete");
        var patchedMethods = harmony.GetPatchedMethods();
        foreach (var method in patchedMethods)
        {
            var info = Harmony.GetPatchInfo(method);
            Logger.Info($"Patched: {method.DeclaringType.Name}.{method.Name}");
            Logger.Info($"  Prefixes: {info.Prefixes.Count}, Postfixes: {info.Postfixes.Count}");
        }
    }
}
