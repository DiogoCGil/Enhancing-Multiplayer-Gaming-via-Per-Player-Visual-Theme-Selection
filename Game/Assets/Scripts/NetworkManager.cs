using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;
using System.Runtime.Serialization;

public class NetworkManager : MonoBehaviourPunCallbacks
{

    [SerializeField]
    private Text connectionText;
    [SerializeField]
    private Transform[] spawnPoints;
    [SerializeField]
    private Camera sceneCamera;
    [SerializeField]
    public GameObject playerModel;
    [SerializeField]
    private GameObject serverWindow;
    [SerializeField]
    private GameObject messageWindow;
    [SerializeField]
    private GameObject sightImage;
    [SerializeField]
    private InputField username;
    [SerializeField]
    private InputField roomName;
    [SerializeField]
    private InputField roomList;
    [SerializeField]
    private InputField messagesLog;

    private GameObject player;
    private Queue<string> messages;
    private const int messageCount = 10;
    private string nickNamePrefKey = "PlayerName";
    public GameGraphicChanger gameGraphicChanger;
    private int spawnIndex;
    private List<GameObject> onlyEnemyPlayers = new List<GameObject>();
    public GameObject canvas;
    public GameObject cameraToDeactivate;

    public GameObject objectToActivate1;
    public GameObject objectToActivate5;
    public GameObject rainParticles;
    public GameObject MainMenu;
    public GameObject Buttons;
    public GameObject AmbientSounds;

    public int totalKillCount = 0;
    public int currentScore;
    public int CurrentKills;
    private List<GameObject> currentPlayers = new List<GameObject>();
    private static HashSet<int> reservedSpawnIndices = new HashSet<int>();

    public GameObject x;

    public float totalTimeAliveMain = 0f;


    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        messages = new Queue<string>(messageCount);
        if (PlayerPrefs.HasKey(nickNamePrefKey))
        {
            username.text = PlayerPrefs.GetString(nickNamePrefKey);
        }
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = "2.1.3";
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "eu";
        PhotonNetwork.ConnectUsingSettings();
        connectionText.text = "Connecting to lobby...";

    }

    /// <summary>
    /// Called on the client when you have successfully connected to a master server.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }
    void Update()
    {
        if (!PhotonNetwork.InRoom) return;

        currentPlayers = GameObject.FindGameObjectsWithTag("Player").ToList();

        foreach (GameObject player in currentPlayers)
        {
            var health = player.GetComponent<PlayerHealth>();
            if (health != null && health.isDead)
            {
                SetLayerRecursively(player, LayerMask.NameToLayer("Ignore Raycast"));
            }
        }
    }
    /// <summary>
    /// Called on the client when the connection was lost or you disconnected from the server.
    /// </summary>
    /// <param name="cause">DisconnectCause data associated with this disconnect.</param>
    public override void OnDisconnected(DisconnectCause cause)
    {
        connectionText.text = cause.ToString();
    }

    /// <summary>
    /// Callback function on joined lobby.
    /// </summary>
    public override void OnJoinedLobby()
    {
        PhotonNetwork.FetchServerTimestamp();
        serverWindow.SetActive(true);
        connectionText.text = "";
    }

    /// <summary>
    /// Callback function on reveived room list update.
    /// </summary>
    /// <param name="rooms">List of RoomInfo.</param>
    public override void OnRoomListUpdate(List<RoomInfo> rooms)
    {
        roomList.text = "";
        foreach (RoomInfo room in rooms)
        {
            roomList.text += room.Name + "\n";
        }
    }

    /// <summary>
    /// The button click callback function for join room.
    /// </summary>
    public void JoinRoom()
    {
        PhotonNetwork.FetchServerTimestamp();
        serverWindow.SetActive(false);
        connectionText.text = "Joining room...";

        PhotonNetwork.LocalPlayer.NickName = username.text;
        PlayerPrefs.SetString(nickNamePrefKey, username.text);
        RoomOptions roomOptions = new RoomOptions()
        {
            IsVisible = true,
            MaxPlayers = 4
        };
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinOrCreateRoom(roomName.text, roomOptions, TypedLobby.Default);
        }
        else
        {
            connectionText.text = "PhotonNetwork connection is not ready, try restart it.";
        }
    }

    /// <summary>
    /// Callback function on joined room.
    /// </summary>
    public override void OnJoinedRoom()
    {
        connectionText.text = "";
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Respawn(0.0f);
    }

    /// <summary>
    /// Start spawn or respawn a player.
    /// </summary>
    /// <param name="spawnTime">Time waited before spawn a player.</param>
    void Respawn(float spawnTime)
    {
        sightImage.SetActive(false);
        sceneCamera.enabled = true;
        sceneCamera.GetComponent<AudioListener>().enabled = true;
        StartCoroutine(RespawnCoroutine(spawnTime));
    }

    /// <summary>
    /// The coroutine function to spawn player.
    /// </summary>
    /// <param name="spawnTime">Time waited before spawn a player.</param>
    IEnumerator RespawnCoroutine(float spawnTime)
    {
        yield return new WaitForSeconds(spawnTime);

        onlyEnemyPlayers.Clear();
        GameObject[] existingPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject obj in existingPlayers)
        {
            PhotonView view = obj.GetComponent<PhotonView>();
            if (view != null && !view.IsMine)
                onlyEnemyPlayers.Add(obj);
        }

        List<int> candidateIndices = Enumerable.Range(0, spawnPoints.Length)
                                               .Where(i => !reservedSpawnIndices.Contains(i))
                                               .ToList();
        if (candidateIndices.Count == 0)
        {
            reservedSpawnIndices.Clear();
            candidateIndices = Enumerable.Range(0, spawnPoints.Length).ToList();
        }

        if (onlyEnemyPlayers.Count == 0)
        {
            spawnIndex = candidateIndices[Random.Range(0, candidateIndices.Count)];
        }
        else
        {
            float maxMinDistance = float.MinValue;
            int bestIndex = candidateIndices[0];
            foreach (int i in candidateIndices)
            {
                float minDist = onlyEnemyPlayers
                    .Where(e => e != null)
                    .Min(e => Vector3.Distance(spawnPoints[i].position, e.transform.position));
                if (minDist > maxMinDistance)
                {
                    maxMinDistance = minDist;
                    bestIndex = i;
                }
            }
            spawnIndex = bestIndex;
        }

        reservedSpawnIndices.Add(spawnIndex);

        player = PhotonNetwork.Instantiate(playerModel.name, spawnPoints[spawnIndex].position, spawnPoints[spawnIndex].rotation, 0);
        x.SetActive(true);


        if (player.GetComponent<PhotonView>().IsMine)
        {
            // Atualiza o Target da MiniMapCam
            GameObject minimapCam = GameObject.FindGameObjectWithTag("MiniMapCam");
            if (minimapCam != null)
            {
                MapCam mapCamScript = minimapCam.GetComponent<MapCam>();
                if (mapCamScript != null)
                {
                    mapCamScript.target = player.transform;
                }
            }

            // Atualiza o ícone do jogador no minimapa


            Transform iconTransform = FindChildWithTag(player.transform, "Icon");
            if (iconTransform != null)
            {
                SpriteRenderer iconRenderer = iconTransform.GetComponent<SpriteRenderer>();
                if (iconRenderer != null)
                {
                    Sprite newIcon = Resources.Load<Sprite>("Icons_Player");
                    if (newIcon != null)
                    {
                        iconRenderer.sprite = newIcon;
                    }
                    else
                    {
                        Debug.LogWarning("Ícone 'Icons_Player' não encontrado nos Resources.");
                    }
                }
            }
            Transform particals = player.transform.Find("Particals");
            particals.gameObject.SetActive(true);


            FindObjectOfType<Menu>()?.OnPlayerRespawn();
        }
        x.SetActive(true);
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        playerHealth.RespawnEvent += Respawn;
        playerHealth.AddMessageEvent += AddMessage;
        sceneCamera.enabled = false;
        sceneCamera.GetComponent<AudioListener>().enabled = false;
        if (spawnTime == 0)
        {
            AddMessage("Player " + PhotonNetwork.LocalPlayer.NickName + " Joined Game.");
        }
        else
        {
            AddMessage("Player " + PhotonNetwork.LocalPlayer.NickName + " Respawned.");
        }
        StartCoroutine(ReleaseSpawnReserveAfterDelay(spawnIndex, 3f));
    }
    private IEnumerator ReleaseSpawnReserveAfterDelay(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        reservedSpawnIndices.Remove(index);
    }

    /// <summary>
    /// Add message to message panel.
    /// </summary>
    /// <param name="message">The message that we want to add.</param>
    void AddMessage(string message)
    {
        photonView.RPC("AddMessage_RPC", RpcTarget.All, message);
    }

    /// <summary>
    /// RPC function to call add message for each client.
    /// </summary>
    /// <param name="message">The message that we want to add.</param>
    [PunRPC]
    void AddMessage_RPC(string message)
    {
        messages.Enqueue(message);
        if (messages.Count > messageCount)
        {
            messages.Dequeue();
        }
        messagesLog.text = "";
        foreach (string m in messages)
        {
            messagesLog.text += m + "\n";
        }


        CheckLastKillMessage();



    }

    /// <summary>
    /// Callback function when other player disconnected.
    /// </summary>
    public override void OnPlayerLeftRoom(Player other)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AddMessage("Player " + other.NickName + " Left Game.");
        }
    }
    private Transform FindChildWithTag(Transform parent, string tag)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag))
            {
                return child;
            }
            else
            {
                Transform found = FindChildWithTag(child, tag);
                if (found != null) return found;
            }
        }
        return null;
    }

    [PunRPC]
    void CheckLastKillMessage()
    {
        GameObject textPanelObj = GameObject.FindGameObjectWithTag("Texto");
        if (textPanelObj == null)
        {
            Debug.LogError("GameObject com a tag 'Texto' não encontrado.");
            return;
        }

        if (!textPanelObj.TryGetComponent<Text>(out var messageText))
        {
            Debug.LogError("O GameObject 'Texto' não tem um componente Text.");
            return;
        }

        if (string.IsNullOrEmpty(messageText.text))
        {
            Debug.LogWarning("O texto da UI está vazio.");
            return;
        }


        string[] messages = messageText.text.Split('\n', System.StringSplitOptions.RemoveEmptyEntries);
        if (messages.Length == 0)
        {
            Debug.LogWarning("Nenhuma mensagem válida encontrada.");
            return;
        }

        string lastMessage = messages[messages.Length - 1].Trim();

        if (string.IsNullOrEmpty(lastMessage))
        {
            Debug.LogWarning("A última mensagem ainda está vazia após limpeza.");
            return;
        }

        if (lastMessage.Contains("was killed by"))
        {

            int index = lastMessage.IndexOf("was killed by");
            if (index != -1)
            {
                string killerName = lastMessage.Substring(index + "was killed by".Length).Trim();

                killerName = killerName.TrimEnd('!');

                if (killerName == PhotonNetwork.NickName)
                {
                    int pointsToAdd = 1;
                    AddScore(pointsToAdd);
                }


                if (PhotonNetwork.IsMasterClient)
                {
                    killCountMais1();
                    photonView.RPC("UpdateKillCount", RpcTarget.AllBuffered, totalKillCount);
                }

            }
            else
            {
                Debug.LogError("Formato inesperado na mensagem de kill.");
            }
        }
    }

    void AddScore(int points)
    {
        GameObject scoreObj = GameObject.FindGameObjectWithTag("Score");
        if (scoreObj != null && scoreObj.TryGetComponent<TMPro.TextMeshProUGUI>(out var scoreText))
        {
            currentScore += points;
            UpdateMyKillCount();
            scoreText.text = $"{currentScore}";

        }
        else
        {
            Debug.LogError("Elemento com tag 'Score' não encontrado ou sem TextMeshProUGUI.");
        }
    }

    public void backButton()
    {
        UpdateUIAndEnvironment();
    }
    private void UpdateUIAndEnvironment()
    {
        canvas.SetActive(false);
        sceneCamera.gameObject.SetActive(false);
        cameraToDeactivate.SetActive(true);
        objectToActivate1.SetActive(false);
        objectToActivate5.SetActive(false);
        rainParticles.SetActive(false);
        Buttons.SetActive(true);
        MainMenu.SetActive(true);
        AmbientSounds.SetActive(false);

    }
    void killCountMais1()
    {
        totalKillCount += 1;
    }
    [PunRPC]
    void UpdateKillCount(int amount)
    {
        totalKillCount = amount;

    }
    public void UpdateMyKillCount()
    {
        CurrentKills += 1;
    }

  
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        if (obj.CompareTag("Coliders"))
        {
            ApplyLayerRecursively(obj, newLayer);
        }
        else
        {
            foreach (Transform child in obj.transform)
            {
                if (child != null)
                {
                    SetLayerRecursively(child.gameObject, newLayer);
                }
            }
        }
    }

    private void ApplyLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                ApplyLayerRecursively(child.gameObject, newLayer);
            }
        }
    }
} 