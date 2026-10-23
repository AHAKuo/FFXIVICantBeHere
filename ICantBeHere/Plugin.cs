using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ICantBeHere.Windows;

namespace ICantBeHere;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IDutyState DutyState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IDtrBar DtrBar { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string MainCommand = "/icantbehere";
    private const string ShortCommand = "/icbh";

    public Configuration Configuration { get; }
    public DutyLeaver Leaver { get; }

    public readonly WindowSystem WindowSystem = new("ICantBeHere");
    private ConfigWindow ConfigWindow { get; }
    private DtrEntry Dtr { get; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        Leaver = new DutyLeaver(this);
        Dtr = new DtrEntry(this);

        CommandManager.AddHandler(MainCommand, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open settings. \"/icbh on|off|toggle\" sets auto-leave, \"/icbh stay\" cancels leaving this duty.",
        });
        CommandManager.AddHandler(ShortCommand, new CommandInfo(OnCommand) { ShowInHelp = false });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUi;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleConfigUi;

        Dtr.Dispose();
        Leaver.Dispose();
        WindowSystem.RemoveAllWindows();

        CommandManager.RemoveHandler(MainCommand);
        CommandManager.RemoveHandler(ShortCommand);
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();

    public void SetEnabled(bool on)
    {
        Configuration.Enabled = on;
        Configuration.Save();
        if (!on) Leaver.Cancel();
        Feedback($"Auto-leave {(on ? "enabled" : "disabled")}.", always: true);
    }

    public void StayThisTime()
    {
        if (!Leaver.IsPending)
        {
            Feedback("Not leaving anything right now.", always: true);
            return;
        }
        Leaver.Cancel();
        Feedback("Staying this time.", always: true);
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "toggle":
                SetEnabled(!Configuration.Enabled);
                break;
            case "on":
                SetEnabled(true);
                break;
            case "off":
                SetEnabled(false);
                break;
            case "stay":
            case "cancel":
                StayThisTime();
                break;
            default:
                ToggleConfigUi();
                break;
        }
    }

    /// <summary>Print a note to the local chat log (visible only to this player).</summary>
    public void Feedback(string message, bool always = false)
    {
        if (!always && !Configuration.ShowFeedback)
            return;
        ChatGui.Print(message, "I Can't Be Here");
    }
}
