using System.Linq;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace MauiApp3.Helpers;

public static class DownloadPathHelper
{
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
    private static async Task<string> SaveBytesAndroidAsync(string fileName, byte[] data)
    {
        var appFolderName = SanitizeFolderName(AppInfo.Current.Name ?? "App");

        if ((int)Android.OS.Build.VERSION.SdkInt < (int)Android.OS.BuildVersionCodes.Q)
        {
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

        var relativePath = Path.Combine(Android.OS.Environment.DirectoryDownloads, appFolderName);
        var values = new Android.Content.ContentValues();
        values.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, fileName);
        values.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, "application/octet-stream");
        values.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, relativePath);
        values.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, 1);

        var resolver = Android.App.Application.Context.ContentResolver;
        var uri = resolver?.Insert(Android.Provider.MediaStore.Downloads.ExternalContentUri, values);
        if (uri == null)
            throw new InvalidOperationException("Unable to create download entry.");

        using (var stream = resolver.OpenOutputStream(uri))
        {
            if (stream == null)
                throw new InvalidOperationException("Unable to open download stream.");

            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
        }

        values.Clear();
        values.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, 0);
        resolver.Update(uri, values, null, null);

        return $"{relativePath}/{fileName}";
    }
#endif

    private static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "App" : sanitized;
    }
}
