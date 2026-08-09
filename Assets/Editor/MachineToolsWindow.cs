using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MachineToolsWindow : EditorWindow
{
    [MenuItem("Machine/Tools")]
    public static void Open()
    {
        MachineToolsWindow window = GetWindow<MachineToolsWindow>("Machine Tools");
        window.minSize = new Vector2(420f, 240f);
    }

    private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

    private void OnGUI()
    {
        GUILayout.Space(12f);
        EditorGUILayout.LabelField("MACHINE MICRO PROTOTYPE", EditorStyles.boldLabel);
        GUILayout.Space(8f);

        EditorGUILayout.HelpBox(
            "UPDATE PROJECT получает свежий main из GitHub. После обновления Unity автоматически перекомпилирует проект.",
            MessageType.Info);

        GUILayout.Space(12f);

        bool busy = EditorApplication.isCompiling ||
                    EditorApplication.isUpdating ||
                    EditorApplication.isPlayingOrWillChangePlaymode;

        using (new EditorGUI.DisabledScope(busy))
        {
            if (GUILayout.Button("UPDATE PROJECT", GUILayout.Height(52f)))
            {
                UpdateProjectFromGitHub();
            }
        }

        GUILayout.Space(10f);

        bool builderAvailable = FindBuilderMethod() != null;
        using (new EditorGUI.DisabledScope(busy || !builderAvailable))
        {
            if (GUILayout.Button("BUILD / UPDATE SCENE", GUILayout.Height(52f)))
            {
                InvokeSceneBuilder();
            }
        }

        if (!builderAvailable)
        {
            GUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Сборщик сцены ещё не загружен локально. Сначала нажми UPDATE PROJECT.",
                MessageType.Warning);
        }

        if (busy)
        {
            GUILayout.Space(8f);
            EditorGUILayout.HelpBox("Unity сейчас занят компиляцией, импортом или Play Mode.", MessageType.Warning);
        }
    }

    private static void UpdateProjectFromGitHub()
    {
        string updaterPath = Path.Combine(ProjectRoot, "UPDATE_FROM_GITHUB.bat");

        if (File.Exists(updaterPath))
        {
            ProcessResult update = RunProcess(
                "cmd.exe",
                "/c \"\"" + updaterPath + "\" --no-pause\"");

            if (!update.Success)
            {
                ShowProcessError("Не удалось обновить проект из GitHub.", update);
                return;
            }

            Debug.Log("[Machine Tools] Project update complete.\n" + update.Output);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            return;
        }

        if (Directory.Exists(Path.Combine(ProjectRoot, ".git")))
        {
            ProcessResult pull = RunProcess("git", "pull --ff-only origin main");
            if (!pull.Success)
            {
                ShowProcessError("Git не смог безопасно обновить проект.", pull);
                return;
            }

            Debug.Log("[Machine Tools] Git update complete.\n" + pull.Output);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            return;
        }

        EditorUtility.DisplayDialog(
            "Machine Tools",
            "В корне проекта нет UPDATE_FROM_GITHUB.bat и папка не является Git-репозиторием.",
            "OK");
    }

    private static MethodInfo FindBuilderMethod()
    {
        Type builderType = Type.GetType("MachineSceneBuilder, Assembly-CSharp-Editor");
        return builderType?.GetMethod("BuildOrUpdateScene", BindingFlags.Public | BindingFlags.Static);
    }

    private static void InvokeSceneBuilder()
    {
        MethodInfo method = FindBuilderMethod();
        if (method == null)
        {
            EditorUtility.DisplayDialog("Machine Tools", "Сначала нажми UPDATE PROJECT.", "OK");
            return;
        }

        try
        {
            method.Invoke(null, null);
        }
        catch (TargetInvocationException exception)
        {
            Exception inner = exception.InnerException ?? exception;
            Debug.LogException(inner);
            EditorUtility.DisplayDialog("Machine Tools", inner.Message, "OK");
        }
    }

    private static ProcessResult RunProcess(string fileName, string arguments)
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = ProjectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                string output = string.IsNullOrWhiteSpace(stderr)
                    ? stdout
                    : stdout + Environment.NewLine + stderr;

                return new ProcessResult(process.ExitCode == 0, output.Trim());
            }
        }
        catch (Exception exception)
        {
            return new ProcessResult(false, exception.Message);
        }
    }

    private static void ShowProcessError(string message, ProcessResult result)
    {
        Debug.LogError("[Machine Tools] " + message + "\n" + result.Output);
        EditorUtility.DisplayDialog("Machine Tools", message + "\n\n" + result.Output, "OK");
    }

    private struct ProcessResult
    {
        public bool Success;
        public string Output;

        public ProcessResult(bool success, string output)
        {
            Success = success;
            Output = output ?? string.Empty;
        }
    }
}
