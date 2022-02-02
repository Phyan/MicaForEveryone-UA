﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Vanara.PInvoke;

using MicaForEveryone.Models;
using MicaForEveryone.Interfaces;
using MicaForEveryone.Win32;

namespace MicaForEveryone.Services
{
    internal class RuleHandler : IRuleService
    {
        public void ApplyRuleToWindow(HWND windowHandle, IRule rule)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Applying rule `{rule}` to `{windowHandle.GetText()}` ({windowHandle.GetClassName()}, {windowHandle.GetProcessName()})");
#endif
            if (rule.ExtendFrameIntoClientArea)
                windowHandle.ExtendFrameIntoClientArea();

            windowHandle.ApplyBackdropRule(rule.BackdropPreference);
            windowHandle.ApplyTitlebarColorRule(rule.TitlebarColor, SystemTitlebarColorMode);
        }

        private readonly IConfigService _configService;
        private readonly User32.EnumWindowsProc _enumWindows;

        public RuleHandler(IConfigService configService)
        {
            _configService = configService;
            _configService.ConfigSource.Changed += ConfigSource_Changed;
            _configService.Updated += ConfigService_Updated;
            _enumWindows = (windowHandle, _) =>
            {
                if (!windowHandle.IsOwned())
                    MatchAndApplyRuleToWindow(windowHandle);
                return true;
            };
        }

        ~RuleHandler()
        {
            _configService.ConfigSource.Changed -= ConfigSource_Changed;
        }

        public TitlebarColorMode SystemTitlebarColorMode { get; set; }

        public void MatchAndApplyRuleToWindow(HWND windowHandle)
        {
            var applicableRules = _configService.Rules.Where(rule => rule.IsApplicable(windowHandle));

            if (!applicableRules.Any(rule => rule is GlobalRule))
                return;

            var rule = applicableRules.FirstOrDefault(rule => rule is not GlobalRule) ??
                applicableRules.FirstOrDefault();

            if (rule == null)
                return;

            ApplyRuleToWindow(windowHandle, rule);
        }

        public void MatchAndApplyRuleToAllWindows()
        {
            User32.EnumWindows(_enumWindows, IntPtr.Zero);
        }

        private async void ConfigSource_Changed(object sender, EventArgs e)
        {
            await _configService.LoadAsync();
        }

        private async void ConfigService_Updated(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                MatchAndApplyRuleToAllWindows();
            });
        }
    }
}
