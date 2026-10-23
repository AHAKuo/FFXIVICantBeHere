using System;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;

namespace ICantBeHere;

/// <summary>
/// Server info bar entry: shows on/off and the leave countdown.
/// Left click toggles (or cancels a running countdown), right click opens settings.
/// </summary>
public sealed class DtrEntry : IDisposable
{
    private const string Title = "I Can't Be Here";
    private const ushort MutedColor = 3;
    private const ushort CountdownColor = 500;

    private readonly Plugin plugin;
    private IDtrBarEntry? entry;
    private DateTime retryAt = DateTime.MinValue;
    private string? lastText;

    public DtrEntry(Plugin plugin)
    {
        this.plugin = plugin;
        Plugin.Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        Plugin.Framework.Update -= OnUpdate;
        entry?.Remove();
        entry = null;
    }

    private void OnUpdate(IFramework framework)
    {
        if (entry == null && !TryCreate())
            return;

        var cfg = plugin.Configuration;
        var left = plugin.Leaver.SecondsLeft;
        var text = left is { } s ? $"Leaving in {Math.Ceiling(s):0}s"
            : cfg.Enabled ? "Auto-leave: On" : "Auto-leave: Off";
        if (text == lastText)
            return;
        lastText = text;

        entry!.Text = left != null ? new SeStringBuilder().AddUiForeground(text, CountdownColor).Build()
            : cfg.Enabled ? new SeString(new TextPayload(text))
            : new SeStringBuilder().AddUiForeground(text, MutedColor).Build();
        entry.Tooltip = left != null
            ? "Duty complete, leaving soon.\nLeft click: stay this time. Right click: settings."
            : $"Auto-leave {(cfg.Enabled ? "on" : "off")}, grace period {cfg.GracePeriodSeconds:0.#}s.\nLeft click: toggle. Right click: settings.";
    }

    private bool TryCreate()
    {
        if (DateTime.UtcNow < retryAt)
            return false;
        try
        {
            entry = Plugin.DtrBar.Get(Title);
            entry.OnClick = OnClick;
            entry.Shown = true;
            lastText = null;
            return true;
        }
        catch (Exception ex)
        {
            // Dalamud refuses a title a stale entry still holds; try again later.
            Plugin.Log.Warning(ex, "Server info bar entry could not be created; retrying in 10 s.");
            retryAt = DateTime.UtcNow.AddSeconds(10);
            entry = null;
            return false;
        }
    }

    private void OnClick(DtrInteractionEvent e)
    {
        if (e.ClickType == MouseClickType.Right)
        {
            plugin.ToggleConfigUi();
            return;
        }

        if (plugin.Leaver.IsPending)
            plugin.StayThisTime();
        else
            plugin.SetEnabled(!plugin.Configuration.Enabled);
        lastText = null;
    }
}
