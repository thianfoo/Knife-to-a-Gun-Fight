using UnityEngine;
using UnityEngine.UI;

namespace MarsFPSKit.Utils
{
    public class Kit_GetRidOfCanvas : Kit_Base
    {
        public void Start()
        {
            GraphicRaycaster gr = GetComponentInChildren<GraphicRaycaster>();

            if (gr)
            {
                Destroy(gr);
            }

            Canvas canvas = GetComponentInChildren<Canvas>();

            if (canvas)
            {
                Destroy(canvas);
            }
        }
    }
}