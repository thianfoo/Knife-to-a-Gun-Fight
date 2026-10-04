using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Events/Score Event Channel")]
public class ScoreEventChannel : ScriptableObject
{
    // Make sure  is included here
    public event Action<int> OnScoreAdded;

    public void RaiseEvent(int amount)
    {
        OnScoreAdded?.Invoke(amount);
    }
}