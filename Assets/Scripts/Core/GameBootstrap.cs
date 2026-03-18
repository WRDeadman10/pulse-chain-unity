using PulseChain.Gameplay;

using UnityEngine;

namespace PulseChain.Core {
    public static class GameBootstrap {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() {
            GameManager existingManager = Object.FindFirstObjectByType<GameManager>();
            if (existingManager != null) {
                return;
            }

            GameObject rootObject = new GameObject("PulseChainRuntime");
            Object.DontDestroyOnLoad(rootObject);

            GameObject uiObject = new GameObject("UIManager");
            uiObject.transform.SetParent(rootObject.transform, false);
            UIManager uiManager = uiObject.AddComponent<UIManager>();

            GameObject nodeSpawnerObject = new GameObject("NodeSpawner");
            nodeSpawnerObject.transform.SetParent(rootObject.transform, false);
            NodeSpawner nodeSpawner = nodeSpawnerObject.AddComponent<NodeSpawner>();

            GameObject pulseControllerObject = new GameObject("PulseController");
            pulseControllerObject.transform.SetParent(rootObject.transform, false);
            PulseController pulseController = pulseControllerObject.AddComponent<PulseController>();

            GameObject audioSystemObject = new GameObject("AudioSystem");
            audioSystemObject.transform.SetParent(rootObject.transform, false);
            AudioSystem audioSystem = audioSystemObject.AddComponent<AudioSystem>();

            GameObject persistenceSystemObject = new GameObject("PersistenceSystem");
            persistenceSystemObject.transform.SetParent(rootObject.transform, false);
            PersistenceSystem persistenceSystem = persistenceSystemObject.AddComponent<PersistenceSystem>();

            GameObject adHooksObject = new GameObject("AdHooksSystem");
            adHooksObject.transform.SetParent(rootObject.transform, false);
            AdHooksSystem adHooksSystem = adHooksObject.AddComponent<AdHooksSystem>();

            GameObject managerObject = new GameObject("GameManager");
            managerObject.transform.SetParent(rootObject.transform, false);
            GameManager gameManager = managerObject.AddComponent<GameManager>();

            gameManager.Configure(uiManager, nodeSpawner, pulseController, audioSystem, persistenceSystem, adHooksSystem);
        }
    }
}