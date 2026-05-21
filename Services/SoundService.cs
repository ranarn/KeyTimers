using System.IO;
using NAudio.Wave;

namespace KeyTimers.Services;

/// <summary>
/// Plays alert sounds using NAudio.
/// Falls back to a system beep when no file is configured or playback fails.
/// </summary>
public sealed class SoundService : IDisposable
{
    private IWavePlayer? _player;
    private AudioFileReader? _reader;

    /// <summary>
    /// Plays the audio file at <paramref name="filePath"/>.
    /// Uses <see cref="Console.Beep()"/> when the path is empty or the file does not exist.
    /// </summary>
    public void Play(string? filePath)
    {
        StopCurrent();

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            PlayBeep();
            return;
        }

        try
        {
            _reader = new AudioFileReader(filePath);
            _player = new WaveOutEvent();
            _player.Init(_reader);
            _player.PlaybackStopped += (_, _) => StopCurrent();
            _player.Play();
        }
        catch
        {
            PlayBeep();
        }
    }

    private static void PlayBeep()
    {
        Task.Run(() =>
        {
            try { Console.Beep(880, 200); } catch { /* no audio device */ }
        });
    }

    private void StopCurrent()
    {
        _player?.Stop();
        _player?.Dispose();
        _reader?.Dispose();
        _player = null;
        _reader = null;
    }

    public void Dispose() => StopCurrent();
}
