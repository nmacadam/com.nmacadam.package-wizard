/* Hibzz PackageCreator © 2023 by Hibzz Games is licensed under CC BY 4.0
 * https://github.com/hibzzgames/Hibzz.PackageCreator
 *
 * PackageWizard.cs includes some functionality from PackageCreator's PackageCreator.cs:
 * - Git repo initialization, add, and commit process implementation
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.PackageManager;

namespace PackageWizard.Editor
{
    [Serializable]
    public class PackageDependency
    {
        public string PackageName;
        public string Version;
    }
    
    [Serializable]
    public class PackageUnityVersion
    {
        public string Major;
        public string Minor;
        public string Release;

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Release}";
        }
    }
    
    public static class PackageWizard
    {
        public static void New(
            string name, 
            string organizationName, 
            string displayName, 
            string description = "", 
            IEnumerable<PackageDependency> dependencies = null, 
            PackageUnityVersion unityVersion = null, 
            bool initializeGitRepository = true,
            string assemblyDefinitionPrefix = "")
        {
            if (!PackageValidation.ValidateName(name))
            {
                throw new ArgumentException($"Name '{name}' is not a valid in a package name.");
            }
            if (!PackageValidation.ValidateOrganizationName(organizationName))
            {
                throw new ArgumentException($"Name '{organizationName}' is not a valid in a package name.");
            }

            var expectedName = $"com.{organizationName}.{name}";
            if (expectedName.Length > 50)
            {
                throw new ArgumentException($"Package name '{expectedName}' is too long to appear in the editor. " +
                                            $"Name must be less than 50 characters.");
            }
            if (!PackageValidation.ValidateCompleteName(expectedName))
            {
                throw new ArgumentException($"Package name '{expectedName}' is not a valid in a package name.");
            }
            
            var templateDirectory = new DirectoryInfo($"{ProjectDirectoryInfo.Packages}/com.nmacadam.package-wizard/Template~/com.$ORGANIZATION.$PACKAGE");
            if (!templateDirectory.Exists)
            {
                throw new IOException($"Could not locate package template at: {templateDirectory}");
            }
            var targetDirectory = new DirectoryInfo($"{ProjectDirectoryInfo.Packages}/{expectedName}");
            if (targetDirectory.Exists)
            {
                throw new IOException($"Target directory for new package does not exist: {targetDirectory}");
            }
                    
            if (targetDirectory.Exists)
            {
                throw new IOException($"Target directory for new package already exists.");
            }

            CopyFilesRecursively(templateDirectory, targetDirectory);

            var manifestReplacements = new[]
            {
                ("$PACKAGE", name),
                ("$ORGANIZATION", organizationName),
                ("$DISPLAY_NAME", displayName),
                ("$DESCRIPTION", description),
                ("$DEPENDENCIES", CreateDependenciesJson(dependencies)),
                ("$UNITY_VERSION", CreateUnityVersionJson(unityVersion)),
            };
            var replacements = new[]
            {
                ("$PACKAGE", name),
                ("$ORGANIZATION", organizationName),
                ("$DISPLAY_NAME", displayName),
                ("$ASSEMBLY", string.IsNullOrEmpty(assemblyDefinitionPrefix) ? displayName.Replace(" ", string.Empty) : assemblyDefinitionPrefix),
            };

            ProcessManifest(new FileInfo($"{targetDirectory}/package.json.template"), manifestReplacements);
            ProcessFilesRecursively(targetDirectory, replacements);
            ProcessDirectoriesRecursively(targetDirectory, replacements);
                    
            Client.Resolve();

            if (initializeGitRepository)
            {
                InitializeGitRepo(targetDirectory.ToString());
            }
        }

        private static void InitializeGitRepo(string path)
        {
            // > git init .
            var initProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "git",
                    Arguments = "init .",
                    WorkingDirectory = path,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            // > git add .
            var addProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "git",
                    Arguments = "add .",
                    WorkingDirectory = path,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            // > git commit -m "Initial commit"
            var commitProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "git",
                    Arguments = "commit -m \"Initial commit\"",
                    WorkingDirectory = path,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            // run the processes in a sequence
            initProcess.Start();
            initProcess.WaitForExit();

            addProcess.Start();
            addProcess.WaitForExit();
			
            commitProcess.Start();
            commitProcess.WaitForExit();
        }

        // https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target) 
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                // skip .git if it exists
                if (dir.Name == ".git")
                {
                    continue;
                }
                
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            }

            foreach (FileInfo file in source.GetFiles())
            {
                file.CopyTo(Path.Combine(target.FullName, file.Name));
            }
        }

        private static string CreateDependenciesJson(IEnumerable<PackageDependency> dependencies)
        {
            var packageDependencies = dependencies as PackageDependency[] ?? dependencies.ToArray();
            
            if (packageDependencies.Length == 0)
            {
                return string.Empty;
            }
            
            var sb = new StringBuilder();
            sb.Append("\"dependencies\": {\n");
            for (var i = 0; i < packageDependencies.Length; i++)
            {
                var dependency = packageDependencies[i];

                sb.Append($"    \"{dependency.PackageName}\": \"{dependency.Version}\"");
                if (i != packageDependencies.Length - 1)
                {
                    sb.Append($",");
                }
                sb.Append("\n");
            }
            sb.Append("  },");
            
            return sb.ToString();
        }

        private static string CreateUnityVersionJson(PackageUnityVersion unityVersion)
        {
            if (unityVersion == null || string.IsNullOrEmpty(unityVersion.Major) || string.IsNullOrEmpty(unityVersion.Minor) || string.IsNullOrEmpty(unityVersion.Release))
            {
                return string.Empty;
            }
            
            var sb = new StringBuilder();
            sb.Append($"\"unity\": \"{unityVersion.Major}.{unityVersion.Minor}\",\n");
            sb.Append($"  \"unityRelease\": \"{unityVersion.Release}\",");
            
            return sb.ToString();
        }

        private static void ProcessManifest(FileInfo file, (string, string)[] replacements)
        {
            if (file.Extension != ".template")
            {
                return;
            }
                
            string text = System.IO.File.ReadAllText(file.ToString());
            foreach (var replacement in replacements)
            {
                text = text.Replace(replacement.Item1, replacement.Item2);
            }
            System.IO.File.WriteAllText(file.ToString(), text);

            string newName = file.Name[..^".template".Length];
            foreach (var replacement in replacements)
            {
                newName = newName.Replace(replacement.Item1, replacement.Item2);
            }
            System.IO.File.Move(file.FullName, $"{file.Directory}/{newName}");
        }

        private static void ProcessFilesRecursively(DirectoryInfo source, (string, string)[] replacements)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                ProcessFilesRecursively(dir, replacements);
            }

            foreach (FileInfo file in source.GetFiles())
            {
                if (file.Extension != ".template")
                {
                    continue;
                }
                
                string text = System.IO.File.ReadAllText(file.ToString());
                foreach (var replacement in replacements)
                {
                    text = text.Replace(replacement.Item1, replacement.Item2);
                }
                System.IO.File.WriteAllText(file.ToString(), text);

                string newName = file.Name[..^".template".Length];
                foreach (var replacement in replacements)
                {
                    newName = newName.Replace(replacement.Item1, replacement.Item2);
                }
                System.IO.File.Move(file.FullName, $"{file.Directory}/{newName}");
            }
        }
        
        private static void ProcessDirectoriesRecursively(DirectoryInfo source, (string, string)[] replacements)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                ProcessDirectoriesRecursively(dir, replacements);
                
                string newName = dir.Name;
                bool didReplacement = false;
                foreach (var replacement in replacements)
                {
                    if (newName.Contains(replacement.Item1))
                    {
                        newName = newName.Replace(replacement.Item1, replacement.Item2);
                        didReplacement = true;
                    }
                }

                if (didReplacement)
                {
                    System.IO.Directory.Move(dir.FullName, $"{dir.Parent}/{newName}");
                }
            }
        }
    }
}
