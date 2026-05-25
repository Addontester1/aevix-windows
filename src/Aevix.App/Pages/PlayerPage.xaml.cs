using Aevix_App.Controls;
using Aevix_App.ViewModels;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

namespace Aevix_App.Pages;

/// <summary>
/// Player page with TV-style controls:
///   - Loading spinner shown while libVLC is buffering (popup hidden so
///     the WinUI ProgressRing is visible).
///   - Auto-hiding controls bar with play / stop / scrubber / time /
///     volume / mute / fullscreen.
///   - Fullscreen via AppWindow's FullScreenPresenter (F11 also toggles).
///   - Centre-pulse glyph that briefly appears on play/pause toggle so the
///     user gets visual feedback that their click registered (the video
///     paint side won't show it because it's a popup overlay).
/// </summary>
public sealed partial class PlayerPage : Page
{
    private const int ControlsAutoHideSeconds = 3;

    public PlayerViewModel Vm { get; }
    private PlayRequest? _request;
    private VideoChildWindow? _surface;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _positionTimer;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _autoHideTimer;
    private bool _userScrubbing;
    private bool _wasFullscreen;
    private OverlappedPresenter? _previousPresenter;

    public PlayerPage()
    {
        Vm = App.Services.GetRequiredService<PlayerViewModel>();
        InitializeComponent();
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Vm.Title)) TitleText.Text = Vm.Title;
        };
        Loaded += PlayerPage_Loaded;
        Unloaded += PlayerPage_Unloaded;
        KeyDown += PlayerPage_KeyDown;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _request = e.Parameter as PlayRequest;
    }

    // ---- Lifecycle -----------------------------------------------------

    private async void PlayerPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowLoading("Loading…");
            await Vm.Player.InitializeAsync();

            // Build the popup overlay surface AFTER VideoArea has actual size.
            _surface ??= new VideoChildWindow(App.MainWindowInstance);
            _surface.Track(VideoArea);
            // Hide initially — only show when first frame arrives.
            _surface.Hide();
            Vm.Player.MediaPlayer.Hwnd = _surface.Hwnd;
            HookMediaPlayerEvents(Vm.Player.MediaPlayer);

            // Start position polling.
            _positionTimer = DispatcherQueue.CreateTimer();
            _positionTimer.Interval = TimeSpan.FromMilliseconds(250);
            _positionTimer.Tick += (_, _) => UpdatePosition();
            _positionTimer.Start();

            // Auto-hide controls timer.
            _autoHideTimer = DispatcherQueue.CreateTimer();
            _autoHideTimer.Interval = TimeSpan.FromSeconds(ControlsAutoHideSeconds);
            _autoHideTimer.IsRepeating = false;
            _autoHideTimer.Tick += (_, _) => HideControls();

            if (_request is not null)
            {
                await Vm.PlayAsync(_request.Title, _request.Url);
            }
        }
        catch (Exception ex)
        {
            ShowLoading("Player init failed: " + ex.Message);
            App.LogDiagnostic("PlayerPage.Loaded", ex);
        }
    }

    private void PlayerPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _positionTimer?.Stop();
        _autoHideTimer?.Stop();
        if (Vm.Player.IsInitialized)
        {
            try
            {
                UnhookMediaPlayerEvents(Vm.Player.MediaPlayer);
                Vm.Player.Stop();
                Vm.Player.MediaPlayer.Hwnd = IntPtr.Zero;
            }
            catch { /* swallow */ }
        }
        _surface?.Dispose();
        _surface = null;

        if (_wasFullscreen) ExitFullscreen();
    }

    // ---- MediaPlayer event glue ----------------------------------------

    private void HookMediaPlayerEvents(MediaPlayer mp)
    {
        mp.Buffering += Mp_Buffering;
        mp.Playing += Mp_Playing;
        mp.Paused += Mp_Paused;
        mp.Stopped += Mp_Stopped;
        mp.EndReached += Mp_EndReached;
        mp.EncounteredError += Mp_Error;
        mp.LengthChanged += Mp_LengthChanged;
    }

    private void UnhookMediaPlayerEvents(MediaPlayer mp)
    {
        mp.Buffering -= Mp_Buffering;
        mp.Playing -= Mp_Playing;
        mp.Paused -= Mp_Paused;
        mp.Stopped -= Mp_Stopped;
        mp.EndReached -= Mp_EndReached;
        mp.EncounteredError -= Mp_Error;
        mp.LengthChanged -= Mp_LengthChanged;
    }

    private void Mp_Buffering(object? sender, MediaPlayerBufferingEventArgs e)
    {
        if (e.Cache >= 99f) return;
        DispatcherQueue.TryEnqueue(() => ShowLoading($"Buffering {(int)e.Cache}%"));
    }

    private void Mp_Playing(object? sender, EventArgs e) => DispatcherQueue.TryEnqueue(HideLoading);
    private void Mp_Paused(object? sender, EventArgs e) => DispatcherQueue.TryEnqueue(() => SetPlayPauseGlyph(false));
    private void Mp_Stopped(object? sender, EventArgs e) => DispatcherQueue.TryEnqueue(() => { SetPlayPauseGlyph(false); ShowLoading("Stopped"); });
    private void Mp_EndReached(object? sender, EventArgs e) => DispatcherQueue.TryEnqueue(() => ShowLoading("End of stream"));
    private void Mp_Error(object? sender, EventArgs e) => DispatcherQueue.TryEnqueue(() => ShowLoading("Playback error — check the URL or your network."));
    private void Mp_LengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e) => DispatcherQueue.TryEnqueue(UpdatePosition);

    // ---- Position / scrubber -------------------------------------------

    private void UpdatePosition()
    {
        if (!Vm.Player.IsInitialized) return;
        var pos = Vm.Player.PositionMs;
        var dur = Vm.Player.DurationMs;
        TimeText.Text = $"{FormatTime(pos)} / {(dur > 0 ? FormatTime(dur) : "live")}";
        if (!_userScrubbing && dur > 0)
        {
            Scrubber.Maximum = dur;
            Scrubber.Value = pos;
        }
        // Sync the play glyph in case the engine state changed externally.
        SetPlayPauseGlyph(Vm.Player.IsPlaying);
    }

    private static string FormatTime(long ms)
    {
        var t = TimeSpan.FromMilliseconds(ms < 0 ? 0 : ms);
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}" : $"{t.Minutes}:{t.Seconds:D2}";
    }

    private void Scrubber_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _userScrubbing = false;
        if (Vm.Player.IsInitialized && Vm.Player.DurationMs > 0)
        {
            Vm.Player.PositionMs = (long)Scrubber.Value;
        }
    }

    private void Scrubber_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (Vm.Player.IsInitialized && Vm.Player.DurationMs > 0)
        {
            Vm.Player.PositionMs = (long)Scrubber.Value;
        }
    }

    // ---- Loading state -------------------------------------------------

    private void ShowLoading(string text)
    {
        LoadingText.Text = text;
        LoadingPanel.Visibility = Visibility.Visible;
        LoadingRing.IsActive = true;
        _surface?.Hide();
    }

    private void HideLoading()
    {
        LoadingPanel.Visibility = Visibility.Collapsed;
        LoadingRing.IsActive = false;
        _surface?.Show();
        SetPlayPauseGlyph(Vm.Player.IsPlaying);
    }

    // ---- Controls ------------------------------------------------------

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (!Vm.Player.IsInitialized) return;
        if (Vm.Player.IsPlaying)
        {
            Vm.Player.Pause();
            SetPlayPauseGlyph(false);
            PulseCenter("\xE769"); // pause glyph
        }
        else
        {
            Vm.Player.Resume();
            SetPlayPauseGlyph(true);
            PulseCenter("\xE768"); // play glyph
        }
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        if (Vm.Player.IsInitialized) Vm.Player.Stop();
    }

    private void Mute_Click(object sender, RoutedEventArgs e)
    {
        if (!Vm.Player.IsInitialized) return;
        Vm.Player.Mute = !Vm.Player.Mute;
        MuteGlyph.Glyph = Vm.Player.Mute ? "\xE74F" : "\xE767";
    }

    private void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (Vm.Player.IsInitialized) Vm.Player.Volume = (int)e.NewValue;
    }

    private void SetPlayPauseGlyph(bool playing)
    {
        // Pause icon when playing, Play icon when paused.
        PlayPauseGlyph.Glyph = playing ? "\xE769" : "\xE768";
    }

    /// <summary>Briefly flash a centre-screen glyph so the user sees their click registered.</summary>
    private async void PulseCenter(string glyph)
    {
        CenterPulseGlyph.Glyph = glyph;
        CenterPulse.Visibility = Visibility.Visible;
        try
        {
            await Task.Delay(400);
            CenterPulse.Visibility = Visibility.Collapsed;
        }
        catch { /* page may have unloaded */ }
    }

    // ---- Auto-hide controls --------------------------------------------

    private void Root_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        ShowControls();
        _autoHideTimer?.Stop();
        _autoHideTimer?.Start();
    }

    private void ShowControls() => ControlsBar.Visibility = Visibility.Visible;

    private void HideControls()
    {
        ControlsBar.Visibility = Visibility.Collapsed;
        // Reposition the popup to fill the new larger VideoHost area.
        _surface?.Show();
    }

    // ---- Fullscreen ----------------------------------------------------

    private void PlayerPage_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.F11 || (e.Key == VirtualKey.Escape && _wasFullscreen))
        {
            Fullscreen_Click(this, e);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Space)
        {
            PlayPause_Click(this, e);
            e.Handled = true;
        }
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e)
    {
        if (_wasFullscreen) ExitFullscreen(); else EnterFullscreen();
    }

    private async void EnterFullscreen()
    {
        var aw = App.MainWindowInstance.AppWindow;
        _previousPresenter = aw.Presenter as OverlappedPresenter;
        aw.SetPresenter(AppWindowPresenterKind.FullScreen);
        FullscreenGlyph.Glyph = "\xE73F"; // back-to-window
        _wasFullscreen = true;

        // Hide the chrome so VideoArea expands to fill the entire monitor.
        // The controls bar will reappear on mouse movement (then re-hide).
        HideControls();

        // Presenter changes don't always raise position/size events; wait a
        // tick for layout to settle and force the popup to catch up.
        await Task.Delay(50);
        _surface?.ForceReposition();
    }

    private async void ExitFullscreen()
    {
        var aw = App.MainWindowInstance.AppWindow;
        if (_previousPresenter is not null) aw.SetPresenter(_previousPresenter);
        else aw.SetPresenter(AppWindowPresenterKind.Overlapped);
        FullscreenGlyph.Glyph = "\xE740";
        _wasFullscreen = false;

        ShowControls();
        _autoHideTimer?.Stop();
        _autoHideTimer?.Start();

        await Task.Delay(50);
        _surface?.ForceReposition();
    }
}
