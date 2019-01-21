using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace PawnSystem.Network
{
    /// <summary>
    /// Simple helper class for enabling local player specific objects
    /// </summary>
    public class NetworkPlayerIdentity : NetworkBehaviour
    {

        public List<GameObject> enableOnOwnerGameObjects = new List<GameObject>();

        public List<GameObject> instantiateForLocalOwner = new List<GameObject>();

        public bool destroyOnUnpossess;

        private bool _isLocalPlayer;

        protected bool IsLocalPlayer
        {
            get
            {
                return _isLocalPlayer;
            }

            set
            {
                if (value != _isLocalPlayer)
                {
                    if (value)
                    {
                        //Debug.Log(gameObject.name + " has become local player object");

                        SetActiveOwnerSpecificObjects(true);

                        InstantiateOwnerSpecificObjects();
                    }
                    else
                    {
                        //Debug.Log(gameObject.name + " has become NON local player object");

                        SetActiveOwnerSpecificObjects(false);
                    }
                }
                _isLocalPlayer = value;
            }
        }

        void SetActiveOwnerSpecificObjects(bool b)
        {
            foreach (GameObject go in enableOnOwnerGameObjects)
            {
                go.SetActive(b);
            }
        }

        bool wasLocalObjectCreated;

        void InstantiateOwnerSpecificObjects()
        {
            if (wasLocalObjectCreated)
                return;

            foreach (GameObject go in instantiateForLocalOwner)
            {
                GameObject newGo = GameObject.Instantiate(go) as GameObject;
                newGo.transform.parent = transform;
                newGo.transform.localPosition = go.transform.localPosition;
                newGo.transform.localRotation = go.transform.localRotation;
                newGo.name = go.name;
                enableOnOwnerGameObjects.Add(newGo);
            }
            wasLocalObjectCreated = true;
        }

        protected bool _abandoned;

        [Command]
        void Cmd_ReplacePlayerForConnection(NetworkIdentity netIdentity, NetworkIdentity targetNetIdentity)
        {
            if (targetNetIdentity != null && targetNetIdentity.playerControllerId != -1)
            {
                if (!targetNetIdentity.GetComponent<NetworkPlayerIdentity>()._abandoned)
                {
                    Debug.LogError(targetNetIdentity.name + " already has playerControllerId = " + targetNetIdentity.playerControllerId + " and cannot be possesed by " + netIdentity.connectionToClient.address);
                    return;
                }
                else
                {
                    targetNetIdentity.GetComponent<NetworkPlayerIdentity>()._abandoned = false;
                }
            }

            if (targetNetIdentity == null || targetNetIdentity.gameObject != gameObject)
            {
                _abandoned = true;
                NetworkConnection connectionToClient = netIdentity.connectionToClient;
                if (targetNetIdentity != null)
                    NetworkServer.ReplacePlayerForConnection(connectionToClient, targetNetIdentity.gameObject, netIdentity.playerControllerId);
                else
                {
                    //NetworkServer.DestroyPlayersForConnection(connectionToClient);
                    /*
                    foreach (PlayerController pc in connectionToClient.playerControllers)
                    {
                        if (pc.playerControllerId == netIdentity.playerControllerId)
                        {
                            pc.gameObject = null;
                            break;
                        }
                    }*/

                    //NetworkServer.ReplacePlayerForConnection(connectionToClient, null, netIdentity.playerControllerId);

                    GameObject playerDefault = GameObject.Instantiate(NetworkManager.singleton.playerPrefab, Camera.main.transform.position, Camera.main.transform.rotation);
                    NetworkServer.ReplacePlayerForConnection(connectionToClient, playerDefault, netIdentity.playerControllerId);



                    //PlayerController fakePlayerController = new PlayerController();
                    //NetworkServer.ReplacePlayerForConnection(connectionToServer, gameObject, connectionToServer.playerControllers[0].playerControllerId);
                    //Debug.Log("connectionToServer.playerControllers[0].playerControllerId = "+ connectionToServer.playerControllers[0].playerControllerId);
                }

                netIdentity.SendMessage("OnPlayerUnPossess", connectionToClient, SendMessageOptions.DontRequireReceiver);
            }
        }

        public void SendCmd_ReplacePlayerForConnection(NetworkIdentity targetNetIdentity = null)
        {
            if (isLocalPlayer)
            {
                Cmd_ReplacePlayerForConnection(GetComponent<NetworkIdentity>(), targetNetIdentity);
            }
        }

        public void Possess(int controllerNum = 0)
        {
            NetworkManager.singleton.client.connection.playerControllers[controllerNum].gameObject.GetComponent<NetworkPlayerIdentity>().SendCmd_ReplacePlayerForConnection(GetComponent<NetworkIdentity>());
        }

        public void UnPossess(int controllerNum = 0)
        {
            NetworkManager.singleton.client.connection.playerControllers[controllerNum].gameObject.GetComponent<NetworkPlayerIdentity>().SendCmd_ReplacePlayerForConnection();

            //ClientScene.AddPlayer(GetComponent<NetworkIdentity>().playerControllerId);
        }

        protected void OnPlayerUnPossess(NetworkConnection connectionToClient)
        {
            Debug.Log(gameObject.name + " was UnPossessed by " + connectionToClient.address);
            if (destroyOnUnpossess)
            {
                Destroy(gameObject);
            }
        }

        override public void OnStartLocalPlayer()
        {
            //Debug.Log(gameObject.name +" OnStartLocalPlayer "+ isLocalPlayer);
            base.OnStartLocalPlayer();

            IsLocalPlayer = true;

            SetActiveOwnerSpecificObjects(true);
        }

        NetworkIdentity netIdentity;

        private void Awake()
        {
            netIdentity = GetComponent<NetworkIdentity>();
            if (netIdentity == null)
            {
                enabled = false;
            }
            else
                SetActiveOwnerSpecificObjects(false);
        }

        void Update()
        {
            IsLocalPlayer = isLocalPlayer;
        }
    }
}