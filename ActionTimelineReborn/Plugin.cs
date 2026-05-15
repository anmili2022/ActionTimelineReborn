using ActionTimelineReborn.Configurations;
using ActionTimelineReborn.Helpers;
using ActionTimelineReborn.Timeline;
using ActionTimelineReborn.Windows;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
using ECommons.Commands;
using ECommons.DalamudServices;
using ECommons.GameHelpers;

namespace ActionTimelineReborn;

public class Plugin : IDalamudPlugin
{
    public static readonly SortedList<uint, byte> IconStack = new()
    {
        { 16203, 0 }, // Medicine.

        { 10155, 1 }, //PLD Fight or Flight.

        { 12556, 1 }, //WAR Inner Strength.

        { 17926, 1 }, //DRK Blood Weapon.

        { 13601, 1 }, //GNB No mercy.

        { 12627, 1 }, //WHM Presence of Mind.

        { 12809, 1 }, //SCH Chain Stratagem.

        { 13245, 1 }, //AST Divination.
        { 13259, 2 }, //AST Harmony of Spirit.

        { 12532, 1 }, //MNK Brotherhood.
        { 12528, 2 }, //MNK Brotherhood.

        { 12578, 1 }, //DRG Battle Litany.
        { 12581, 2 }, //DRG Right Eye.
        { 10304, 3 }, //DRG Lance Charge.

        { 12918, 1 }, //NIN Trick Attack.
        { 15020, 2 }, //NIN Vulnerability Up.

        { 12601, 1 }, //BRD Battle Voice.
        { 12622, 2 }, //BRD Radiant Finale.
        { 10354, 3 }, //BRD Radiant Finale.

        { 13011, 1 }, //MCH Wildfire.

        { 13714, 1 }, //DNC Devilment.
        { 13709, 2 }, //DNC Technical Finish.

        { 12653, 1 }, //BLM Ley Lines.

        { 13409, 1 }, //RDM Embolden.

        { 12699, 1 }, //SMN 2703.
    };
    public static string Name => "ActionTimelineReborn";

    public static string Version { get; private set; } = "";

    public static Settings Settings { get; private set; } = null!;

    private static WindowSystem _windowSystem = null!;
    private static SettingsWindow _settingsWindow = null!;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        var eCommonsInitialized = false;
        var drawRegistered = false;
        var openConfigRegistered = false;
        var openMainRegistered = false;
        var timelineInitialized = false;

        try
        {
            ECommonsMain.Init(pluginInterface, this);
            eCommonsInitialized = true;

            Svc.PluginInterface.UiBuilder.Draw += Draw;
            drawRegistered = true;

            Svc.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
            openConfigRegistered = true;

            Svc.PluginInterface.UiBuilder.OpenMainUi += OpenConfigUi;
            openMainRegistered = true;

            TimelineManager.Initialize();
            timelineInitialized = true;

            DrawHelper.Init();

            try
            {
                Settings = pluginInterface.GetPluginConfig() as Settings ?? new Settings();
            }
            catch
            {
                Settings = new Settings();
            }

            CreateWindows();
        }
        catch
        {
            if (timelineInitialized)
            {
                TimelineManager.Instance?.Dispose();
            }

            if (drawRegistered)
            {
                Svc.PluginInterface.UiBuilder.Draw -= Draw;
            }

            if (openConfigRegistered)
            {
                Svc.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
            }

            if (openMainRegistered)
            {
                Svc.PluginInterface.UiBuilder.OpenMainUi -= OpenConfigUi;
            }

            if (eCommonsInitialized)
            {
                ECommonsMain.Dispose();
            }

            throw;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    [Cmd("/atl", "打开 ActionTimelineReborn 设置窗口；使用 /atl <时间轴名称> 显示或隐藏指定时间轴。")]
    [SubCmd("lock", "锁定所有时间轴窗口")]
    [SubCmd("unlock", "解锁所有时间轴窗口")]
    private static void PluginCommand(string command, string arguments)
    {
        var trimmedArguments = arguments.Trim();
        var timelineName = trimmedArguments.Trim('"');
        var sub = trimmedArguments.Split(' ').FirstOrDefault();
        if(string.Equals("unlock", sub, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var setting in Settings.TimelineSettings)
            {
                setting.Locked = false;
            }
            Settings.Save();
            Svc.Chat.Print("[ActionTimelineReborn] 已解锁所有时间轴窗口。");
        }
        else if (string.Equals("lock", sub, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var setting in Settings.TimelineSettings)
            {
                setting.Locked = true;
            }
            Settings.Save();
            Svc.Chat.Print("[ActionTimelineReborn] 已锁定所有时间轴窗口。");
        }
        else if (!string.IsNullOrWhiteSpace(trimmedArguments))
        {
            ToggleTimeline(timelineName);
        }
        else
        {
            _settingsWindow.IsOpen = !_settingsWindow.IsOpen;
        }
    }

    private static void ToggleTimeline(string timelineName)
    {
        List<DrawingSettings> matches = [];
        foreach (var setting in Settings.TimelineSettings)
        {
            if (string.Equals(setting.Name, timelineName, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(setting);
            }
        }

        if (matches.Count == 0)
        {
            Svc.Chat.Print($"[ActionTimelineReborn] 未找到名为“{timelineName}”的时间轴。");
            return;
        }

        var enabled = !matches[0].Enable;
        foreach (var setting in matches)
        {
            setting.Enable = enabled;
        }

        Settings.Save();
        Svc.Chat.Print($"[ActionTimelineReborn] 已{(enabled ? "显示" : "隐藏")}时间轴“{timelineName}”。");
    }

    private void CreateWindows()
    {
        _settingsWindow = new SettingsWindow();

        _windowSystem = new WindowSystem("ActionTimelineReborn_Windows");
        AddWindow(_windowSystem, _settingsWindow);
    }

    private static void AddWindow(WindowSystem windowSystem, SettingsWindow settingsWindow)
    {
        var addWindowMethod = typeof(WindowSystem).GetMethods()
            .First(method => method.Name == "AddWindow" && method.GetParameters().Length == 1);

        addWindowMethod.Invoke(windowSystem, [settingsWindow]);
    }

    private void Draw()
    {
        if (Settings == null || !Player.Available) return;
        if (Svc.GameGui.GameUiHidden) return;

        _windowSystem?.Draw();

        if (!ShowTimeline()) return;

        int index = 0;
        foreach (var setting in Settings.TimelineSettings)
        {
            TimelineWindow.Draw(setting, index++);
        }
    }

    private bool ShowTimeline()
    {
        if (Settings.ShowTimelineOnlyInCombat && !Svc.Condition[ConditionFlag.InCombat])
        {
            return false;
        }

        if (Settings.ShowTimelineOnlyInDuty && !Svc.Condition[ConditionFlag.BoundByDuty])
        {
            return false;
        }

        if (Settings.HideTimelineInCutscene 
            && (Svc.Condition[ConditionFlag.WatchingCutscene] 
            || Svc.Condition[ConditionFlag.WatchingCutscene78]
            || Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]))
        {
            return false;
        }

        if (Settings.HideTimelineInQuestEvent 
            && (Svc.Condition[ConditionFlag.OccupiedInQuestEvent]))
        {
            return false;
        }

        return true;
    }

    public static void OpenConfigUi()
    {
        _settingsWindow.IsOpen = true;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        Settings.Save();

        TimelineManager.Instance?.Dispose();

        _windowSystem.RemoveAllWindows();

        ECommonsMain.Dispose();

        Svc.PluginInterface.UiBuilder.Draw -= Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        Svc.PluginInterface.UiBuilder.OpenMainUi -= OpenConfigUi;
    }
}
