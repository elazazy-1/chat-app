namespace MauiApp3.Services;

public interface IAudioRecorderService
{
    bool IsRecording { get; }
    Task<bool> StartRecordingAsync();
    Task<byte[]?> StopRecordingAsync();
    Task PlayAudioAsync(byte[] audioData);
    void StopPlayback();
}
