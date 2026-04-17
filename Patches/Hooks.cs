using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;

namespace OneRelicToRuleThemAll.Patches;

[HarmonyPatch]
public static class AncientEventModel_RelicOption_Override
{
    [HarmonyPatch]
    public static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            type: typeof(AncientEventModel),
            name: "RelicOption",
            parameters: new Type[] { typeof(RelicModel), typeof(Func<Task>), typeof(string) }
        );
    }

    [HarmonyPrefix]
    public static bool Prefix(
        AncientEventModel __instance, 
        RelicModel relic,
        ref Func<Task> onChosen,  // Use 'ref' to modify the parameter
        string pageName)
    {
        if (StateHandler.SelectedRelic == null)
        {
            return true; // Let original run unchanged
        }
        var runState = RunManager.Instance.State;
        if (runState?._players == null || runState._players.Count != 1 || runState.TotalFloor != 1)
        {
            MainFile.Logger.Info("returning early: " + runState?._players?.Count);
            return true;
        }
        
        Func<Task> originalOnChosen = onChosen;
        
        onChosen = async () =>
        {
            var runState = RunManager.Instance.State;
            if (runState?._players == null || runState._players.Count != 1 || runState.TotalFloor != 1)
            {
                await originalOnChosen();
                return;
            }
            
            
            var player = runState._players[0];
            
            foreach (RelicModel original in player._relics.ToList())
            {
                if (original.Id == StateHandler.SelectedRelic.Id)
                {
                    continue;
                }

                await RelicCmd.Replace(original, StateHandler.SelectedRelic.CanonicalInstance.ToMutable());
            }

            await originalOnChosen();

            MainFile.Logger.Info("[Hook] Relic choice logic completed.");
        };

        return true;
    }
}