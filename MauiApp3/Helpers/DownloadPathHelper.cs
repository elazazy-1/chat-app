using System.Linq;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace MauiApp3.Helpers;

/// <summary>
/// Provides cross-platform functionality for saving downloaded files to the device.
/// </summary>
public static class DownloadPathHelper
{
    /// <summary>
    /// Saves byte array data to the appropriate downloads folder for the platform.
    /// On Android Q and above, uses MediaStore. On Windows/older Android, uses file system.
    /// </summary>
    /// <param name="fileName">The desired name of the file.</param>
    /// <param name="data">The byte data of the file to save.</param>
    /// <returns>The path or URI where the file was saved.</returns>
    public static async Task<string> SaveBytesAsync(string fileName, byte[] data)
    {
    var safeFileName = Path.GetFileName(fileName);
    if (string.IsNullOrWhiteSpace(safeFileName))
        safeFileName = "file";

#if ANDROID
    return await SaveBytesAndroidAsync(safeFileName, data);
#else
    var folder = GetAppDownloadsFolder();
    var targetPath = Path.Combine(folder, safeFileName);
    await File.WriteAllBytesAsync(targetPath, data);
    return targetPath;
#endif
    }

    /// <summary>
    /// Determines the correct base downloads directory for the application.
    /// </summary>
    /// <returns>The path to the app's specific downloads folder.</returns>
    public static string GetAppDownloadsFolder()
    {
        var appFolderName = SanitizeFolderName(AppInfo.Current.Name ?? "App");
        var baseDownloads = string.Empty;

#if ANDROID
    var downloadsDir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
    baseDownloads = downloadsDir?.AbsolutePath ?? string.Empty;
#elif WINDOWS
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        baseDownloads = string.IsNullOrWhiteSpace(userProfile)
            ? string.Empty
            : Path.Combine(userProfile, "Downloads");
#endif

        if (string.IsNullOrWhiteSpace(baseDownloads))
        {
            baseDownloads = FileSystem.AppDataDirectory;
        }

        var target = Path.Combine(baseDownloads, appFolderName);
        Directory.CreateDirectory(target);
        return target;
    }

#if ANDROID
    /// <summary>
    /// Android-specific implementation for saving files, handling Scoped Storage (MediaStore) for Android Q+
    /// and traditional file system saving for older versions.
    /// </summary>
    private static async Task<string> SaveBytesAndroidAsync(string fileName, byte[] data)
    {
        var appFolderName = SanitizeFolderName(AppInfo.Current.Name ?? "App");

        if ((int)Android.OS.Build.VERSION.SdkInt < (int)Android.OS.BuildVersionCodes.Q)
        {
            // For older versions (Android 9/Pie and below), we must manually request
            // storage permissions and use traditional file IO to save to the public directory.
            var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
                throw new UnauthorizedAccessException("Storage permission denied.");

            var downloadsDir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
            var targetDir = Path.Combine(downloadsDir?.AbsolutePath ?? string.Empty, appFolderName);
            Directory.CreateDirectory(targetDir);
            var targetPath = Path.Combine(targetDir, fileName);
            await File.WriteAllBytesAsync(targetPath, data);
            return targetPath;
        }

        // For Android 10 (Q) and newer, scoped storage applies.
        // We use MediaStore to insert a new file record safely into the Downloads folder.
        var relativePath = Path.Combine(Android.OS.Environment.DirectoryDownloads, appFolderName);
        var values = new Android.Content.ContentValues();
        values.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, fileName);
        values.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, "application/octet-stream");
        values.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, relativePath);
        values.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, 1); // Mark as pending while writing

        var resolver = Android.App.Application.Context.ContentResolver;
        var uri = resolver?.Insert(Android.Provider.MediaStore.Downloads.ExternalContentUri, values);
        if (uri == null)
            throw new InvalidOperationException("Unable to create download entry.");

        // Open an output stream through the ContentResolver to safely write to the sandboxed path
        using (var stream = resolver.OpenOutputStream(uri))
        {
            if (stream == null)
                throw new InvalidOperationException("Unable to open download stream.");

            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
        }

        // Finalize the file by clearing the IsPending flag
        values.Clear();
        values.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, 0);
        resolver.Update(uri, values, null, null);

        return $"{relativePath}/{fileName}";
    }
#endif

    /// <summary>
    /// Cleans the application name to ensure it's a valid directory name.
    /// </summary>
    private static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "App" : sanitized;
    }
}
