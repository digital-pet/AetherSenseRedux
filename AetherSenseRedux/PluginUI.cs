﻿using AetherSenseRedux.Pattern;
using AetherSenseRedux.Trigger;
using Dalamud.Logging;
using Dalamud.Interface.Raii;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using XIVChatTypes;

namespace AetherSenseRedux
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUi : IDisposable
    {
        private Configuration _configuration;
        private Plugin _plugin;

        private bool _settingsVisible = false;
        public bool SettingsVisible
        {
            get { return _settingsVisible; }
            set { _settingsVisible = value; }
        }

        private int _selectedTrigger = 0;
        private int _selectedFilterCategory = 0;

        // In order to keep the UI from trampling all over the configuration as changes are being made, we keep a working copy here when needed.
        private Configuration? _workingCopy;

        public PluginUi(Configuration configuration, Plugin plugin)
        {
            this._configuration = configuration;
            this._plugin = plugin;
        }

        /// <summary>
        /// Would dispose of any unmanaged resources if we used any here. Since we don't, we probably don't need this.
        /// </summary>
        public void Dispose()
        {

        }

        /// <summary>
        /// Draw handler for plugin UI
        /// </summary>
        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.

            DrawSettingsWindow();
        }

        /// <summary>
        /// Draws the settings window and does a little housekeeping with the working copy of the config. 
        /// </summary>
        private void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                
                // if we aren't drawing the window we don't need a working copy of the configuration
                if (_workingCopy != null)
                {
                    PluginLog.Debug("Making WorkingCopy null.");
                    _workingCopy = null;
                }

                return;
            }

            // we can only get here if we know we're going to draw the settings window, so let's get our working copy back

            if (_workingCopy == null)
            {
                PluginLog.Debug("WorkingCopy was null, importing current config.");
                _workingCopy = new Configuration();
                _workingCopy.Import(_configuration);
            }

            ////
            ////    SETTINGS WINDOW
            ////
            ImGui.SetNextWindowSize(new Vector2(640, 400), ImGuiCond.Appearing);
            if (ImGui.Begin("AetherSense Redux", ref _settingsVisible, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.MenuBar))
            {
                ////
                ////    MENU BAR
                ////
                
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("File"))
                    {
                        ImGui.MenuItem("Import...", "", false, false);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("NOT IMPLEMENTED");
                        }
                        ImGui.MenuItem("Export...", "", false, false);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("NOT IMPLEMENTED");
                        }
                        ImGui.EndMenu();
                    }
                    ImGui.EndMenuBar();
                }

                ////
                ////    BODY
                ////
                ImGui.BeginChild("body", new Vector2(0, -ImGui.GetFrameHeightWithSpacing()), false);
                
                ImGui.Indent(1); //for some reason the UI likes to cut off a pixel on the left side if we don't do this

                if (ImGui.BeginTabBar("MyTabBar", ImGuiTabBarFlags.None))
                {
                    if (ImGui.BeginTabItem("Intiface"))
                    {
                        var address = _workingCopy.Address;
                        if (ImGui.InputText("Intiface Address", ref address, 64))
                        {
                            _workingCopy.Address = address;
                        }
                        ImGui.SameLine();
                        if (_plugin.Status == ButtplugStatus.Connected)
                        {
                            if (ImGui.Button("Disconnect"))
                            {
                                _plugin.Stop(true);
                            }
                        }
                        else if (_plugin.Status == ButtplugStatus.Connecting || _plugin.Status == ButtplugStatus.Disconnecting)
                        {
                            if (ImGui.Button("Wait..."))
                            {

                            }
                        }
                        else 
                        {
                            if (ImGui.Button("Connect"))
                            {
                                _configuration.Address = _workingCopy.Address;
                                _plugin.Start();
                            }
                        }

                        //if (ImGui.Button(plugin.Scanning ? "Scanning..." : "Scan Now")){
                        //    if (!plugin.Scanning)
                        //    {
                        //        Task.Run(plugin.DoScan);
                        //    }
                        //}

                        ImGui.Spacing();
                        ImGui.BeginChild("status", new Vector2(0, 0), true);
                        if (_plugin.WaitType == WaitType.Slow_Timer)
                        {
                            ImGui.TextColored(new Vector4(1,0,0,1), "High resolution timers not available, patterns will be inaccurate.");
                        }
                        ImGui.Text("Connection Status:");
                        ImGui.Indent();
                        ImGui.Text(_plugin.Status == ButtplugStatus.Connected ? "Connected" : _plugin.Status == ButtplugStatus.Connecting ? "Connecting..." : _plugin.Status == ButtplugStatus.Error ? "Error" : "Disconnected");
                        if (_plugin.LastException != null)
                        {
                            ImGui.Text(_plugin.LastException.Message);
                        }
                        ImGui.Unindent();
                        if (_plugin.Status == ButtplugStatus.Connected)
                        {
                            ImGui.Text("Devices Connected:");
                            ImGui.Indent();
                            foreach (var device in _plugin.ConnectedDevices)
                            {
                                ImGui.Text(String.Format("{0} [{1}]",device.Key,(int)device.Value));
                            }
                            ImGui.Unindent();
                        }

                        ImGui.EndChild();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Triggers"))
                    {
                        ImGui.BeginChild("leftouter", new Vector2(155,0));
                        ImGui.Indent(1);
                        ImGui.BeginChild("left", new Vector2(0, -ImGui.GetFrameHeightWithSpacing()), true);
                            
                        foreach (var (t, i) in _workingCopy.Triggers.Select((value, i) => (value, i)))
                        {
                            ImGui.PushID(i); // We push the iterator to the ID stack so multiple triggers of the same type and name are still distinct
                            if (ImGui.Selectable(String.Format("{0} ({1})", t.Name, t.Type), _selectedTrigger == i))
                            {
                                _selectedTrigger = i;
                            }
                            ImGui.PopID();
                        }

                        ImGui.EndChild();
                        if (ImGui.Button("Add New"))
                        {
                            List<dynamic> triggers = _workingCopy.Triggers;
                            triggers.Add(new ChatTriggerConfig()
                            {
                                PatternSettings = new ConstantPatternConfig()
                            });
                        }
                        ImGui.SameLine();
                        if (ImGui.Button("Remove"))
                        {
                            _workingCopy.Triggers.RemoveAt(_selectedTrigger);
                            if (_selectedTrigger >= _workingCopy.Triggers.Count)
                            {
                                _selectedTrigger = (_selectedTrigger > 0) ? _workingCopy.Triggers.Count - 1 : 0;
                            }
                        }

                        ImGui.EndChild();
                        ImGui.SameLine();
                            
                        ImGui.BeginChild("right", new Vector2(0, 0), false);
                        ImGui.Indent(1);
                        if (_workingCopy.Triggers.Count == 0)
                        {
                            ImGui.Text("Use the Add New button to add a trigger.");

                        } 
                        else
                        {
                            DrawChatTriggerConfig(_workingCopy.Triggers[_selectedTrigger]);
                        }
                        ImGui.Unindent();
                        ImGui.EndChild();

                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Advanced"))
                    {
                        var configValue = _workingCopy.LogChat;
                        if (ImGui.Checkbox("Log Chat to Debug", ref configValue))
                        {
                            _workingCopy.LogChat = configValue;

                        }
                        if (ImGui.Button("Restore Default Triggers"))
                        {
                            _workingCopy.LoadDefaults();
                        }
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                }

                ImGui.Unindent(1); //for some reason the UI likes to cut off a pixel on the left side if we don't do this

                ImGui.EndChild();
                
                ////
                ////    FOOTER
                ////
                // save button
                if (ImGui.Button("Save"))
                {
                    _configuration.Import(_workingCopy);
                    _configuration.Save();
                    _plugin.Reload();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Save configuration changes to disk.");
                }
                // end save button
                ImGui.SameLine();
                // apply button
                if (ImGui.Button("Apply"))
                {
                    _configuration.Import(_workingCopy);
                    _plugin.Reload();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Apply configuration changes without saving.");
                }
                // end apply button
                ImGui.SameLine();
                // revert button
                if (ImGui.Button("Revert"))
                {
                    try
                    {
                        var cloneconfig = _configuration.CloneConfigurationFromDisk();
                        _configuration.Import(cloneconfig);
                        _workingCopy.Import(_configuration);
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error(ex, "Could not restore configuration.");
                    }

                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Discard all changes and reload the configuration from disk.");
                }
                // end revert button

                //ImGui.SameLine();
                //if (ImGui.Button("Run Benchmark"))
                //{
                //    var t = Plugin.DoBenchmark();
                //}
            }

            ImGui.End();
        }

        /// <summary>
        /// Draws the configuration interface for chat triggers
        /// </summary>
        /// <param name="t">A ChatTriggerConfig object containing the current configuration for the trigger.</param>
        private void DrawChatTriggerConfig(dynamic t)
        {
            using var triggerConfigTabBar = ImRaii.TabBar("TriggerConfig", ImGuiTabBarFlags.None);
            if (triggerConfigTabBar) {
                DrawChatTriggerBasicTab(t);

                DrawTriggerDevicesTab(t);

                if (t.UseFilter) {
                    DrawChatTriggerFilterTab(t);
                }

                DrawTriggerPatternTab(t);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        private void DrawChatTriggerBasicTab(ChatTriggerConfig t)
        {
            using var basicTab = ImRaii.TabItem("Basic");
            if (basicTab)
            {

                //begin name field
                var name = (string)t.Name;
                if (ImGui.InputText("Name", ref name, 64))
                {
                    t.Name = name;
                }
                //end name field

                //begin regex field
                var regex = (string)t.Regex;
                if (ImGui.InputText("Regex", ref regex, 255))
                {
                    t.Regex = regex;
                }
                //end regex field

                //begin retrigger delay field
                var retriggerdelay = (int)t.RetriggerDelay;
                if (ImGui.InputInt("Retrigger Delay (ms)", ref retriggerdelay))
                {
                    t.RetriggerDelay = (long)retriggerdelay;
                }
                //end retrigger delay field
                var usefilter = (bool)t.UseFilter;
                if (ImGui.Checkbox("Use Filters", ref usefilter))
                {
                    t.UseFilter = usefilter;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        private void DrawTriggerDevicesTab(TriggerConfig t)
        {
            ////
            ////    DEVICES TAB
            ////
            using var devicesTab = ImRaii.TabItem("Devices");
            if (devicesTab) {
                //Begin enabled devices selection
                _workingCopy!.SeenDevices = new List<string>(_configuration.SeenDevices);
                if (_workingCopy.SeenDevices.Count > 0) {
                    bool[] selected = new bool[_workingCopy.SeenDevices.Count];
                    bool   modified = false;
                    foreach (var (device, j) in _workingCopy.SeenDevices.Select((value, i) => (value, i))) {
                        if (t.EnabledDevices.Contains(device)) {
                            selected[j] = true;
                        } else {
                            selected[j] = false;
                        }
                    }

                    using var devicesListBox = ImRaii.ListBox("Enabled Devices");
                    if (devicesListBox) {
                        foreach (var (device, j) in _workingCopy.SeenDevices.Select((value, i) => (value, i))) {
                            if (ImGui.Selectable(device, selected[j])) {
                                selected[j] = !selected[j];
                                modified    = true;
                            }
                        }
                    }

                    if (modified) {
                        var toEnable = new List<string>();
                        foreach (var (device, j) in _workingCopy.SeenDevices.Select((value, i) => (value, i))) {
                            if (selected[j]) {
                                toEnable.Add(device);
                            }
                        }

                        t.EnabledDevices = toEnable;
                    }
                } else {
                    ImGui.Text("Connect to Intiface and connect devices to populate the list.");
                }
                //end enabled devices selection
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        private void DrawChatTriggerFilterTab(ChatTriggerConfig t)
        {
            using var filtersTab = ImRaii.TabItem("Filters");
            if (!filtersTab)
                return;

            using var filterCategoryCombo =
                ImRaii.Combo("##filtercategory", XIVChatFilter.FilterCategoryNames[_selectedFilterCategory]);
            if (filterCategoryCombo) {
                var k = 0;
                foreach (string name in XIVChatFilter.FilterCategoryNames) {
                    if (name == "GM Messages") {
                        // don't show the GM chat options for this filter configuration.
                        k++;
                        continue;
                    }

                    bool isSelected = k == _selectedFilterCategory;
                    if (ImGui.Selectable(name, isSelected)) {
                        _selectedFilterCategory = k;
                    }

                    k++;
                }
            }

            using var filterListChild = ImRaii.Child("filtercatlist", new Vector2(0, 0));
            if (filterListChild) {
                var  i        = 0;
                bool modified = false;
                foreach (string name in XIVChatFilter.FilterNames[_selectedFilterCategory]) {
                    if (name == "Novice Network" || name == "Novice Network Notifications") {
                        // don't show novice network as selectable filters either.
                        i++;
                        continue;
                    }

                    bool filtersetting = t.FilterTable[_selectedFilterCategory][i];

                    if (ImGui.Checkbox(name, ref filtersetting)) {
                        modified = true;
                    }

                    if (modified) {
                        t.FilterTable[_selectedFilterCategory][i] = filtersetting;
                    }

                    i++;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        private void DrawTriggerPatternTab(TriggerConfig t)
        {
            using var patternTab = ImRaii.TabItem("Pattern");
            if (!patternTab)
                return;
            
            
            string[] patterns = { "Constant", "Ramp", "Random", "Square", "Saw" };

            //begin pattern selection
            if (ImGui.BeginCombo("##combo", t.PatternSettings!.Type)) {
                foreach (string pattern in patterns) {
                    bool isSelected = t.PatternSettings.Type == pattern;
                    if (ImGui.Selectable(pattern, isSelected)) {
                        if (t.PatternSettings.Type != pattern) {
                            t.PatternSettings = PatternFactory.GetDefaultsFromString(pattern);
                        }
                    }

                    if (isSelected) {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }
            //end pattern selection

            ImGui.SameLine();

            //begin test button
            if (ImGui.ArrowButton("test", ImGuiDir.Right)) {
                _plugin.DoPatternTest(t.PatternSettings);
            }

            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Preview pattern on all devices.");
            }
            //end test button

            ImGui.Indent();

            //begin pattern settings
            switch ((string)t.PatternSettings.Type) {
                case "Constant":
                    DrawConstantPatternSettings(t.PatternSettings);
                    break;
                case "Ramp":
                    DrawRampPatternSettings(t.PatternSettings);
                    break;
                case "Saw":
                    DrawSawPatternSettings(t.PatternSettings);
                    break;
                case "Random":
                    DrawRandomPatternSettings(t.PatternSettings);
                    break;
                case "Square":
                    DrawSquarePatternSettings(t.PatternSettings);
                    break;
                default:
                    //we should never get here but just in case
                    ImGui.Text("Select a valid pattern.");
                    break;
            }
            //end pattern settings

            ImGui.Unindent();
        }

        /// <summary>
        /// Draws the configuration interface for constant patterns
        /// </summary>
        /// <param name="pattern">A ConstantPatternConfig object containing the current configuration for the pattern.</param>
        private static void DrawConstantPatternSettings(dynamic pattern)
        {
            int duration = (int)pattern.Duration;
            if (ImGui.InputInt("Duration (ms)", ref duration))
            {
                pattern.Duration = (long)duration;
            }
            double level = (double)pattern.Level;
            if (ImGui.InputDouble("Level", ref level))
            {
                pattern.Level = level;
            }
        }

        /// <summary>
        /// Draws the configuration interface for ramp patterns
        /// </summary>
        /// <param name="pattern">A RampPatternConfig object containing the current configuration for the pattern.</param>
        private static void DrawRampPatternSettings(dynamic pattern)
        {
            int duration = (int)pattern.Duration;
            if (ImGui.InputInt("Duration (ms)", ref duration))
            {
                pattern.Duration = (long)duration;
            }
            double start = (double)pattern.Start;
            if (ImGui.InputDouble("Start", ref start))
            {
                pattern.Start = start;
            }
            double end = (double)pattern.End;
            if (ImGui.InputDouble("End", ref end))
            {
                pattern.End = end;
            }
        }

        /// <summary>
        /// Draws the configuration interface for saw patterns
        /// </summary>
        /// <param name="pattern">A SawPatternConfig object containing the current configuration for the pattern.</param>
        private static void DrawSawPatternSettings(dynamic pattern)
        {
            int duration = (int)pattern.Duration;
            if (ImGui.InputInt("Duration (ms)", ref duration))
            {
                pattern.Duration = (long)duration;
            }
            double start = (double)pattern.Start;
            if (ImGui.InputDouble("Start", ref start))
            {
                pattern.Start = start;
            }
            double end = (double)pattern.End;
            if (ImGui.InputDouble("End", ref end))
            {
                pattern.End = end;
            }
            int duration1 = (int)pattern.Duration1;
            if (ImGui.InputInt("Saw Duration (ms)", ref duration1))
            {
                pattern.Duration1 = (long)duration1;
            }
        }

        /// <summary>
        /// Draws the configuration interface for random patterns
        /// </summary>
        /// <param name="pattern">A RandomPatternConfig object containing the current configuration for the pattern.</param>
        private static void DrawRandomPatternSettings(dynamic pattern)
        {
            int duration = (int)pattern.Duration;
            if (ImGui.InputInt("Duration (ms)", ref duration))
            {
                pattern.Duration = (long)duration;
            }
            double min = (double)pattern.Minimum;
            if (ImGui.InputDouble("Minimum", ref min))
            {
                pattern.Minimum = min;
            }
            double max = (double)pattern.Maximum;
            if (ImGui.InputDouble("Maximum", ref max))
            {
                pattern.Maximum = max;
            }
        }

        /// <summary>
        /// Draws the configuration interface for square patterns
        /// </summary>
        /// <param name="pattern">A SquarePatternConfig object containing the current configuration for the pattern.</param>
        private static void DrawSquarePatternSettings(dynamic pattern)
        {
            int duration = (int)pattern.Duration;
            if (ImGui.InputInt("Duration (ms)", ref duration))
            {
                pattern.Duration = (long)duration;
            }
            double level1 = (double)pattern.Level1;
            if (ImGui.InputDouble("Level 1", ref level1))
            {
                pattern.Level1 = level1;
            }
            int duration1 = (int)pattern.Duration1;
            if (ImGui.InputInt("Level 1 Duration (ms)", ref duration1))
            {
                pattern.Duration1 = (long)duration1;
            }
            double level2 = (double)pattern.Level2;
            if (ImGui.InputDouble("Level 2", ref level2))
            {
                pattern.Level2 = level2;
            }
            int duration2 = (int)pattern.Duration2;
            if (ImGui.InputInt("Level 2 Duration (ms)", ref duration2))
            {
                pattern.Duration2 = (long)duration2;
            }
            int offset = (int)pattern.Offset;
            if (ImGui.InputInt("Offset (ms)", ref offset))
            {
                pattern.Offset = (long)offset;
            }
        }
    }
}
