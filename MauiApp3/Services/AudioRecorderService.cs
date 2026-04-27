using Plugin.Maui.Audio;

namespace MauiApp3.Services;

/// <summary>
/// Service responsible for handling microphone recording and audio playback
/// using the Plugin.Maui.Audio library.
/// </summary>
public class AudioRecorderService : IAudioRecorderService
{
    private readonly IAudioManager _audioManager;
    private IAudioRecorder? _recorder;
    private IAudioPlayer? _player;
    private string? _lastPlaybackFile;

    /// <summary>Indicates if an audio recording is currently active.</summary>
    public bool IsRecording => _recorder?.IsRecording ?? false;

    public AudioRecorderService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    /// <summary>
    /// Requests necessary microphone permissions and starts an audio recording.
    /// </summary>
    /// <returns>True if recording started successfully.</returns>
    public async Task<bool> StartRecordingAsync()
    {
        try
        {
#if ANDROID
            // Android requires explicit runtime permission request for microphone
            var status = await Permissions.RequestAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Permission Denied", "Microphone access is required to record audio.", "OK");
                return false;
            }
#endif

            // Initialize the audio recorder instance and begin capturing hardware input
            _recorder = _audioManager.CreateRecorder();
            await _recorder.StartAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Recording start error: {ex.Message}");
            await Shell.Current.DisplayAlert("Recording Error", $"Could not start recording: {ex.Message}", "OK");
            return false;
        }
    }

    /// <summary>
    /// Stops the current recording and returns the audio data as a byte array.
    /// </summary>
    /// <returns>A byte array containing the WAV audio data, or null on failure.</returns>
    public async Task<byte[]?> StopRecordingAsync()
    {
        try
        {
            if (_recorder == null || !_recorder.IsRecording)
                return null;

            var source = await _recorder.StopAsync();
            if (source == null) return null;

            byte[]? bytes = null;
            string? sourceFilePath = null;

            // Try to get the original file path directly to avoid stream copy issues
            if (source is FileAudioSource fileSource)
            {
                sourceFilePath = fileSource.GetFilePath();
            }

            if (!string.IsNullOrEmpty(sourceFilePath) && File.Exists(sourceFilePath))
            {
                // Give the OS a moment to finalize the file
                await Task.Delay(200);
                bytes = await File.ReadAllBytesAsync(sourceFilePath);

                // Clean up the plugin's temp file
                try { File.Delete(sourceFilePath); } catch { }
            }
            else
                       {
                // Fallback to stream copy
                var tempFile = Path.Combine(FileSystem.CacheDirectory, $"rec_{Guid.NewGuid()}.wav");
                using (var stream = source.GetAudioStream())
                using (var fs = File.Create(tempFile))
                {
                    await stream.CopyToAsync(fs);
                }

                bytes = await File.ReadAllBytesAsync(tempFile);
                try { File.Delete(tempFile); } catch { }
            }

            if (bytes == null || bytes.Length < 100)
            {
                await Shell.Current.DisplayAlert("Recording Error", "No audio was recorded. Please check your microphone permissions and try again.", "OK");
                return null;
            }

            // Validate WAV header to ensure the file format is correct and playable.
            // A valid WAV file always starts with "RIFF" and has "WAVE" at offset 8.
            bool isValidWav = bytes.Length > 44 &&
                bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F' &&
                bytes[8] == 'W' && bytes[9] == 'A' && bytes[10] == 'V' && bytes[11] == 'E';

            if (!isValidWav)
            {
                System.Diagnostics.Debug.WriteLine("Warning: Recorded audio does not have a valid WAV header.");
            }

            return bytes;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Recording stop error: {ex.Message}");
            await Shell.Current.DisplayAlert("Recording Error", $"Could not stop recording: {ex.Message}", "OK");
            return null;
        }
    }

    /// <summary>
    /// Plays an audio file from a byte array. Stops any currently playing audio first.
    /// </summary>
    /// <param name="audioData">The WAV file bytes to play.</param>
    public async Task PlayAudioAsync(byte[] audioData)
    {
        try
        {
            StopPlayback();

            if (audioData == null || audioData.Length == 0)
            {
                await Shell.Current.DisplayAlert("Playback Error", "Audio data is empty.", "OK");
                return;
            }

            // Validate WAV header
            bool isValidWav = audioData.Length > 44 &&
                audioData[0] == 'R' && audioData[1] == 'I' && audioData[2] == 'F' && audioData[3] == 'F' &&
                audioData[8] == 'W' && audioData[9] == 'A' && audioData[10] == 'V' && audioData[11] == 'E';

            if (!isValidWav)
            {
                System.Diagnostics.Debug.WriteLine("Warning: Audio data does not have a valid WAV header.");
            }

#if WINDOWS
            // On Windows, CreatePlayer(string) incorrectly prepends "ms-appx:///Assets/" to absolute paths,
            // which breaks playback for dynamically generated or downloaded files.
            // Using a MemoryStream bypasses this Windows-specific bug.
            var ms = new MemoryStream(audioData);
            _player = _audioManager.CreatePlayer(ms);
            _player.Play();
#else
            // On Android and iOS, playing directly from a MemoryStream can sometimes be unreliable
            // with the MAUI Audio plugin. Writing to a temporary file ensures stable playback.
            var tempFile = Path.Combine(FileSystem.CacheDirectory, $"play_{Guid.NewGuid()}.wav");
            await File.WriteAllBytesAsync(tempFile, audioData);
            _lastPlaybackFile = tempFile;

            try
            {
                _player = _audioManager.CreatePlayer(tempFile);
                _player.Play();
            }
            catch
            {
                // Fallback: try playing from stream if file playback fails
                var stream = new MemoryStream(audioData);
                _player = _audioManager.CreatePlayer(stream);
                _player.Play();
            }
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Playback error: {ex.Message}");
            await Shell.Current.DisplayAlert("Playback Error", $"Could not play audio: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Immediately stops audio playback and cleans up resources.
    /// </summary>
    public void StopPlayback()
    {
        try
        {
            // Halt any currently active audio output
            if (_player?.IsPlaying == true)
            {
                _player.Stop();
            }
            
            // Dispose the player to release native audio hardware resources
            _player?.Dispose();
            _player = null;

            // Clean up the temporary WAV file that was written to disk for playback
            if (!string.IsNullOrEmpty(_lastPlaybackFile) && File.Exists(_lastPlaybackFile))
            {
                try { File.Delete(_lastPlaybackFile); } catch { }
                _lastPlaybackFile = null;
            }
        }
        catch { }
    }
}
