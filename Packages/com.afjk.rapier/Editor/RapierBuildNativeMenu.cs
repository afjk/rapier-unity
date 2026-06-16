using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AFJK.Rapier.Editor
{
    public static class RapierBuildNativeMenu
    {
        [MenuItem("Rapier/Build Native/Release")]
        public static void BuildNativeRelease()
        {
            var packagePath = Path.GetFullPath("Packages/com.afjk.rapier");
            var repositoryRoot = Path.GetFullPath(Path.Combine(packagePath, "..", ".."));
            var nativePath = Path.Combine(repositoryRoot, "native");

            if (!Directory.Exists(nativePath))
            {
                Debug.LogWarning($"Native workspace not found at {nativePath}. Run this from a repository checkout.");
                return;
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cargo",
                    Arguments = "build --release -p rapier_unity_ffi",
                    WorkingDirectory = nativePath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Debug.Log(args.Data);
                }
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Debug.Log(args.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) =>
            {
                Debug.Log($"Rapier native build finished with exit code {process.ExitCode}.");
                process.Dispose();
            };
        }

        [MenuItem("Rapier/Build Native/Open Native README")]
        public static void OpenNativeReadme()
        {
            var path = Path.GetFullPath("native/README.md");
            if (File.Exists(path))
            {
                EditorUtility.OpenWithDefaultApp(path);
            }
            else
            {
                Debug.LogWarning("native/README.md was not found.");
            }
        }
    }
}

