using System;
using System.IO;
using UnityEngine;

namespace PackageWizard.Editor
{
    public static class ProjectDirectoryInfo
    {
        public static DirectoryInfo Root => Directory.GetParent(Application.dataPath);
        public static DirectoryInfo Assets => new DirectoryInfo(Application.dataPath);
        public static DirectoryInfo ProjectSettings => new(Path.GetFullPath(Path.Combine(Application.dataPath, "../ProjectSettings")));
        public static DirectoryInfo UserSettings => new(Path.GetFullPath(Path.Combine(Application.dataPath, "../UserSettings")));
        public static DirectoryInfo Packages => new(Path.GetFullPath(Path.Combine(Application.dataPath, "../Packages")));

        public static string AssetFolderPathToFullPath(string assetFolderPath)
        {
            return Path.Combine(Root.ToString(), assetFolderPath);
        }
        
        public static string FullPathToAssetFolderPath(string fullPath)
        {
            var rootPath = Root.ToString();
            if (fullPath.StartsWith(rootPath))
            {
                return fullPath.Substring(rootPath.Length + 1, fullPath.Length - rootPath.Length - 1);
            }

            throw new ArgumentException("The project asset folder is not contained within " + fullPath);
        }
    }
}