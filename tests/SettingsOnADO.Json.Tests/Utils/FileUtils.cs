﻿namespace SettingsOnADO.Json.Tests.Utils;

public static class FileUtils
{
    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive, bool overwrite = true)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        if (overwrite && Directory.Exists(destinationDir))
        {
            Directory.Delete(destinationDir, recursive);
        }

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }

    public static string GetTempFolderName()
    {
        // Generate a GUID
        string uniqueId = Guid.NewGuid().ToString();

        // Get the path for the temporary directory
        string tempPath = Path.GetTempPath();

        // Combine the temp path with the GUID to create a unique directory name
        string tempFolderName = Path.Combine(tempPath, uniqueId);

        // Create the temporary folder
        Directory.CreateDirectory(tempFolderName);

        // Return the path of the temporary folder
        return tempFolderName;
    }
}
