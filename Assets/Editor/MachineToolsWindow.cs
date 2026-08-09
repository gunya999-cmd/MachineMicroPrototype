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
        if (!Directory.Exists(Path.Combine(ProjectRoot, ".git")))
        {
            EditorUtility.DisplayDialog(
                "Machine Tools",
                "Локальная папка Unity не связана с Git. Нужна однократная настройка Git для этой папки.",
                "OK");
            return;
        }

        GitResult branch = RunGit("branch --show-current");
        if (!branch.Success)
        {
            ShowGitError("Не удалось определить текущую ветку.", branch);
            return;
        }

        if (!string.Equals(branch.Output.Trim(), "main", StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog(
                "Machine Tools",
                "UPDATE PROJECT работает из ветки main. Текущая ветка: " + branch.Output.Trim(),
                "OK");
            return;
        }

        GitResult pull = RunGit("pull --ff-only origin main");
        if (!pull.Success)
        {
            ShowGitError(
                "Git не смог безопасно обновить проект. Локальные изменения не перезаписаны.",
                pull);
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

    private static GitResult RunGit(string arguments)
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "git",
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

                return new GitResult(process.ExitCode == 0, output.Trim());
            }
        }
        catch (Exception exception)
        {
            return new GitResult(false, exception.Message);
        }
    }

    private static void ShowGitError(string message, GitResult result)
    {
        Debug.LogError("[Machine Tools] " + message + "\n" + result.Output);
        EditorUtility.DisplayDialog("Machine Tools", message + "\n\n" + result.Output, "OK");
    }

    private struct GitResult
    {
        public bool Success;
        public string Output;

        public GitResult(bool success, string output)
        {
            Success = success;
            Output = output ?? string.Empty;
        }
    }
}
