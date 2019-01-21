using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.SceneManagement;

namespace PawnSystem.Network
{

    public class ServerAutostart : MonoBehaviour
    {

        public int networkManagerSceneIndex;

        public bool startAsHost;

        private void Awake()
        {
            GameObject.DontDestroyOnLoad(gameObject);

            if (NetworkManager.singleton == null)
            {
                string sceneName = SceneManager.GetActiveScene().name;

                if (networkManagerSceneIndex != SceneManager.GetActiveScene().buildIndex)
                {
                    StartCoroutine(DO_AsyncLoadScene(networkManagerSceneIndex, LoadSceneMode.Additive, success =>
                    {
                        if (success)
                        {
                            Debug.LogWarning("ServerAutostart: loaded scene #" + networkManagerSceneIndex + ". StartServer()");

                            StartServer();

                            SceneManager.UnloadSceneAsync(0);

                            NetworkManager.networkSceneName = sceneName;
                        }
                    }
                    ));
                }
                else
                {
                    Debug.LogError("ServerAutostart: scene #" + networkManagerSceneIndex + " is already loaded and can't be started as offline scene. Create separate offline scene");
                }
            }
            else
            {
                if (!NetworkManager.singleton.isNetworkActive)
                {
                    Debug.LogWarning("ServerAutostart: isNetworkActive FALSE. StartHost()");
                    StartServer();
                }
                Destroy(gameObject);
            }
        }

        void StartServer()
        {
            if (startAsHost)
                NetworkManager.singleton.StartHost();
            else
                NetworkManager.singleton.StartServer();
        }

        IEnumerator DO_AsyncLoadScene(int n, LoadSceneMode loadSceneMode, System.Action<bool> onSuccess = null)
        {
            var operation = SceneManager.LoadSceneAsync(n, loadSceneMode); //SceneManager.LoadSceneAsync(n, loadSceneMode);
            while (!operation.isDone)
            {
                yield return null;
            }

            if (onSuccess != null)
            {
                onSuccess.Invoke(true);
            }
        }
    }
}