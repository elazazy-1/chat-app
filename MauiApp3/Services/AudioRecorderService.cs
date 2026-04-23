using Plugin.Maui.Audio;

namespace MauiApp3.Services;

public class AudioRecorderService : IAudioRecorderService
{
    private readonly IAudioManager _audioManager;
    private IAudioRecorder? _recorder;
    private IAudioPlayer? _player;
    private string? _lastPlaybackFile;

    public bool IsRecording => _recorder?.IsRecording ?? false;

    public AudioRecorderService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    public async Task<bool> StartRecordingAsync()
    {
        try
        {
#if ANDROID
            var status = await Permissions.RequestAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Permission Denied", "Microphone access is required to record audio.", "OK");
                return false;
            }
#endif

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

            // Validate WAV header
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
            // On Windows, CreatePlayer(string) incorrectly prepends ms-appx:///Assets/ to the path.
            // Always use MemoryStream on Windows for reliable playback.
            var ms = new MemoryStream(audioData);
            _player = _audioManager.CreatePlayer(ms);
            _player.Play();
#else
            // On other platforms, write to temp file first for better compatibility
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
                // Fallback: try playing from stream
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

    public void StopPlayback()
    {
        try
        {
            if (_player?.IsPlaying == true)
            {
                _player.Stop();
            }
            _player?.Dispose();
            _player = null;

            // Clean up old playback file
            if (!string.IsNullOrEmpty(_lastPlaybackFile) && File.Exists(_lastPlaybackFile))
            {
                try { File.Delete(_lastPlaybackFile); } catch { }
                _lastPlaybackFile = null;
            }
        }
        catch { }
    }
}
