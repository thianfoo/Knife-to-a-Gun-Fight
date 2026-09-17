using UnityEngine;
using System.Collections;

namespace MarsFPSKit
{
    /// <summary>
    /// This implementes shaking
    /// </summary>
    public class Kit_CameraShake : Kit_Base
    {
        private float shakeAmount;
        private float shakeDuration;

        //Readonly values...
        private float shakePercentage;   // 0-1 representing remaining shake.
        private float startAmount;       // initial shake amount snapshot
        private float startDuration;     // initial duration snapshot

        private bool isRunning = false;  // Is the coroutine running right now?

        public bool smooth;              // Smooth rotation?
        public float smoothAmount = 5f;  // Amount to smooth

        public void ShakeCamera(float amount, float duration)
        {
            // Keep call/signature the same, but make stacking behave predictably.
            // Amount stacks additively.
            shakeAmount += amount;

            // Duration stacks additively.
            shakeDuration += duration;

            // We want a stable "starting point" for the current shake session.
            // If we are already shaking, extend the "startDuration" so percentage stays coherent.
            if (!isRunning)
            {
                startAmount = shakeAmount;
                startDuration = shakeDuration;
            }
            else
            {
                // If additional shake is added while running, treat the new total duration
                // as the new "total time" for percentage calculations, without causing jumps.
                startAmount = Mathf.Max(startAmount, shakeAmount);
                startDuration = Mathf.Max(startDuration, shakeDuration);
            }

            gameObject.SetActive(true);

            if (!isRunning)
                StartCoroutine(Shake());
        }

        private IEnumerator Shake()
        {
            isRunning = true;

            // If you want shake unaffected by timescale, switch to Time.unscaledDeltaTime below.
            // (Kept as Time.deltaTime to preserve typical Unity behavior unless you choose otherwise.)
            while (shakeDuration > 0f)
            {
                float dt = Time.deltaTime;

                // Avoid divide-by-zero if someone calls ShakeCamera with duration ~0.
                float total = Mathf.Max(0.0001f, startDuration);

                // Linear, framerate-stable countdown.
                shakeDuration -= dt;

                // Normalized remaining percentage (1 -> 0).
                shakePercentage = Mathf.Clamp01(shakeDuration / total);

                // Taper amplitude based on remaining percentage.
                float currentAmount = startAmount * shakePercentage;

                // Pick a random rotation target (in degrees) scaled by current amount.
                Vector3 rotationAmount = Random.insideUnitSphere * currentAmount;
                rotationAmount.z = 0f; // Don't roll.

                if (transform.parent)
                {
                    Quaternion targetRot = Quaternion.Euler(rotationAmount);

                    if (smooth)
                    {
                        // Lerp factor as a proper exponential smoothing step (stable across FPS)
                        // instead of "Time.deltaTime * smoothAmount" which can overshoot at low FPS.
                        float lerpT = 1f - Mathf.Exp(-smoothAmount * dt);
                        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, lerpT);
                    }
                    else
                    {
                        transform.localRotation = targetRot;
                    }
                }

                yield return null;
            }

            if (transform.parent)
            {
                transform.localRotation = Quaternion.identity;
            }

            // Reset state for next run.
            shakeAmount = 0f;
            shakeDuration = 0f;
            startAmount = 0f;
            startDuration = 0f;
            shakePercentage = 0f;

            isRunning = false;
        }
    }
}