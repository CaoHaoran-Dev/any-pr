using System.Runtime.InteropServices;
using System.Windows.Input;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using ProcessSH.Models;
using ProcessSH.Services;
using ProcessSH.Views;
using WinRT.Interop;
using Windows.Graphics;

namespace ProcessSH;

public sealed partial class MainWindow : Window
{
    private AppWindow _appWindow;
    private bool _hideOnDeactivate;
    private bool _trayMenuOpen;
    private bool _suppressHide;
    private bool _firstActivated;
    private DateTime _lastShownAt = DateTime.MinValue;

    // 子窗口单例
    private SettingsWindow? _settingsWindow;
    private AboutWindow? _aboutWindow;

    private const int WindowWidth = 520;
    private const int BaseHeight = 200;
    private const int MaxHeight = 500;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_SHOW = 5;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    public ICommand ToggleWindowCommand { get; }

    public MainWindow()
    {
        InitializeComponent();

        _appWindow = GetAppWindow();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        _appWindow.Resize(new SizeInt32(WindowWidth, BaseHeight));
        PositionWindowBottomLeft();
        _appWindow.IsShownInSwitchers = false;

        TrySetIcon();

        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        RootFrame.Navigate(typeof(MainPage));

        _hideOnDeactivate = AppSettings.Current.HideOnDeactivate;
        Activated += OnWindowActivated;
        Activated += OnFirstActivated;
        Closed += OnWindowClosed;

        ToggleWindowCommand = new RelayCommand(ToggleWindow);

        ApplyLocalization();
        Localization.LanguageChanged += ApplyLocalization;
    }

    private AppWindow GetAppWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    private void TrySetIcon()
    {
        try
        {
            var icon = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(icon))
                _appWindow.SetIcon(icon);
        }
        catch { }
    }
