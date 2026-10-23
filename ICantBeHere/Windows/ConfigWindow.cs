using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace ICantBeHere.Windows;

public class ConfigWindow : Window
{
    private readonly Plugin plugin;

    public ConfigWindow(Plugin plugin) : base("I Can't Be Here###ICantBeHereConfig", ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.plugin = plugin;
    }

    private Configuration Config => plugin.Configuration;

    public override void Draw()
    {
        var enabled = Config.Enabled;
        using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.45f, 1f, 0.55f, 1f), enabled))
        {
            if (ImGui.Checkbox("Leave duties automatically when they complete", ref enabled))
                plugin.SetEnabled(enabled);
        }

        var grace = Config.GracePeriodSeconds;
        ImGui.SetNextItemWidth(220);
        if (ImGui.SliderFloat("Grace period (seconds)", ref grace, 0f, 120f, "%.0f"))
        {
            Config.GracePeriodSeconds = grace;
            Config.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Time to roll on loot or say goodbye before leaving.\nCtrl+click to type a value.");

        var feedback = Config.ShowFeedback;
        if (ImGui.Checkbox("Print notices to my chat log", ref feedback))
        {
            Config.ShowFeedback = feedback;
            Config.Save();
        }

        ImGui.Separator();
        if (plugin.Leaver.SecondsLeft is { } left)
        {
            ImGui.TextUnformatted($"Leaving in {left:0.0}s");
            ImGui.SameLine();
            if (ImGui.Button("Stay this time"))
                plugin.StayThisTime();
        }
        else
        {
            ImGui.TextDisabled("Idle. Leaving waits for cutscenes and combat to end.");
        }
        ImGui.TextDisabled("Server info bar: left click toggles (or stays), right click opens this window.");
    }
}
