using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace MarsFPSKit.Services
{
    /// <summary>
    /// This handles the UGS login.
    /// </summary>
    public class Kit_UGS : Kit_Base
    {
        public static Kit_UGS instance;
        /// <summary>
        /// Are we currently logged in?
        /// </summary>
        public bool isLoggedIn;

        async void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
                return;
            }

            //Assign singleton
            instance = this;
            DontDestroyOnLoad(gameObject);

            try
            {
                DebugLog("Initializing UGS.", this);
                await UnityServices.InitializeAsync();
                DebugLog("Signing in anonymously.", this);
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                DebugLog("Logged into UGS, player ID: " + AuthenticationService.Instance.PlayerId, this);
                isLoggedIn = true;
            }
            catch (Exception e)
            {
                isLoggedIn = false;
                DebugLog(e.ToString(), this);
            }
        }
    }
}