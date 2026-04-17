using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace OneRelicToRuleThemAll.Patches;


[HarmonyPatch]
public class RelicCollectionPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NRelicCollectionEntry), nameof(NRelicCollectionEntry._Ready))]
    public static void NRelicCollectionEntry_ReadyPostfix(ref NRelicCollectionEntry __instance)
    {
        __instance.ModelVisibility = ModelVisibility.Visible;
        var entry = __instance;
        if (StateHandler.SelectedRelic?.Id == entry.relic.Id)
        {
            // Readd the outline
            StateHandler.AddOutline(entry);
        }
        // Connect to GuiInput event to handle right-clicks
        entry.GuiInput += (@event => OnRelicEntryGuiInput(@event, entry));
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NRelicCollectionEntry), nameof(NRelicCollectionEntry.Create))]
    public static void NRelicCollectionEntryCreatePostfix(ref NRelicCollectionEntry __result)
    {
        __result.ModelVisibility = ModelVisibility.Visible;
    }

    private static void OnRelicEntryGuiInput(InputEvent @event, NRelicCollectionEntry entry)
    {
        if (@event is InputEventMouseButton mouseEvent &&
            mouseEvent.ButtonIndex == MouseButton.Right &&
            mouseEvent.Pressed)
        {
            StateHandler.ToggleRelicSelection(entry);
        }
    }
}
[HarmonyPatch]
public static class RelicCollectionStateSwitcher
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NRelicCollection), nameof(NRelicCollection.OnSubmenuOpened))]
    public static void OnSubmenuOpened(NRelicCollection __instance)
    {
        StateHandler.IsRelicCollectionOpened = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NRelicCollection), nameof(NRelicCollection.OnSubmenuClosed))]
    public static void OnSubmenuClosed(NRelicCollection __instance)
    {
        StateHandler.IsRelicCollectionOpened = false;
    }
    
}