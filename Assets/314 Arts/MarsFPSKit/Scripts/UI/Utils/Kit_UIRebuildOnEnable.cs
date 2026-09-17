using UnityEngine;
using UnityEngine.UI;

namespace MarsFPSKit.UI
{
    public class Kit_UIRebuildOnEnable : Kit_Base
    {
        public void OnEnable()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }
    }
}