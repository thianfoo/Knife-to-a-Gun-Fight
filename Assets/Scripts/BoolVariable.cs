using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "NewBoolVariable", menuName = "SO Architecture/Variables/Bool")]
public class BoolVariable : ScriptableObject
{
    [SerializeField] private bool value;

    // Event raised automatically whenever the state changes
    public UnityAction<bool> OnValueChanged;

    public bool Value
    {
        get => value;
        set
        {
            if (this.value != value)
            {
                this.value = value;
                OnValueChanged?.Invoke(this.value); // Fired instantly on state flip
            }
        }
    }

    // Reset value on enable (e.g. when starting play mode in Unity)
    private void OnEnable()
    {
        value = false;
    }
}