namespace MauiApp3.Services;

/// <summary>
/// Interface for audio recording and playback services.
/// </summary>
public interface IAudioRecorderService
{
    /// <summary>Gets a value indicating whether recording is currently in progress.</summary>
    bool IsRecording { get; }
    
    /// <summary>Starts an audio recording session.</summary>
    /// <returns>True if successful; otherwise, false.</returns>
    Task<bool> StartRecordingAsync();
    
    /// <summary>Stops the current audio recording session.</summary>
    /// <returns>The recorded audio data as a byte array, or null if failed.</returns>
    Task<byte[]?> StopRecordingAsync();
    
    /// <summary>Plays the provided audio data.</summary>
    /// <param name="audioData">The audio data to play.</param>
    Task PlayAudioAsync(byte[] audioData);
    
    /// <summary>Stops any currently playing audio.</summary>
    void StopPlayback();
}
