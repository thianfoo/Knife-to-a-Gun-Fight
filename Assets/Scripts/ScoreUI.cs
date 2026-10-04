using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public ScoreEventChannel scoreChannel; // Drag the ScriptableObject here
    
    private int currentScore = 0;

    void OnEnable()
    {
        // Subscribe to the event (Observer Pattern)
        if (scoreChannel != null)
            scoreChannel.OnScoreAdded += AddScore;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        if (scoreChannel != null)
            scoreChannel.OnScoreAdded -= AddScore;
    }

    void AddScore(int amount)
    {
        currentScore += amount;
        scoreText.text = $"SCORE: {currentScore:D5}";
    }
}