using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace PawnSystem.Network
{

    /// <summary>
    /// Interface for MonoBehaviour-like component responsible for player input handling.
    /// UpdateControls should update local input and update local controls state but should not assign it to receivers.
    /// EvaluateControls should assign local controls state to recievers (called by NetworkPlayerReplicator when data received from network).
    /// StateResync should assign full state to handle initial state synchronization.
    /// </summary>
    public interface IControls
    {
        bool enabled
        {
            get;
            set;
        }

        void UpdateControls();
        void EvaluateControls();
        void StateResync();
    }
    /// <summary>
    /// Use this NetworkBehaviour component to replicate IControls component state.
    /// Implement your [Command]'s and call them OnLocalPlayerNetUpdate to send data to server and then use [ClientRpc] or [SyncVar] to propogate it across other clients.
    /// </summary>
    public abstract class NetworkPlayerReplicator : NetworkPlayerIdentity
    {
        [Range(0, 60f)]
        public int networkSendFrequency = 10;

        public int networkChannel = 1;

        protected static float THRESHOLD = Mathf.Epsilon;

        protected WaitForSeconds networkSendInterval;

        protected IControls inputSource;

        //Sending uNET Commands/RPCs with locally stored values; assigning values immideately if needed
        //NOTE: call it only if isLocalPlayer == true
        virtual public void OnLocalPlayerNetUpdate()
        {
            //CmdSendState(inputSource.state);
        }

        virtual protected void Awake()
        {
            inputSource = GetComponent<IControls>();
            if (inputSource != null)
                inputSource.enabled = false;
        }

        virtual protected void Update()
        {
            if (isLocalPlayer)
            {
                UpdateLocalPlayer();
            }
            else
            if (isClient)
            {
                UpdateRemotePlayer();
            }

            if (isServer)
            {
                UpdateServer();
            }
        }

        virtual protected void UpdateLocalPlayer()
        {
            if (inputSource != null)
            {
                inputSource.UpdateControls();
                inputSource.EvaluateControls();
            }
        }

        virtual protected void UpdateServer()
        {
            if (inputSource != null)
            {
                inputSource.EvaluateControls();
            }
        }

        virtual protected void UpdateRemotePlayer()
        {
            if (inputSource != null)
            {
                inputSource.EvaluateControls();
            }
        }

        override public int GetNetworkChannel()
        {
            return networkChannel;
        }

        override public float GetNetworkSendInterval()
        {
            return 1f/networkSendFrequency;
        }

        override public void OnStartLocalPlayer()
        {
            networkSendInterval = new WaitForSeconds(GetNetworkSendInterval());

            StartCoroutine(Do_NetUpdates());

            base.OnStartLocalPlayer();
        }

        private IEnumerator Do_NetUpdates()
        {
            while (isLocalPlayer && isActiveAndEnabled)
            {
                OnLocalPlayerNetUpdate();
                yield return networkSendInterval;
            }
        }
    }
}
