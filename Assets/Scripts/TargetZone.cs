using UnityEngine;

public class TargetZone : MonoBehaviour
{
    [Tooltip("Тег объекта, который должен попасть в цель. По умолчанию Ball.")]
    public string targetTag = "Ball";

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (string.IsNullOrEmpty(targetTag))
        {
            return;
        }

        if (collision.gameObject.CompareTag(targetTag))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel();
            }
        }
    }
}
