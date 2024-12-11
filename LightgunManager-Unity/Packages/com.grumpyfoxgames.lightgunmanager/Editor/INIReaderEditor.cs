using System.IO;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace GrumpyFoxGames
{
    // Copy .ini file to the build folder
    public class PostBuildActions : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            var iniPath = INIReader.GetINIPath();
            var destinationFilePath = Path.Combine(Path.GetDirectoryName(report.summary.outputPath), INIReader.GetINIFileName());

            try
            {
                if (File.Exists(iniPath))
                {
                    File.Copy(iniPath, destinationFilePath, overwrite: true);
                    Debug.Log($"[INIReader] File copied successfully to: {destinationFilePath}");
                }
                else
                {
                    Debug.LogWarning($"[INIReader] Source file does not exist: {iniPath}");
                }
            }
            catch (IOException ex)
            {
                Debug.LogError($"[INIReader] Failed to copy file: {ex.Message}");
            }
        }
    }
}
