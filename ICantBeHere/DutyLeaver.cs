using System;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;

namespace ICantBeHere;

/// <summary>
/// Waits for a duty to complete, counts down the grace period, then leaves the instance.
/// </summary>
public sealed class DutyLeaver : IDisposable
{
    private readonly Plugin plugin;
    private readonly IDutyState.DutyCompletedDelegate onDutyCompleted;

    private DateTime? dueAt;
    private uint dutyTerritory;
    private DateTime nextAttempt = DateTime.MinValue;

    public DutyLeaver(Plugin plugin)
    {
        this.plugin = plugin;
        onDutyCompleted = _ => OnDutyCompleted();

        Plugin.DutyState.DutyCompleted += onDutyCompleted;
        Plugin.ClientState.TerritoryChanged += OnTerritoryChanged;
        Plugin.Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        Plugin.DutyState.DutyCompleted -= onDutyCompleted;
        Plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
        Plugin.Framework.Update -= OnUpdate;
    }

    public bool IsPending => dueAt != null;

    /// <summary>Seconds left on the grace period, or null when nothing is queued.</summary>
    public double? SecondsLeft => dueAt is { } due ? Math.Max(0, (due - DateTime.UtcNow).TotalSeconds) : null;

    public void Cancel() => dueAt = null;

    private Configuration Config => plugin.Configuration;

    private void OnDutyCompleted()
    {
        if (!Config.Enabled)
            return;

        var grace = Math.Max(0, Config.GracePeriodSeconds);
        dutyTerritory = Plugin.ClientState.TerritoryType;
        dueAt = DateTime.UtcNow.AddSeconds(grace);
        plugin.Feedback(grace > 0
            ? $"Duty complete. Leaving in {grace:0.#}s (click the server info bar entry to stay)."
            : "Duty complete. Leaving.");
    }

    // Left already (manually or otherwise), nothing to do.
    private void OnTerritoryChanged(uint territory) => dueAt = null;

    private void OnUpdate(IFramework framework)
    {
        if (dueAt is not { } due)
            return;

        if (!Config.Enabled || Plugin.ClientState.TerritoryType != dutyTerritory)
        {
            dueAt = null;
            return;
        }

        var now = DateTime.UtcNow;
        if (now < due || now < nextAttempt)
            return;

        // Hold the exit while something would block or break it; it goes through right after.
        if (IsBusy())
            return;

        nextAttempt = now.AddSeconds(1);
        try
        {
            if (!EventFramework.CanLeaveCurrentContent())
                return;

            dueAt = null;
            EventFramework.LeaveCurrentContent();
            Plugin.Log.Information("Left duty in territory {Territory}.", dutyTerritory);
        }
        catch (Exception ex)
        {
            dueAt = null;
            Plugin.Log.Error(ex, "Leaving the duty failed.");
            plugin.Feedback("Could not leave the duty, see /xllog.", always: true);
        }
    }

    private static bool IsBusy()
    {
        var c = Plugin.Condition;
        return c[ConditionFlag.InCombat]
            || c[ConditionFlag.WatchingCutscene]
            || c[ConditionFlag.WatchingCutscene78]
            || c[ConditionFlag.OccupiedInCutSceneEvent]
            || c[ConditionFlag.BetweenAreas]
            || c[ConditionFlag.BetweenAreas51]
            || Plugin.ObjectTable.LocalPlayer == null;
    }
}
