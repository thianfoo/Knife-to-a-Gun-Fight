using System.Collections.Generic;
using System.Diagnostics;
using MarsFPSKit.Networking;
using UnityEngine;
using System;

namespace MarsFPSKit
{
    /// <summary>
    /// This script acts as a base for all MonoBehaviour scripts and supplies static references
    /// for cleaner code aswell as a unified Debug system.
    /// </summary>
    public abstract class Kit_Base : MonoBehaviour
    {
        [SerializeField] private bool debug;

        protected Kit_IngameMain main
        {
            get
            {
                return Kit_IngameMain.instance;
            }
        }

        protected Kit_GameInformation game
        {
            get
            {
                if (!networkManager) return null;
                return networkManager.game;
            }
        }

        protected Kit_NetworkPlayerManager networkPlayerManager
        {
            get
            {
                return Kit_NetworkPlayerManager.instance;
            }
        }

        protected Kit_NetworkGameInformation networkGameInformation
        {
            get
            {
                return Kit_NetworkGameInformation.instance;
            }
        }

        protected Kit_SceneSyncer sceneSyncer
        {
            get
            {
                return Kit_SceneSyncer.instance;
            }
        }

        protected Kit_NetworkManager networkManager
        {
            get
            {
                return Kit_NetworkManager.instance;
            }
        }

        protected Kit_InputSystem inputSystem
        {
            get
            {
                return Kit_InputSystem.instance;
            }
        }

        // Cache per *Type* so all instances share.
        private static readonly Dictionary<Type, string> _typeNameCache = new();
        private static readonly Dictionary<Type, string> _typeColorHexCache = new();

        private string CachedTypeName
        {
            get
            {
                var t = GetType();
                if (_typeNameCache.TryGetValue(t, out var name)) return name;

                name = t.Name;
                _typeNameCache[t] = name;
                return name;
            }
        }

        private string CachedTypeColorHex
        {
            get
            {
                var t = GetType();
                if (_typeColorHexCache.TryGetValue(t, out var hex)) return hex;

                var color = ColorFromString(CachedTypeName);
                hex = ColorUtility.ToHtmlStringRGB(color);
                _typeColorHexCache[t] = hex;
                return hex;
            }
        }

        // Info
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        protected void DebugLog(string message, UnityEngine.Object context = null)
        {
            if (!debug && (!game || !game.debugGlobalOverride)) return;

            UnityEngine.Debug.Log(FormatPrefix(message), context);
        }

        // Warning
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        protected void DebugLogWarning(string message, UnityEngine.Object context = null)
        {
            if (!debug && (!game || !game.debugGlobalOverride)) return;

            UnityEngine.Debug.LogWarning(FormatPrefix(message), context);
        }

        // Error (often you may want errors even when debug==false; keep as-is or remove the check)
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        protected void DebugLogError(string message, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogError(FormatPrefix(message), context);
        }

        private string FormatPrefix(string message)
        {
            string tag = $"<color=#{CachedTypeColorHex}>[{CachedTypeName}]</color>";
            return $"{tag}:{message}";
        }

        // Stable HSV color from string (FNV-1a hash)
        private static Color ColorFromString(string s)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < s.Length; i++)
                {
                    hash ^= s[i];
                    hash *= 16777619;
                }

                float hue = (hash % 360u) / 360f;                  // 0..1
                float sat = 0.65f + ((hash >> 9) % 25u) / 100f;    // 0.65..0.89
                float val = 0.80f + ((hash >> 17) % 15u) / 100f;   // 0.80..0.94

                return Color.HSVToRGB(hue, sat, val);
            }
        }
    }
}