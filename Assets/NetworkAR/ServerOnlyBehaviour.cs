using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace PawnSystem.Network
{
    public class ServerOnlyBehaviour : MonoBehaviour
    {

        public bool enableIfHost;
        public bool enableIfOffline;
        // Use this for initialization
        void Start()
        {
            if (NetworkServer.active)
            {
                if (!NetworkClient.active || enableIfHost)
                {
                    if (!isActiveAndEnabled)
                    {
                        Debug.Log(gameObject.name + " enabled (ServerOnly)");
                        gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (isActiveAndEnabled)
                    {
                        Debug.Log(gameObject.name + " disabled (ServerOnly)");
                        gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                if (NetworkManager.singleton != null)
                {
                    if (isActiveAndEnabled)
                    {
                        Debug.Log(gameObject.name + " disabled (ServerOnly)");
                        gameObject.SetActive(false);
                    }
                }
                else
                {
                    //LOCAL GAME?
                    if (!enableIfOffline && isActiveAndEnabled)
                    {
                        Debug.Log(gameObject.name + " object (ServerOnly)");
                        gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}