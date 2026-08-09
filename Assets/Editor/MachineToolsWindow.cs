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
            "UPDATE PROJECT выполняет безопасный git pull --ff-only из origin/main.\n" +
            "Никакие .bat, PowerShell или ZIP-загрузчики не используются.",
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
                "Сборщик сцены ещё не загружен локально. Сначала обнови проект через Git.",
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
        if (!Directory.Exists(Path.Combine(ProjectRoot, ".git")))
        {
            EditorUtility.DisplayDialog(
                "Machine Tools",
                "Локальная папка проекта не является Git-репозиторием.",
                "OK");
            return;
        }

        ProcessResult remote = RunProcess("git", "remote get-url origin");
        if (!remote.Success)
        {
            ShowProcessError("Не удалось проверить Git remote origin.", remote);
            return;
        }

        const string expectedRepository = "github.com/gunya999-cmd/MachineMicroPrototype.git";
        if (remote.Output.IndexOf(expectedRepository, StringComparison.OrdinalIgnoreCase) < 0)
        {
            EditorUtility.DisplayDialog(
                "Machine Tools",
                "origin указывает не на MachineMicroPrototype:\n\n" + remote.Output,
                "OK");
            return;
        }

        ProcessResult branch = RunProcess("git", "branch --show-current");
        if (!branch.Success || !string.Equals(branch.Output.Trim(), "main", StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog(
                "Machine Tools",
                "UPDATE PROJECT работает только в ветке main.\n\nТекущая ветка: " + branch.Output,
                "OK");
            return;
        }

        ProcessResult status = RunProcess("git", "status --porcelain");
        if (!status.Success)
        {
            ShowProcessError("Не удалось проверить локальные изменения.", status);
            return;
        }

        if (!string.IsNullOrWhiteSpace(status.Output))
        {
            EditorUtility.DisplayDialog(
                "Machine Tools",
                "Есть локальные изменения. UPDATE PROJECT остановлен, чтобы ничего не потерять.\n\n" + status.Output,
                "OK");
            return;
        }

        ProcessResult pull = RunProcess("git", "pull --ff-only origin main");
        if (!pull.Success)
        {
            ShowProcessError("Git не смог безопасно обновить проект.", pull);
            return;
        }

        Debug.Log("[Machine Tools] Git update complete.\n" + pull.Output);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
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
            EditorUtility.DisplayDialog("Machine Tools", "Сборщик сцены ещё недоступен.", "OK");
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
