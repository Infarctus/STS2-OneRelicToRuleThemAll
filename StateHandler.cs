using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;

namespace OneRelicToRuleThemAll;

public class StateHandler
{
    public static RelicModel? SelectedRelic = null; // null
    private static bool _isRelicCollectionOpened = false;
    
    public static bool IsRelicCollectionOpened
    {
        get => _isRelicCollectionOpened;
        set
        {
            _isRelicCollectionOpened = value;
            if (!_isRelicCollectionOpened)
            {
                RemoveOutline();
            }
        }
    }
    
    public static Panel? ActiveOutline = null;

    public static void ToggleRelicSelection(NRelicCollectionEntry entry)
    {
        // 1. If we clicked the currently selected one, deselect it
        if (SelectedRelic != null && entry.relic.Id == SelectedRelic.Id)
        {
            RemoveOutline();
            SelectedRelic = null;
            MainFile.Logger.Info("Relic removed");
            return;
        }

        // 2. Otherwise, select the new one (remove old outline first)
        RemoveOutline();
        
        SelectedRelic = entry.relic;
        AddOutline(entry);
        MainFile.Logger.Info($"Selected relic: {entry.relic.Title.GetRawText()}");
    }

    private static void RemoveOutline()
    {
        if (ActiveOutline != null && GodotObject.IsInstanceValid(ActiveOutline))
        {
            ActiveOutline.QueueFree();
            ActiveOutline = null;
        }
    }

    public static void AddOutline(NRelicCollectionEntry entry)
    {
        if (entry._relicNode?.GetChild(0) is TextureRect holder)
        {
            Panel outline = new Panel();
            outline.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            outline.MouseFilter = Control.MouseFilterEnum.Ignore;

            StyleBoxFlat style = new StyleBoxFlat
            {
                DrawCenter = false,
                BorderColor = new Color(1, 0, 0),
                AntiAliasing = true,
                AntiAliasingSize = 1.0f,
                ShadowColor = new Color(1, 0, 0, 0.5f),
                ShadowSize = 4
            };
            style.SetBorderWidthAll(2);
            style.SetExpandMarginAll(2);

            outline.AddThemeStyleboxOverride("panel", style);
            holder.AddChild(outline);
            ActiveOutline = outline;
        }
    }
}