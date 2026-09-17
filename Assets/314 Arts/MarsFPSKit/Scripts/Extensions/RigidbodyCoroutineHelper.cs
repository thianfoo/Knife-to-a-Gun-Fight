using System.Collections;
using UnityEngine;

namespace MarsFPSKit.Helper
{
    public class RigidbodyCoroutineHelper : Kit_Base
    {
        public IEnumerator AddForceNextFrame(Vector3 force)
        {
            yield return new WaitForEndOfFrame();
            GetComponent<Rigidbody>().AddForce(force);
            Destroy(this);
        }
    }
}