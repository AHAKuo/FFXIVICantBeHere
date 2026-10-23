using System;
using Dalamud.Configuration;

namespace ICantBeHere;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    /// <summary>Leave duties automatically when they complete.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Seconds to wait after the duty completes before leaving.</summary>
    public float GracePeriodSeconds { get; set; } = 5f;

    /// <summary>Print a line to your own chat log when leaving is scheduled.</summary>
    public bool ShowFeedback { get; set; } = true;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
