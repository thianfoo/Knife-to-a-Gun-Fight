using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MarsFPSKit.UI
{
    public class Kit_SimpleChatEntry : Kit_Base
    {
        /// <summary>
        /// The text for this entry
        /// </summary>
        [SerializeField] private TextMeshProUGUI txt;

        /// <summary>
        /// Sets up this chat entry with given parameteres
        /// </summary>
        /// <param name="content"></param>
        /// <param name="col"></param>
        public void Setup(string content)
        {
            //Set it up
            txt.text = content; //Text
        }
    }
}
