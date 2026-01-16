using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerController2D : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 8f;

    [SerializeField] private GameObject cameraPrefab;
    private bool cameraSpawned = false;
    private GameObject spawnedCamera; // Store reference to destroy it later

    private int currentTick;
    private float tickTimer;

    // --- CLIENT ---
    private Vector2 currentInput;
    private Vector3 predictedPosition;

    // Store predicted states and inputs
    private Dictionary<int, Vector3> predictionHistory = new();
    private Dictionary<int, Vector2> inputHistory = new();

    // --- SERVER ---
    private Vector3 serverPosition;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[OnNetworkSpawn] {gameObject.name} - IsOwner: {IsOwner}, IsClient: {IsClient}, IsServer: {IsServer}, OwnerClientId: {OwnerClientId}, LocalClientId: {NetworkManager.Singleton.LocalClientId}");

        // Initialize positions to current transform to avoid (0,0,0) snaps
        predictedPosition = transform.position;
        serverPosition = transform.position;

        // For clients, ownership might not be set immediately
        // The Update loop will catch it when IsOwner becomes true
        if (IsOwner)
        {
            Debug.Log($"[OnNetworkSpawn] Attempting to spawn camera for {gameObject.name}");
            SpawnCamera();
        }
        else if (IsClient)
        {
            Debug.Log($"[OnNetworkSpawn] Client but not owner yet, will check in Update");
        }
    }

    public override void OnGainedOwnership()
    {
        Debug.Log($"[OnGainedOwnership] {gameObject.name} gained ownership!");
        SpawnCamera();
    }

    public override void OnNetworkDespawn()
    {
        // Clean up the camera when this player object is despawned
        if (spawnedCamera != null)
        {
            Debug.Log($"[OnNetworkDespawn] Destroying camera for {gameObject.name}");
            Destroy(spawnedCamera);
            spawnedCamera = null;
            cameraSpawned = false;
        }
    }

    private void SpawnCamera()
    {
        Debug.Log($"[SpawnCamera] Called - cameraSpawned: {cameraSpawned}, cameraPrefab null: {cameraPrefab == null}");

        if (cameraSpawned)
        {
            Debug.Log("[SpawnCamera] Camera already spawned, returning");
            return;
        }

        // Safety: Ensure only one local camera exists in the scene
        GameObject existingCam = GameObject.Find("PlayerCamera_Local");
        if (existingCam != null)
        {
            Debug.Log($"[SpawnCamera] Found existing camera: {existingCam.name}, marking as spawned");
            cameraSpawned = true;
            return;
        }

        if (cameraPrefab == null)
        {
            Debug.LogError("[SpawnCamera] Camera prefab is NULL! Assign it in the Inspector!");
            return;
        }

        Debug.Log($"[SpawnCamera] Instantiating camera for {gameObject.name}");
        GameObject cam = Instantiate(cameraPrefab);
        cam.name = "PlayerCamera_Local";

        // Store reference so we can destroy it later
        spawnedCamera = cam;

        // CRITICAL: Mark as DontDestroyOnLoad to prevent scene cleanup from destroying it
        DontDestroyOnLoad(cam);
        Debug.Log("[SpawnCamera] Camera marked as DontDestroyOnLoad");

        // CRITICAL: Disable any other AudioListeners in the scene (like on Main Camera)
        AudioListener[] allListeners = FindObjectsOfType<AudioListener>();
        Debug.Log($"[SpawnCamera] Found {allListeners.Length} AudioListeners in scene");

        foreach (AudioListener listener in allListeners)
        {
            // Keep the one on our camera, disable all others
            if (listener.gameObject != cam && !listener.transform.IsChildOf(cam.transform))
            {
                Debug.Log($"[SpawnCamera] Disabling AudioListener on {listener.gameObject.name}");
                listener.enabled = false;
            }
        }

        if (cam.TryGetComponent<CameraFollow2D>(out var follow))
        {
            follow.SetTarget(transform);
            Debug.Log("[SpawnCamera] CameraFollow2D found and target set");
        }
        else
        {
            Debug.LogWarning("[SpawnCamera] No CameraFollow2D component found on camera prefab");
        }

        cameraSpawned = true;
        Debug.Log($"[SpawnCamera] SUCCESS! Camera spawned for {gameObject.name}");

        // Verify it still exists after a frame
        StartCoroutine(VerifyCameraExists(cam));
    }

    private System.Collections.IEnumerator VerifyCameraExists(GameObject cam)
    {
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForSeconds(0.1f);
            if (cam == null)
            {
                Debug.LogError($"[SpawnCamera] CAMERA WAS DESTROYED after {(i + 1) * 0.1f}s!");
                cameraSpawned = false; // Allow retry
                yield break;
            }
        }
        Debug.Log("[SpawnCamera] Camera verified - still exists after 1 second");
    }

    void Update()
    {
        // Keep checking for ownership in Update as a fallback
        if (IsOwner && !cameraSpawned)
        {
            Debug.Log($"[Update] IsOwner is true but camera not spawned, attempting spawn...");
            SpawnCamera();
        }

        tickTimer += Time.deltaTime;
        while (tickTimer >= NetConfig.TICK_DELTA)
        {
            tickTimer -= NetConfig.TICK_DELTA;
            Tick();
        }
    }

    void Tick()
    {
        currentTick++;

        if (IsOwner)
        {
            ReadInput();
            ClientPredictMovement();
            SendInputServerRpc(currentInput, currentTick);
        }

        // Server only simulates for clients, NOT for host (host uses client prediction)
        if (IsServer && !IsOwner)
        {
            ServerSimulateMovement();
        }
    }

    // =========================
    // CLIENT
    // =========================

    void ReadInput()
    {
        currentInput.x = Input.GetAxisRaw("Horizontal");
        currentInput.y = Input.GetAxisRaw("Vertical");
        currentInput = Vector2.ClampMagnitude(currentInput, 1f);
    }

    void ClientPredictMovement()
    {
        Vector3 delta =
            (Vector3)(currentInput * moveSpeed * NetConfig.TICK_DELTA);

        predictedPosition += delta;
        transform.position = predictedPosition;

        predictionHistory[currentTick] = predictedPosition;
        inputHistory[currentTick] = currentInput;
    }

    // =========================
    // SERVER
    // =========================

    private Vector2 lastReceivedInput;

    [ServerRpc]
    void SendInputServerRpc(Vector2 input, int tick)
    {
        // --- Validation ---
        if (input.magnitude > 1.01f)
            return;

        lastReceivedInput = input;
    }

    void ServerSimulateMovement()
    {
        Vector3 delta =
            (Vector3)(lastReceivedInput * moveSpeed * NetConfig.TICK_DELTA);

        serverPosition += delta;
        transform.position = serverPosition;

        SendStateClientRpc(serverPosition, currentTick);
    }

    // =========================
    // RECONCILIATION
    // =========================

    [ClientRpc]
    void SendStateClientRpc(Vector3 serverPos, int serverTick)
    {
        // Only reconcile for non-host clients
        if (!IsOwner || IsHost) return;

        if (!predictionHistory.TryGetValue(serverTick, out Vector3 predicted))
            return;

        float error = Vector3.Distance(serverPos, predicted);

        if (error > 0.01f)
        {
            // Correct position to server's authoritative position
            predictedPosition = serverPos;

            // Re-simulate all inputs that happened AFTER the server tick
            var sortedKeys = new System.Collections.Generic.List<int>(inputHistory.Keys);
            sortedKeys.Sort();

            foreach (var tick in sortedKeys)
            {
                if (tick > serverTick)
                {
                    Vector2 input = inputHistory[tick];
                    Vector3 delta = (Vector3)(input * moveSpeed * NetConfig.TICK_DELTA);
                    predictedPosition += delta;
                }
            }

            transform.position = predictedPosition;
        }

        // Cleanup old history
        var keysToRemove = new System.Collections.Generic.List<int>();
        foreach (var key in predictionHistory.Keys)
        {
            if (key <= serverTick)
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            predictionHistory.Remove(key);
            inputHistory.Remove(key);
        }
    }
}