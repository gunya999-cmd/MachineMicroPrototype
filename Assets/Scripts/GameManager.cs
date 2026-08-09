using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameMode
    {
        Edit,
        Run
    }

    public static GameManager Instance { get; private set; }

    public GameMode CurrentMode { get; private set; } = GameMode.Edit;

    [Header("UI")]
    [SerializeField] private GameObject winPanel;

    private Part[] parts;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        parts = FindObjectsOfType<Part>();
    }

    private void Start()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        foreach (var part in parts)
        {
            part.SaveState();
        }
    }

    public void StartRun()
    {
        if (CurrentMode == GameMode.Run)
        {
            return;
        }

        CurrentMode = GameMode.Run;

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        foreach (var part in parts)
        {
            part.SaveState();
        }

        foreach (var part in parts)
        {
            part.EnterRun();
        }
    }

    public void ResetToEdit()
    {
        CurrentMode = GameMode.Edit;

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        foreach (var part in parts)
        {
            part.Restore();
        }
    }

    public void CompleteLevel()
    {
        if (CurrentMode != GameMode.Run)
        {
            return;
        }

        CurrentMode = GameMode.Edit;

        foreach (var part in parts)
        {
            part.FreezeEdit();
        }

        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
    }
}
