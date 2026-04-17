using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using GodotPlugins.Game;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using OneRelicToRuleThemAll.Util;
using Environment = System.Environment;

namespace OneRelicToRuleThemAll.Patches;
[HarmonyPatch]
public static class RelicFactoryPatch
{
    [HarmonyPrefix, HarmonyPatch(typeof(RelicFactory), "PullNextRelicFromFront",typeof(Player),typeof(RelicRarity))]
    static bool PullNextRelicFromFrontPrefix(ref RelicModel __result)
    {
        MainFile.Logger.Info("PullNextRelicFromFront");
        if (StateHandler.SelectedRelic != null)
        {
            __result = StateHandler.SelectedRelic.CanonicalInstance;
            return false;
        }
        return true;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(RelicFactory), "PullNextRelicFromBack",typeof(Player),typeof(RelicRarity),typeof(IEnumerable<RelicModel>))]
    static bool PullNextRelicFromBackPrefix(ref RelicModel __result)
    {
        MainFile.Logger.Info("PullNextRelicFromBack");
        if (StateHandler.SelectedRelic != null)
        {
            __result = StateHandler.SelectedRelic.CanonicalInstance;
            return false;
        }
        return true;
    }

    [HarmonyPrefix,
     HarmonyPatch(typeof(AncientEventModel), nameof(AncientEventModel.RelicOption), typeof(RelicModel), typeof(string),
         typeof(string))] //RelicOption(RelicModel relic, string pageName = "INITIAL", string? customDonePage = null)
    static void AncientEventModel_RelicOption_Prefix(ref RelicModel relic)
    {
        if (StateHandler.SelectedRelic == null || StateHandler.IsRelicCollectionOpened)
        {
            return;
        }

        relic = StateHandler.SelectedRelic.CanonicalInstance.ToMutable();
    }
}

[HarmonyPatch]
public static class RelicCmdPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(RelicCmd), nameof(RelicCmd.Obtain), typeof(RelicModel), typeof(Player), typeof(int))]
    static bool RelicCmdObtainPatch(ref Task<RelicModel> __result, ref RelicModel relic, Player player, int index = -1)
    {
        if (StateHandler.SelectedRelic != null)
        {
            relic = StateHandler.SelectedRelic.CanonicalInstance.ToMutable();
            MainFile.Logger.Info($"Relic replaced with: {StateHandler.SelectedRelic.Title.GetFormattedText()}");
            MainFile.Logger.Info(Environment.StackTrace);
        }
        //AncientEventModel
        return true;
    }
}

[HarmonyPatch]
public static class PlayerPatch
{
    [HarmonyTargetMethods]
    static IEnumerable<MethodBase> TargetMethods()
    {
        var charModelType = typeof(CharacterModel);
        var assembly = charModelType.Assembly;
        
        // Find all non-abstract classes that inherit from CharacterModel
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsSubclassOf(charModelType) && 
                !type.IsAbstract && 
                !type.IsInterface)
            {
                var prop = type.GetProperty("StartingRelics",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null)
                {
                    var getter = prop.GetGetMethod(true);
                    if (getter != null && !getter.IsAbstract)
                        yield return getter;
                }
            }
        }
    }
    [HarmonyPostfix]
    static void StartingRelicsPostfix(ref IReadOnlyList<RelicModel> __result)
    {
        return;
        if (StateHandler.SelectedRelic == null)
        {
            return;
        }
        //MainFile.Logger.Info("StartingRelicsPostfix " + NGame.Instance?.MainMenu?.SubmenuStack._submenus.Peek().Name);
        if (NGame.Instance?.MainMenu?.SubmenuStack._submenus.Peek().Name == "CharacterSelectScreen" && RunManager.Instance.State == null)
        {
            
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                MaxDepth = 4, // Stop before hitting deep native objects
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            MainFile.Logger.Info("StartingRelics modified" + Json.Stringify(Json.FromNative(NGame.Instance,true)));
            __result = Enumerable.Repeat(StateHandler.SelectedRelic.CanonicalInstance, __result.Count).ToList();
        } else if (NGame.Instance?.MainMenu?.SubmenuStack._submenus.Peek().Name == "CharacterSelectScreen")
        {
            //MainFile.Logger.Info("CharacterSelectScreen modified" + JsonSerializer.Serialize(RunManager.Instance));
        }

        ;
        //JsonConverter();
    }
}
[HarmonyPatch]
public static class TreasureRoomPatch
{
    [HarmonyPatch(typeof(RelicGrabBag), nameof(RelicGrabBag.PullFromFront))]
    [HarmonyPrefix]
    static bool PullFromFrontPrefix(ref RelicModel __result, RelicRarity rarity, IRunState runState)
    {
        if (StateHandler.SelectedRelic != null && RunManager.Instance.IsSinglePlayerOrFakeMultiplayer)
        {
            // Return your selected relic instead
            __result = StateHandler.SelectedRelic.CanonicalInstance;
            return false; // Skip original method
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LocalContext), nameof(LocalContext.IsMe),typeof(Player))]
    static void LocalContext_IsMe_Postfix(ref Player player, ref bool __result)
    {
        if (__result == false)
        {
            if (!LocalContext.NetId.HasValue)
            {
                long netId1 = (long) player.NetId;
                ulong? netId2 = RunManager.Instance.NetService.NetId;
                long valueOrDefault = (long) netId2.GetValueOrDefault();
                __result = netId1 == valueOrDefault & netId2.HasValue;
            }
        }
        /*MainFile.Logger.Info("ShouldSelectLocalCard_Postfix result: " + __result);
        MainFile.Logger.Info("is Player me check :" +LocalContext.IsMe(player));
        long netId1 = (long) player.NetId;
        ulong? netId2 = LocalContext.NetId;
        long valueOrDefault = (long) netId2.GetValueOrDefault();
        MainFile.Logger.Info("valueOrDefault: " + valueOrDefault);
        MainFile.Logger.Info("netId2: " + netId2);
        MainFile.Logger.Info("netId2 has value: " + netId2.HasValue);
        MainFile.Logger.Info("netId1: " + netId1);
        if (!netId2.HasValue)
        {
            
        }*/
        //valueOrDefault & netId2.HasValue;
        //RunManager.FinalizeStartingRelics
        /*
         * public static bool IsMe(Player? player)
  {
    if (player == null || !LocalContext.NetId.HasValue)
      return false;
    long netId1 = (long) player.NetId;
    ulong? netId2 = LocalContext.NetId;
    long valueOrDefault = (long) netId2.GetValueOrDefault();
    return netId1 == valueOrDefault & netId2.HasValue;
  }
         */
        
    }
}

