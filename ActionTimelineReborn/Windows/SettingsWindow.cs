using ActionTimelineReborn.Helpers;
using ActionTimelineReborn.Timeline;
using ActionTimelineReborn.Configurations;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ECommons.Commands;
using ECommons.DalamudServices;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using System.Numerics;

namespace ActionTimelineReborn.Windows
{
    public class SettingsWindow : Window
    {
        private float _scale => ImGuiHelpers.GlobalScale;
        private Settings Settings => Plugin.Settings;

        public SettingsWindow() : base($"ActionTimelineReborn 设置 v{typeof(SettingsWindow).Assembly.GetName().Version?.ToString() ?? string.Empty}")
        {
            SizeCondition = ImGuiCond.FirstUseEver;
            Size = new Vector2(300, 490f);
            RespectCloseHotkey = true;
        }

        public override void OnClose()
        {
            Settings.Save();
            base.OnClose();
        }

        public override void Draw()
        {
            if (!ImGui.BeginTabBar("ActionTimelineReborn Bar")) return;

            if (ImGui.BeginTabItem("常规##General"))
            {
                DrawGeneralSetting();
                ImGui.EndTabItem();
            }

            int index = 0;
            DrawingSettings? removingSetting = null;

            if (Settings.TimelineSettings.Count == 0) Settings.TimelineSettings.Add(new DrawingSettings());

            foreach (var setting in Settings.TimelineSettings)
            {
                if (ImGui.BeginTabItem($"时间轴:{index}##TL:{index}"))
                {
                    if (DrawTimelineSetting(setting))
                    {
                        removingSetting = setting;
                    }
                    ImGui.EndTabItem();
                }
                index++;
            }

            if(removingSetting != null)
            {
                Settings.TimelineSettings.Remove(removingSetting);
            }

            if (ImGui.BeginTabItem("帮助##Help"))
            {
                DrawHelp();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        private void DrawHelp() 
        {
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{FontAwesomeIcon.Code.ToIconString()}##Github"))
            {
                Util.OpenLink("https://github.com/ArchiDog1998/ActionTimelineReborn");
            }

            ImGui.SameLine();

            if (ImGui.Button($"{FontAwesomeIcon.History.ToIconString()}##ChangeLog"))
            {
                Util.OpenLink("https://github.com/ArchiDog1998/ActionTimelineReborn/blob/release/CHANGELOG.md");
            }
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF5E5BFF);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD5E5BFF);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA5E5BFF);
            if (ImGui.Button($"{FontAwesomeIcon.Coffee.ToIconString()}##Support"))
            {
                Util.OpenLink("https://ko-fi.com/archited");
            }

            ImGui.PopStyleColor(3);
            ImGui.PopFont();

            if (ImGui.BeginChild("Help Information", new Vector2(0f, -1f), true))
            {
                CmdManager.DrawHelp();
                ImGui.Separator();
                ImGui.TextWrapped("/atl <时间轴名称>：显示或隐藏指定时间轴；名称带空格时可使用引号。");
                ImGui.EndChild();
            }
        }

        private ushort _aboutAdd = 0;
        private void DrawGeneralSetting()
        {
            if (ImGui.Button("添加一个时间轴##Add One Timeline"))
            {
                Settings.TimelineSettings.Add(new DrawingSettings()
                {
                    Name = (Settings.TimelineSettings.Count + 1).ToString(),
                });
            }
            ImGui.Checkbox("记录数据##Record Data", ref Settings.Record);
            ImGui.Checkbox("仅在副本中显示##Show Only In Duty", ref Settings.ShowTimelineOnlyInDuty);
            ImGui.Checkbox("仅在战斗中显示##Show Only In Combat", ref Settings.ShowTimelineOnlyInCombat);
            ImGui.Checkbox("过场动画中隐藏##Hide In Cutscene", ref Settings.HideTimelineInCutscene);
            ImGui.Checkbox("任务事件中隐藏##Hide In Quest Event", ref Settings.HideTimelineInQuestEvent);
            ImGui.Checkbox("在聊天栏输出 GCD 卡顿时间##Print Clipping Time On Chat", ref Settings.PrintClipping);
            if (Settings.PrintClipping)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * _scale);
                ImGui.DragIntRange2("卡顿范围（毫秒）##Clipping Range", ref Settings.PrintClippingMin, ref Settings.PrintClippingMax);
            }

            ImGui.NewLine();
            
            // Clear All Data button with warning color
            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF4444AA);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF3333DD);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF5555BB);
            if (ImGui.Button("清空所有时间轴数据##Clear All Timeline Data"))
            {
                TimelineManager.Instance?.ClearAllData();
                Svc.Chat.Print("[ActionTimelineReborn] 已清空所有时间轴数据。");
            }
            ImGui.PopStyleColor(3);
            DrawHelper.SetTooltip("清空所有已记录的时间轴数据和缓存。此操作会释放内存并重置时间轴。");
            
            ImGui.NewLine();
            ImGui.Checkbox("记录目标状态##Record Target Status", ref Settings.RecordTargetStatus);

            var index = 0;

            if(ImGui.CollapsingHeader("已记录过的状态##Showed Statuses"))
            {
                foreach (var statusId in TimelineManager.ShowedStatusId)
                {
                    var status = Svc.Data.GetExcelSheet<Status>()?.GetRow(statusId);
                    var texture = DrawHelper.GetTextureFromIconId(status?.Icon ?? 0);
                    if (texture != null)
                    {
                        ImGui.Image(texture.Handle, new Vector2(18, 24));
                        var tips = $"{status?.Name ?? string.Empty} [{status?.RowId ?? 0}]";
                        DrawHelper.SetTooltip(tips);
                        if (++index % 10 != 0) ImGui.SameLine();
                    }
                }
            }

            ImGui.SameLine();
            ImGui.NewLine();

            ImGui.Text("不要记录以下状态。");

            if (ImGui.BeginChild("ExceptStatus", new Vector2(0f, -1f), true))
            {
                ushort removeId = 0, addId = 0;
                index = 0;
                foreach (var statusId in Plugin.Settings.HideStatusIds)
                {
                    var status = Svc.Data.GetExcelSheet<Status>()?.GetRow(statusId);
                    var texture = DrawHelper.GetTextureFromIconId(status?.Icon ?? 0);
                    if (texture != null)
                    {
                        ImGui.Image(texture.Handle, new Vector2(24, 30));
                        DrawHelper.SetTooltip(status?.Name.ToString() ?? string.Empty);
                        ImGui.SameLine();
                    }

                    var id = statusId.ToString();
                    ImGui.SetNextItemWidth(100 * _scale);
                    if (ImGui.InputText($"##Status{index++}", ref id, 8) && ushort.TryParse(id, out var newId))
                    {
                        removeId = statusId;
                        addId = newId;
                    }

                    ImGui.SameLine();

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button($"{FontAwesomeIcon.Ban.ToIconString()}##Remove{statusId}"))
                    {
                        removeId = statusId;
                    }
                    ImGui.PopFont();
                }
                var oneId = string.Empty;
                ImGui.SetNextItemWidth(100 * _scale);
                if (ImGui.InputText($"##AddOne", ref oneId, 8) && ushort.TryParse(oneId, out var newOneId))
                {
                    _aboutAdd = newOneId;
                }
                ImGui.SameLine();

                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{FontAwesomeIcon.Plus.ToIconString()}##AddNew"))
                {
                    addId = _aboutAdd;
                }
                ImGui.PopFont();

                if (removeId != 0)
                {
                    Plugin.Settings.HideStatusIds.Remove(removeId);
                }
                if (addId != 0)
                {
                    Plugin.Settings.HideStatusIds.Add(addId);
                }
                ImGui.EndChild();
            }
        }

        #region Timeline
        private bool DrawTimelineSetting(DrawingSettings settings)
        {
            var result = false;
            if (!ImGui.BeginTabBar("##Timeline_Settings_TabBar"))
            {
                return result;
            }

            ImGui.PushItemWidth(80 * _scale);

            // general
            if (ImGui.BeginTabItem("常规##Timeline_General"))
            {
                result = DrawGeneralTab(settings);
                ImGui.EndTabItem();
            }

            // icons
            if (ImGui.BeginTabItem("图标##Timeline_Icons"))
            {
                DrawIconsTab(settings);
                ImGui.EndTabItem();
            }

            // casts
            if (ImGui.BeginTabItem("条形##Timeline_Bar"))
            {
                DrawBarTab(settings);
                ImGui.EndTabItem();
            }

            // grid
            if (ImGui.BeginTabItem("网格##Timeline_Grid"))
            {
                DrawGridTab(settings);
                ImGui.EndTabItem();
            }

            // gcd clipping
            if (ImGui.BeginTabItem("GCD 卡顿##Timeline_GCD"))
            {
                DrawGCDClippingTab(settings);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();

            return result;
        }

        private string _undoName = string.Empty;
        private DateTime _lastTime = DateTime.MinValue;
        private bool RemoveValue(string name)
        {
            ImGui.SameLine();

            bool isLast = name == _undoName && DateTime.Now - _lastTime < TimeSpan.FromSeconds(2);
            bool isTime = DateTime.Now - _lastTime > TimeSpan.FromSeconds(0.5);

            bool result = false;

            if (isLast) ImGui.PushStyleColor(ImGuiCol.Text, isTime ? ImGuiColors.HealerGreen : ImGuiColors.DPSRed);

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{(isLast ? FontAwesomeIcon.Check : FontAwesomeIcon.Ban).ToIconString()}##Remove{name}"))
            {
                if (isLast && isTime)
                {
                    result = true;
                    _lastTime = DateTime.MinValue;
                }
                else
                {
                    _lastTime = DateTime.Now;
                    _undoName = name;
                }
            }

            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(!isTime ? "请稍等一秒。" :
                isLast ? "确定要删除这个时间轴吗？"
                : "点击删除这个时间轴。");
            }

            if (isLast) ImGui.PopStyleColor();
            return result;
        }

        private bool DrawGeneralTab(DrawingSettings settings)
        {
            ImGui.InputText("名称##Name", ref settings.Name, 32);
            var result = Plugin.Settings.TimelineSettings.Count > 1 && RemoveValue(settings.Name);

            ImGui.Checkbox("启用##Enable", ref settings.Enable);
            ImGui.Checkbox("循环轴模式##Is Rotation", ref settings.IsRotation);
            ImGui.Checkbox("横向显示##Is Horizonal", ref settings.IsHorizonal);
            ImGui.Checkbox("反向显示##Is Reverse", ref settings.IsReverse);

            ImGui.NewLine();

            ImGui.Checkbox("锁定##Locked", ref settings.Locked);
            ImGui.ColorEdit4("锁定背景色##Locked Color", ref settings.LockedBackgroundColor, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("未锁定背景色##Unlocked Color", ref settings.UnlockedBackgroundColor, ImGuiColorEditFlags.NoInputs);

            ImGui.NewLine();

            ImGui.DragFloat("每秒长度##Size per second", ref settings.SizePerSecond, 0.3f, 20, 150);
            DrawHelper.SetTooltip("时间轴中每秒占用的像素长度。");

            ImGui.DragInt("时间偏移（秒）##Offset Time (seconds)", ref settings.TimeOffsetSetting, 0.1f, 0, 1000);
            DrawHelper.SetTooltip(settings.IsRotation ? "循环轴模式的时间偏移。"
                : "技能使用点相对时间轴起始线的提前时间。");

            ImGui.DragFloat("绘制中心偏移##Drawing Center offset", ref settings.CenterOffset, 0.3f, -500, 500);

            return result;
        }

        private static void DrawIconsTab(DrawingSettings settings)
        {
            ImGui.DragInt("GCD 图标大小##Icon Size", ref settings.GCDIconSize);

            ImGui.NewLine();
            ImGui.Checkbox("显示能力技 / oGCD##Show Off GCD", ref settings.ShowOGCD);

            if (settings.ShowOGCD)
            {
                ImGui.Indent();
                ImGui.DragInt("oGCD 图标大小##Off GCD Icon Size", ref settings.OGCDIconSize, 0.2f, 1, 100);
                ImGui.DragFloat("oGCD 垂直偏移##Iff GCD Vertical Offset", ref settings.OGCDOffset, 0.01f, 0, 1);
                ImGui.Unindent();
            }

            ImGui.NewLine();
            ImGui.Checkbox("显示自动攻击##Show Auto Attacks", ref settings.ShowAutoAttack);

            if (settings.ShowAutoAttack)
            {
                ImGui.Indent();
                ImGui.DragInt("自动攻击图标大小##Auto Attack Icon Size", ref settings.AutoAttackIconSize, 0.2f, 1, 100);
                ImGui.DragFloat("自动攻击垂直偏移##Auto Attack Vertical Offset", ref settings.AutoAttackOffset, 0.01f, 0, 1);
                ImGui.Unindent();
            }

            ImGui.NewLine();
            ImGui.Checkbox("显示状态获得/失去##Show Status Gain Lose", ref settings.ShowStatus);

            if (settings.ShowStatus)
            {
                ImGui.Indent();
                ImGui.DragInt("状态图标大小##Status Icon Size", ref settings.StatusIconSize, 0.2f, 1, 100);
                ImGui.DragFloat("状态图标透明度##Status Icon Alpha", ref settings.StatusIconAlpha, 0.01f, 0, 1);
                ImGui.ColorEdit4("获得状态颜色##Status Gain Color", ref settings.StatusGainColor, ImGuiColorEditFlags.NoInputs);
                ImGui.ColorEdit4("失去状态颜色##Status Lose Color", ref settings.StatusLoseColor, ImGuiColorEditFlags.NoInputs);
                ImGui.DragFloat("状态图标偏移##Status Offset", ref settings.StatusOffset, 0.01f, 0, 1);
                ImGui.Unindent();
            }

            ImGui.NewLine();
            ImGui.Checkbox("显示暴击/直击类型##Show Damage Type", ref settings.ShowDamageType);
            if (settings.ShowDamageType)
            {
                ImGui.Indent();
                ImGui.ColorEdit4("直击颜色##Direct Color", ref settings.DirectColor, ImGuiColorEditFlags.NoInputs);
                ImGui.ColorEdit4("暴击颜色##Critical Color", ref settings.CriticalColor, ImGuiColorEditFlags.NoInputs);
                ImGui.ColorEdit4("暴击直击颜色##Critical Direct Color", ref settings.CriticalDirectColor, ImGuiColorEditFlags.NoInputs);
                ImGui.Unindent();
            }
        }

        private static void DrawBarTab(DrawingSettings settings)
        {
            ImGui.ColorEdit4("条形背景色##Bar Background Color", ref settings.BackgroundColor, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("GCD 边框颜色##GCD Border Color", ref settings.GCDBorderColor, ImGuiColorEditFlags.NoInputs);
            ImGui.DragFloat("GCD 边框粗细##GCD Border Thickness", ref settings.GCDThickness, 0.01f, 0, 10);
            ImGui.DragFloat("GCD 边框圆角##GCD Border Round", ref settings.GCDRound, 0.01f, 0, 10);
            ImGui.DragFloatRange2("GCD 条高度##GCD Bar Height", ref settings.GCDHeightLow, ref settings.GCDHeightHigh, 0.01f, 0, 1);
            ImGui.NewLine();

            ImGui.ColorEdit4("施法中颜色##Cast In Progress Color", ref settings.CastInProgressColor, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("施法完成颜色##Cast Finished Color", ref settings.CastFinishedColor, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("施法取消颜色##Cast Canceled Color", ref settings.CastCanceledColor, ImGuiColorEditFlags.NoInputs);

            ImGui.NewLine();

            ImGui.Checkbox("显示动画锁时间##Show Animation Lock Time", ref settings.ShowAnimationLock);

            if (settings.ShowAutoAttack)
            {
                ImGui.Indent();
                ImGui.ColorEdit4("动画锁颜色##Animation Lock Color", ref settings.AnimationLockColor, ImGuiColorEditFlags.NoInputs);
                ImGui.Unindent();
            }

            ImGui.NewLine();

            ImGui.Checkbox("显示状态持续条##Show Status Line", ref settings.ShowStatusLine);

            if (settings.ShowAutoAttack)
            {
                ImGui.Indent();
                ImGui.DragFloat("状态持续条高度##Status Line Height", ref settings.StatusLineSize, 0.2f, 1, 100);
                ImGui.Unindent();
            }
        }

        private static void DrawGridTab(DrawingSettings settings)
        {
            ImGui.Checkbox("启用##Grid_Enabled", ref settings.ShowGrid);

            ImGui.DragFloat("起始线宽度##Start Line Width", ref settings.GridStartLineWidth, 0.1f, 0.1f, 10);
            ImGui.ColorEdit4("起始线颜色##Start Line Color", ref settings.GridStartLineColor, ImGuiColorEditFlags.NoInputs);

            if (!settings.ShowGrid) { return; }
            ImGui.NewLine();

            ImGui.Checkbox("显示中心线##Show Center Line", ref settings.ShowGridCenterLine);
            if (settings.ShowGridCenterLine)
            {
                ImGui.Indent();
                ImGui.DragFloat("中心线宽度##Center Line Width", ref settings.GridCenterLineWidth, 0.1f, 0.1f, 10);
                ImGui.ColorEdit4("中心线颜色##Center Line Color", ref settings.GridCenterLineColor, ImGuiColorEditFlags.NoInputs);
                ImGui.Unindent();
            }

            ImGui.NewLine();

            ImGui.DragFloat("网格线宽度##Line Width", ref settings.GridLineWidth, 0.1f, 0.1f, 10);
            ImGui.ColorEdit4("网格线颜色##Line Color", ref settings.GridLineColor, ImGuiColorEditFlags.NoInputs);

            ImGui.NewLine();
            ImGui.Checkbox("按秒划分##Divide By Seconds", ref settings.GridDivideBySeconds);

            if (!settings.GridDivideBySeconds) { return; }

            ImGui.Checkbox("显示秒数文字##Show Text", ref settings.GridShowSecondsText);

            ImGui.NewLine();
            ImGui.Checkbox("秒内细分##Sub-Divide By Seconds", ref settings.GridSubdivideSeconds);

            if (!settings.GridSubdivideSeconds) { return; }

            ImGui.DragInt("细分数量##Sub-Division Count", ref settings.GridSubdivisionCount, 0.2f, 2, 8);
            ImGui.DragFloat("细分线宽度##Sub-Division Line Width", ref settings.GridSubdivisionLineWidth, 0.5f, 1, 5);
            ImGui.ColorEdit4("细分线颜色##Sub-Division Line Color", ref settings.GridSubdivisionLineColor, ImGuiColorEditFlags.NoInputs);
        }

        private static void DrawGCDClippingTab(DrawingSettings settings)
        {
            ImGui.Checkbox("启用##GCDClipping_Enabled", ref settings.ShowGCDClippingSetting);
            DrawHelper.SetTooltip("仅在非循环轴模式的时间轴中显示。");

            if (!settings.ShowGCDClipping) return;

            int clippingThreshold = (int)(settings.GCDClippingThreshold * 1000f);
            if (ImGui.DragInt("阈值（毫秒）##Threshold (ms)", ref clippingThreshold, 0.1f, 0, 1000))
            {
                settings.GCDClippingThreshold = clippingThreshold / 1000f;
            }
            DrawHelper.SetTooltip("用于过滤由延迟或其他因素造成的“误报”。短于该数值的 GCD 卡顿会被忽略。\n强烈建议根据自己的网络和设备环境测试合适的数值。");

            ImGui.DragInt("最大时间（秒）##Max Time (seconds)", ref settings.GCDClippingMaxTime, 0.1f, 3, 60);
            DrawHelper.SetTooltip("长于该数值的 GCD 卡顿会被忽略。");

            ImGui.ColorEdit4("颜色##Color", ref settings.GCDClippingColor, ImGuiColorEditFlags.NoInputs);

            ImGui.ColorEdit4("文字颜色##Text Color", ref settings.GCDClippingTextColor, ImGuiColorEditFlags.NoInputs);
        }
        #endregion
    }
}
