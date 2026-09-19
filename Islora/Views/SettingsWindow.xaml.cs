using System;
using System.Windows;
using Islora.Models;
using Islora.Services;
using Islora.Theme;

namespace Islora.Views;

/// <summary>设置窗口：主题模式 / 日夜间 / 自启 / 自动更新 / 语言。</summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        Populate();
        ApplyLanguage();
        LanguageBox.SelectionChanged += (_, _) =>
        {
            // 选择语言时立即刷新整个窗口文字，保存后持久化
            LocalizationService.SetLanguage(LanguageBox.SelectedIndex == 1
                ? AppLanguage.English
                : AppLanguage.Chinese);
            ApplyLanguage();
        };
    }

    private void Populate()
    {
        var s = SettingsService.Current;

        ThemeModeBox.SelectedIndex = s.ThemeMode switch
        {
            ThemePreference.Light => 1,
            ThemePreference.Dark => 2,
            _ => 0,
        };

        for (int i = 0; i < 24; i++)
        {
            DayStartBox.Items.Add($"{i:00}:00");
            NightStartBox.Items.Add($"{i:00}:00");
        }
        DayStartBox.SelectedIndex = Math.Clamp(s.DayStartHour, 0, 23);
        NightStartBox.SelectedIndex = Math.Clamp(s.NightStartHour, 0, 23);

        AutoStartCheck.IsChecked = s.AutoStart;
        AutoUpdateCheck.IsChecked = s.AutoUpdate;
        WidgetsCheck.IsChecked = s.ShowSystemWidgets;
        NeonSpeedSlider.Value = Math.Clamp(s.NeonSpeedMs, 400, 2000);
        NeonSpeedLabel.Text = s.NeonSpeedMs + "ms";

        LanguageBox.SelectedIndex = s.Language == AppLanguage.English ? 1 : 0;
    }

    /// <summary>按当前语言刷新所有界面文字。</summary>
    private void ApplyLanguage()
    {
        Title = LocalizationService.T("Settings.Title");
        SubtitleText.Text = LocalizationService.T("Settings.Title");
        ThemeTitleText.Text = LocalizationService.T("Settings.Theme");
        ModeLabel.Text = LocalizationService.T("Settings.Mode");
        ModeAutoItem.Content = LocalizationService.T("Settings.ModeAuto");
        ModeLightItem.Content = LocalizationService.T("Settings.ModeLight");
        ModeDarkItem.Content = LocalizationService.T("Settings.ModeDark");
        DayStartLabel.Text = LocalizationService.T("Settings.DayStart");
        NightStartLabel.Text = LocalizationService.T("Settings.NightStart");
        SystemTitleText.Text = LocalizationService.T("Settings.System");
        AutoStartCheck.Content = LocalizationService.T("Settings.AutoStart");
        AutoUpdateCheck.Content = LocalizationService.T("Settings.AutoUpdate");
        WidgetsCheck.Content = LocalizationService.T("Settings.Widgets");
        LanguageLabel.Text = LocalizationService.T("Settings.Language");
        NeonTitleText.Text = LocalizationService.T("Settings.Neon");
        NeonHintText.Text = LocalizationService.T("Settings.NeonHint");
        CancelButton.Content = LocalizationService.T("Settings.Cancel");
        SaveButton.Content = LocalizationService.T("Settings.Save");
    }

    private void OnNeonSpeedChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        => NeonSpeedLabel.Text = ((int)e.NewValue) + "ms";

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var s = SettingsService.Current;
        s.ThemeMode = ThemeModeBox.SelectedIndex switch
        {
            1 => ThemePreference.Light,
            2 => ThemePreference.Dark,
            _ => ThemePreference.Auto,
        };
        s.DayStartHour = Math.Clamp(DayStartBox.SelectedIndex, 0, 23);
        s.NightStartHour = Math.Clamp(NightStartBox.SelectedIndex, 0, 23);
        s.AutoStart = AutoStartCheck.IsChecked == true;
        s.AutoUpdate = AutoUpdateCheck.IsChecked == true;
        s.ShowSystemWidgets = WidgetsCheck.IsChecked == true;
        s.NeonSpeedMs = (int)NeonSpeedSlider.Value;
        s.Language = LanguageBox.SelectedIndex == 1 ? AppLanguage.English : AppLanguage.Chinese;

        SettingsService.Save();
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        // 取消：把语言恢复到已保存的值（选择语言时是即时预览，不保存应回滚）
        LocalizationService.SetLanguage(SettingsService.Current.Language);
        DialogResult = false;
        Close();
    }
}
