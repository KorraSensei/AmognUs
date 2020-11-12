using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


public class NetworkManagerAmognus : NetworkManager
{
    [SerializeField] private int minPlayers = 2;
    [Scene] [SerializeField] private string menuScene = string.Empty;
    
    [Header("Room")]
    [SerializeField] private NetworkRoomPlayerAmognus roomPlayerPrefab = null;

    [Header("game")]
    [SerializeField] private NetworkGamePlayerAmognus gamePlayerPrefab = null;
    [SerializeField] private GameObject playerSpawnSystem = null;

    public static event Action OnClientConnected;
    public static event Action OnClientDisconnected;
    public static event Action<NetworkConnection> OnServerReadied;

    public List<NetworkRoomPlayerAmognus> RoomPlayers { get; } = new List<NetworkRoomPlayerAmognus>();

    public List<NetworkGamePlayerAmognus> GamePlayers { get; } = new List<NetworkGamePlayerAmognus>();

    public override void OnStartServer() => spawnPrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs").ToList();
    

    public override void OnStartClient()
    {
        var spawnablePrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs");

        foreach (var prefab in spawnablePrefabs)
        {
            ClientScene.RegisterPrefab(prefab);
        }
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);

        OnClientConnected?.Invoke();
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);

        OnClientDisconnected?.Invoke();
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        if (numPlayers >= maxConnections)
        {
            conn.Disconnect();
            return;
        }

        if ("Assets/Scenes/" + SceneManager.GetActiveScene().name + ".unity" != menuScene)
        {
            conn.Disconnect();
            return;
        }
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        if ("Assets/Scenes/" + SceneManager.GetActiveScene().name + ".unity" == menuScene)
        {
            bool isLeader = RoomPlayers.Count == 0;

            NetworkRoomPlayerAmognus roomPlayerInstance = Instantiate(roomPlayerPrefab);

            roomPlayerInstance.IsLeader = isLeader;

            NetworkServer.AddPlayerForConnection(conn, roomPlayerInstance.gameObject);
        }
    }

    public override void OnServerDisconnect(NetworkConnection conn) {
        if (conn.identity != null) {
            var player = conn.identity.GetComponent<NetworkRoomPlayerAmognus>();

            RoomPlayers.Remove(player);

            NotifyPlayersOfReadyState();


        }

        base.OnServerDisconnect(conn);
    }

    public override void OnStopServer() {
        RoomPlayers.Clear();
    }

    public void NotifyPlayersOfReadyState(){
        foreach (var player in RoomPlayers) {
            player.HandleReadyToStart(IsReadyToStart());
        }

    }

    private bool IsReadyToStart() {
        if (numPlayers < minPlayers ) {return false;}

        foreach (var player in RoomPlayers) {
            if (!player.IsReady) { return false; }
        }
        return true;
    }

    public void StartGame() {
        if ("Assets/Scenes/" + SceneManager.GetActiveScene().name + ".unity" == menuScene) {
            if (!IsReadyToStart()) {return;}

            ServerChangeScene("Amogn_us");
        }
    }

    public override void ServerChangeScene(string newSceneName)
    {
        // From menu to game
        if ("Assets/Scenes/" + SceneManager.GetActiveScene().name + ".unity" == menuScene && newSceneName.StartsWith("Amogn_us"))
        {
            for (int i = RoomPlayers.Count - 1; i >= 0; i--)
            {
                var conn = RoomPlayers[i].connectionToClient;
                var gameplayerInstance = Instantiate(gamePlayerPrefab);
                gameplayerInstance.SetDisplayName(RoomPlayers[i].DisplayName);

                NetworkServer.Destroy(conn.identity.gameObject);

                NetworkServer.ReplacePlayerForConnection(conn, gameplayerInstance.gameObject, true);
            }
        }

        base.ServerChangeScene(newSceneName);
    }

    public override void OnServerSceneChanged(string sceneName) {
        if (sceneName.StartsWith("Amogn_us")) {
            GameObject playerSpawnSystemInstance = Instantiate(playerSpawnSystem);
            NetworkServer.Spawn(playerSpawnSystemInstance);
        }
    }

    public override void OnServerReady(NetworkConnection conn){
        base.OnServerReady(conn);

        OnServerReadied?.Invoke(conn);
    }     

}
