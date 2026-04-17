using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Saves;

namespace OneRelicToRuleThemAll.Patches;
[HarmonyPatch]
public class SpecificRelicCreation
{
    private static bool HandleGenericRelicEvent<T>(T __instance, ref Task __result, string l10nKey) where T : EventModel
    {
        // 1. Check if we should override
        if (StateHandler.SelectedRelic == null) return true;
    
        // 2. Access the Player/Owner  
        var player = __instance.Owner;

        // 3. Obtain the selected relic and block until finished
        RelicCmd.Obtain(StateHandler.SelectedRelic.CanonicalInstance, player!).GetAwaiter().GetResult();
 
        // 5. Signal the event is finished
        __instance.SetEventFinished(__instance.L10NLookup(l10nKey));

        // 6. Complete the task and block the original method
        __result = Task.CompletedTask;
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(HatchRestSiteOption), "OnSelect")]
    private static bool HatchRestSiteOptionOnSelectPrefix(HatchRestSiteOption __instance, ref Task<bool> __result)
    {
        if (StateHandler.SelectedRelic == null) return true;
        var player = __instance.Owner;

        RelicCmd.Obtain(StateHandler.SelectedRelic.CanonicalInstance, player).GetAwaiter().GetResult();
        var list = PileType.Deck.GetPile(player).Cards.Where((Func<CardModel, bool>)(c => c is ByrdonisEgg)).ToList();
        foreach (var original in list) original.RemoveFromCurrentPile();

        __result = Task.FromResult(true);
        return false; // Skip original
    }

    [HarmonyPatch(typeof(ColossalFlower), "ObtainPollinousCore")]
    [HarmonyPrefix]
    private static bool ObtainPollinousCorePrefix(ColossalFlower __instance, ref Task __result) 
        => HandleGenericRelicEvent(__instance, ref __result, "COLOSSAL_FLOWER.pages.POLLINOUS_CORE.description");

    [HarmonyPatch(typeof(GraveOfTheForgotten), "Accept")]
    [HarmonyPrefix]
    private static bool GraveAcceptPrefix(GraveOfTheForgotten __instance, ref Task __result) 
        => HandleGenericRelicEvent(__instance, ref __result, "GRAVE_OF_THE_FORGOTTEN.pages.ACCEPT.description");

    [HarmonyPatch(typeof(HungryForMushrooms), "BigMushroom")]
    [HarmonyPrefix]
    private static bool BigMushroomPrefix(HungryForMushrooms __instance, ref Task __result) 
        => HandleGenericRelicEvent(__instance, ref __result, "HUNGRY_FOR_MUSHROOMS.pages.BIG_MUSHROOM.description");
    
    [HarmonyPatch(typeof(HungryForMushrooms), "FragrantMushroom")]
    [HarmonyPrefix]
    private static bool FragrantMushroomPrefix(HungryForMushrooms __instance, ref Task __result) 
        => HandleGenericRelicEvent(__instance, ref __result, "HUNGRY_FOR_MUSHROOMS.pages.FRAGRANT_MUSHROOM.description");
    
    [HarmonyPatch(typeof(LostWisp), "Claim")]
    [HarmonyPrefix]
    private static bool LostWispClaimPrefix(LostWisp __instance, ref Task __result) 
        => HandleGenericRelicEvent(__instance, ref __result, "LOST_WISP.pages.CLAIM.description");
    
    [HarmonyPatch(typeof(RoomFullOfCheese), "Search")]
    [HarmonyPrefix]
    private static bool RoomFullOfCheeseSearchPrefix(RoomFullOfCheese __instance, ref Task __result) 
        => HandleGenericRelicEvent(__instance, ref __result, "ROOM_FULL_OF_CHEESE.pages.SEARCH.description");
    
    [HarmonyPatch(typeof(RoundTeaParty), "EnjoyTea")]
    [HarmonyPrefix]
    private static bool EnjoyTeaPrefix(RoundTeaParty __instance, ref Task __result) 
        => HandleGenericRelicEvent(__instance, ref __result, "ROUND_TEA_PARTY.pages.ENJOY_TEA.description");
    
    [HarmonyPatch(typeof(SunkenStatue), "GrabSword")]
    [HarmonyPrefix]
    private static bool SunkenStatueGrabSwordPrefix(SunkenStatue __instance, ref Task __result) 
        => HandleGenericRelicEvent(__instance, ref __result, "SUNKEN_STATUE.pages.GRAB_SWORD.description");
    
    [HarmonyPatch(typeof(TeaMaster), "BoneTea")]
    [HarmonyPrefix]
    private static bool TeaMasterBoneTeaPrefix(TeaMaster __instance, ref Task __result) 
        => HandleGenericRelicEvent(__instance, ref __result, "TEA_MASTER.pages.DONE.description");
    
    [HarmonyPatch(typeof(TeaMaster), "EmberTea")]
    [HarmonyPrefix]
    private static bool TeaMasterEmberTeaPrefix(TeaMaster __instance, ref Task __result) 
        => HandleGenericRelicEvent(__instance, ref __result, "TEA_MASTER.pages.DONE.description");
    
    [HarmonyPatch(typeof(TeaMaster), "TeaOfDiscourtesy")]
    [HarmonyPrefix]
    private static bool TeaMasterTeaOfDiscourtesyPrefix(TeaMaster __instance, ref Task __result) 
        => HandleGenericRelicEvent(__instance, ref __result, "TEA_MASTER.pages.TEA_OF_DISCOURTESY.description");

    [HarmonyPatch(typeof(WelcomeToWongos), "CheckObtainWongoBadge")]
    [HarmonyPrefix]
    private static bool WelcomeToWongosCheckObtainWongoBadgePrefix(WelcomeToWongos __instance, ref Task<LocString> __result, int pointsEarned)
    {
        if (StateHandler.SelectedRelic == null) return true;
        
        int wongoPoints = SaveManager.Instance.Progress.WongoPoints;
        int num1 = wongoPoints % 2000 + pointsEarned;
        int num2 = wongoPoints + pointsEarned;
        
        __instance.DynamicVars["WongoPointAmount"].BaseValue = (decimal)num1;
        __instance.DynamicVars["RemainingWongoPointAmount"].BaseValue = (decimal)(2000 - num1);
        __instance.DynamicVars["TotalWongoBadgeAmount"].BaseValue = (decimal)(num2 / 2000);
    
        __instance.Owner!.ExtraFields.WongoPoints = pointsEarned;
        
        string l10nKey;

        if (num1 < 2000)
        {
            // Path A: Not enough points for a badge/relic
            l10nKey = (__instance.DynamicVars["TotalWongoBadgeAmount"].BaseValue > 0M) 
                ? "WELCOME_TO_WONGOS.pages.AFTER_BUY_BADGE_COUNTER.description" 
                : "WELCOME_TO_WONGOS.pages.AFTER_BUY.description";
        }
        else
        {
            // Path B: Reward reached!
            // Replace WongoBadge with your SelectedRelic
            RelicCmd.Obtain(StateHandler.SelectedRelic.CanonicalInstance, __instance.Owner).GetAwaiter().GetResult();
            l10nKey = "WELCOME_TO_WONGOS.pages.AFTER_BUY_RECEIVE_BADGE.description";
        }
        
        __result = Task.FromResult(__instance.L10NLookup(l10nKey)); 

        return false; // Skip original
    }

    [HarmonyPatch(typeof(WelcomeToWongos), "BuyMysteryBox")]
    [HarmonyPrefix]
    private static bool WelcomeToWongosBuyMysteryBoxPrefix(WelcomeToWongos __instance,ref Task __result)
    {
        if (StateHandler.SelectedRelic == null) return true;
        
        PlayerCmd.LoseGold(__instance.DynamicVars["MysteryBoxCost"].BaseValue, __instance.Owner!, GoldLossType.Spent);
        
        
        __instance.SetEventFinished(__instance.CheckObtainWongoBadge(8).GetAwaiter().GetResult());
        __result = Task.CompletedTask;
        return false;
    }
}