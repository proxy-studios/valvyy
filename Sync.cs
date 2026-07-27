using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class Sync : MonoBehaviour
{
    [Header("Player Settings")]
    public GameObject PlayerObj;
    public string PlayerTag = "Player";

    [Header("For vr (optinal)")]
    public Transform localHead;
    public Transform localLeftHand;
    public Transform localRightHand;

    private GameObject localPlayerObject;

    private readonly Dictionary<int, GameObject> spawnedRemotePlayers = new Dictionary<int, GameObject>();

    // Body targets
    private readonly Dictionary<int, Vector3> targetPositions = new Dictionary<int, Vector3>();
    private readonly Dictionary<int, Quaternion> targetRotations = new Dictionary<int, Quaternion>();

    // VR Hand targets
    private readonly Dictionary<int, Vector3> targetLeftHandPos = new Dictionary<int, Vector3>();
    private readonly Dictionary<int, Quaternion> targetLeftHandRot = new Dictionary<int, Quaternion>();
    private readonly Dictionary<int, Vector3> targetRightHandPos = new Dictionary<int, Vector3>();
    private readonly Dictionary<int, Quaternion> targetRightHandRot = new Dictionary<int, Quaternion>();

    private readonly HashSet<int> activeSessionPlayerIds = new HashSet<int>();

    private float syncTimer;

    private void Start()
    {
        FindLocalPlayer();
    }

    public void FindLocalPlayer()
    {
        if (!string.IsNullOrEmpty(PlayerTag))
        {
            localPlayerObject = GameObject.FindGameObjectWithTag(PlayerTag);
        }

        if (localPlayerObject == null && Camera.main != null && Camera.main.transform.root != null)
        {
            localPlayerObject = Camera.main.transform.root.gameObject;
        }

        // Auto-find VR head/hands if not manually assigned
        if (localPlayerObject != null)
        {
            if (localHead == null && Camera.main != null) localHead = Camera.main.transform;
        }
    }

    private void Update()
    {
        // 1. Send local player transform (Supports PC, Console, Mobile, and VR)
        if (NetworkManager.Instance != null && NetworkManager.Instance.isClient && NetworkManager.Instance.localClientId != -1)
        {
            if (localPlayerObject == null) FindLocalPlayer();

            if (localPlayerObject != null)
            {
                syncTimer += Time.deltaTime;
                if (syncTimer >= 0.033f) // ~30 FPS network pulse
                {
                    syncTimer = 0f;

                    Vector3 pos = localPlayerObject.transform.position;
                    Quaternion rot = localPlayerObject.transform.rotation;

                    // Check if local player is using VR hands
                    if (localLeftHand != null && localRightHand != null)
                    {
                        Vector3 lhPos = localLeftHand.localPosition;
                        Quaternion lhRot = localLeftHand.localRotation;
                        Vector3 rhPos = localRightHand.localPosition;
                        Quaternion rhRot = localRightHand.localRotation;

                        string vrPosData = string.Format(CultureInfo.InvariantCulture,
                            "VRPOS|{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}|{13}|{14}|{15}|{16}|{17}|{18}|{19}|{20}",
                            NetworkManager.Instance.localClientId,
                            pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w,
                            lhPos.x, lhPos.y, lhPos.z, lhRot.x, lhRot.y, lhRot.z, lhRot.w,
                            rhPos.x, rhPos.y, rhPos.z, rhRot.x, rhRot.y, rhRot.z, rhRot.w);

                        NetworkManager.Instance.Client.SendPacket(vrPosData);
                    }
                    else
                    {
                        // Standard PC / Console packet
                        string posData = string.Format(CultureInfo.InvariantCulture, "POS|{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}",
                            NetworkManager.Instance.localClientId, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w);

                        NetworkManager.Instance.Client.SendPacket(posData);
                    }
                }
            }
        }

        // 2. Smoothly lerp remote player clones across all platforms
        foreach (var kvp in spawnedRemotePlayers)
        {
            int id = kvp.Key;
            GameObject remoteGo = kvp.Value;
            if (remoteGo == null) continue;

            // Interpolate Main Body
            if (targetPositions.TryGetValue(id, out Vector3 targetPos))
            {
                remoteGo.transform.position = Vector3.Lerp(remoteGo.transform.position, targetPos, Time.deltaTime * 20f);
            }

            if (targetRotations.TryGetValue(id, out Quaternion targetRot))
            {
                remoteGo.transform.rotation = Quaternion.Slerp(remoteGo.transform.rotation, targetRot, Time.deltaTime * 20f);
            }

            // Interpolate VR Hands (If player is on VR)
            Transform remoteLH = remoteGo.transform.Find("LeftHand") ?? remoteGo.transform.Find("Left Hand");
            if (remoteLH != null && targetLeftHandPos.TryGetValue(id, out Vector3 lhP))
            {
                remoteLH.localPosition = Vector3.Lerp(remoteLH.localPosition, lhP, Time.deltaTime * 20f);
                if (targetLeftHandRot.TryGetValue(id, out Quaternion lhR)) remoteLH.localRotation = Quaternion.Slerp(remoteLH.localRotation, lhR, Time.deltaTime * 20f);
            }

            Transform remoteRH = remoteGo.transform.Find("RightHand") ?? remoteGo.transform.Find("Right Hand");
            if (remoteRH != null && targetRightHandPos.TryGetValue(id, out Vector3 rhP))
            {
                remoteRH.localPosition = Vector3.Lerp(remoteRH.localPosition, rhP, Time.deltaTime * 20f);
                if (targetRightHandRot.TryGetValue(id, out Quaternion rhR)) remoteRH.localRotation = Quaternion.Slerp(remoteRH.localRotation, rhR, Time.deltaTime * 20f);
            }
        }
    }

    public void ProcessPacket(string packet)
    {
        string[] parts = packet.Split('|');
        if (parts.Length == 0) return;

        string cmd = parts[0];

        if (cmd == "INIT" && parts.Length >= 2)
        {
            if (int.TryParse(parts[1], out int id))
            {
                if (NetworkManager.Instance != null) NetworkManager.Instance.localClientId = id;
                activeSessionPlayerIds.Add(id);
                EvaluateSpawns();
            }
        }
        else if (cmd == "SPAWN" && parts.Length >= 2)
        {
            if (int.TryParse(parts[1], out int id))
            {
                activeSessionPlayerIds.Add(id);
                EvaluateSpawns();
            }
        }
        else if (cmd == "DESPAWN" && parts.Length >= 2)
        {
            if (int.TryParse(parts[1], out int id))
            {
                activeSessionPlayerIds.Remove(id);
                DespawnRemotePlayer(id);
                EvaluateSpawns();
            }
        }
        else if (cmd == "POS" && parts.Length >= 9)
        {
            // Standard PC / Console player position packet
            if (int.TryParse(parts[1], out int id) && (NetworkManager.Instance == null || id != NetworkManager.Instance.localClientId))
            {
                if (float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float px) &&
                    float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float py) &&
                    float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float pz) &&
                    float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float rx) &&
                    float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float ry) &&
                    float.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out float rz) &&
                    float.TryParse(parts[8], NumberStyles.Float, CultureInfo.InvariantCulture, out float rw))
                {
                    targetPositions[id] = new Vector3(px, py, pz);
                    targetRotations[id] = new Quaternion(rx, ry, rz, rw);
                }
            }
        }
        else if (cmd == "VRPOS" && parts.Length >= 23)
        {
            // VR platform position packet (Body + Left/Right Hands)
            if (int.TryParse(parts[1], out int id) && (NetworkManager.Instance == null || id != NetworkManager.Instance.localClientId))
            {
                if (float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float px) &&
                    float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float py) &&
                    float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float pz) &&
                    float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float rx) &&
                    float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float ry) &&
                    float.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out float rz) &&
                    float.TryParse(parts[8], NumberStyles.Float, CultureInfo.InvariantCulture, out float rw))
                {
                    targetPositions[id] = new Vector3(px, py, pz);
                    targetRotations[id] = new Quaternion(rx, ry, rz, rw);
                }

                // Parse Left Hand
                if (float.TryParse(parts[9], NumberStyles.Float, CultureInfo.InvariantCulture, out float lhx) &&
                    float.TryParse(parts[10], NumberStyles.Float, CultureInfo.InvariantCulture, out float lhy) &&
                    float.TryParse(parts[11], NumberStyles.Float, CultureInfo.InvariantCulture, out float lhz) &&
                    float.TryParse(parts[12], NumberStyles.Float, CultureInfo.InvariantCulture, out float lhrx) &&
                    float.TryParse(parts[13], NumberStyles.Float, CultureInfo.InvariantCulture, out float lhry) &&
                    float.TryParse(parts[14], NumberStyles.Float, CultureInfo.InvariantCulture, out float lhrz) &&
                    float.TryParse(parts[15], NumberStyles.Float, CultureInfo.InvariantCulture, out float lhrw))
                {
                    targetLeftHandPos[id] = new Vector3(lhx, lhy, lhz);
                    targetLeftHandRot[id] = new Quaternion(lhrx, lhry, lhrz, lhrw);
                }

                // Parse Right Hand
                if (float.TryParse(parts[16], NumberStyles.Float, CultureInfo.InvariantCulture, out float rhx) &&
                    float.TryParse(parts[17], NumberStyles.Float, CultureInfo.InvariantCulture, out float rhy) &&
                    float.TryParse(parts[18], NumberStyles.Float, CultureInfo.InvariantCulture, out float rhz) &&
                    float.TryParse(parts[19], NumberStyles.Float, CultureInfo.InvariantCulture, out float rhrx) &&
                    float.TryParse(parts[20], NumberStyles.Float, CultureInfo.InvariantCulture, out float rhry) &&
                    float.TryParse(parts[21], NumberStyles.Float, CultureInfo.InvariantCulture, out float rhrz) &&
                    float.TryParse(parts[22], NumberStyles.Float, CultureInfo.InvariantCulture, out float rhrw))
                {
                    targetRightHandPos[id] = new Vector3(rhx, rhy, rhz);
                    targetRightHandRot[id] = new Quaternion(rhrx, rhry, rhrz, rhrw);
                }
            }
        }
    }

    private void EvaluateSpawns()
    {
        if (activeSessionPlayerIds.Count <= 1)
        {
            List<int> idsToDespawn = new List<int>(spawnedRemotePlayers.Keys);
            foreach (int id in idsToDespawn)
            {
                DespawnRemotePlayer(id);
            }
        }
        else
        {
            foreach (int id in activeSessionPlayerIds)
            {
                if (NetworkManager.Instance == null || id != NetworkManager.Instance.localClientId)
                {
                    SpawnRemotePlayer(id);
                }
            }
        }
    }

    private void SpawnRemotePlayer(int id)
    {
        if (spawnedRemotePlayers.ContainsKey(id)) return;

        if (localPlayerObject == null) FindLocalPlayer();

        Vector3 spawnPos = localPlayerObject != null ? localPlayerObject.transform.position : transform.position;
        Quaternion spawnRot = localPlayerObject != null ? localPlayerObject.transform.rotation : transform.rotation;

        GameObject prefabToUse = PlayerObj;

        GameObject go;
        if (prefabToUse != null)
        {
            go = Instantiate(prefabToUse, spawnPos, spawnRot);
        }
        else if (localPlayerObject != null)
        {
            go = Instantiate(localPlayerObject, spawnPos, spawnRot);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.position = spawnPos;
            go.transform.rotation = spawnRot;
        }

        go.name = $"RemotePlayer_{id}";

        SetupRemotePlayer(go);
        spawnedRemotePlayers.Add(id, go);

        if (!targetPositions.ContainsKey(id)) targetPositions[id] = spawnPos;
        if (!targetRotations.ContainsKey(id)) targetRotations[id] = spawnRot;
    }

    private void SetupRemotePlayer(GameObject playerObj)
    {
        // Disable cameras on remote player clones
        foreach (var cam in playerObj.GetComponentsInChildren<Camera>())
        {
            cam.enabled = false;
        }

        // Disable audio listeners on remote player clones
        foreach (var listener in playerObj.GetComponentsInChildren<AudioListener>())
        {
            listener.enabled = false;
        }

        // Keep Rigidbodies kinematic for smooth network sync
        foreach (var rb in playerObj.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
        }
    }

    private void DespawnRemotePlayer(int id)
    {
        if (spawnedRemotePlayers.TryGetValue(id, out GameObject go))
        {
            if (go != null) Destroy(go);
            spawnedRemotePlayers.Remove(id);
            targetPositions.Remove(id);
            targetRotations.Remove(id);
            targetLeftHandPos.Remove(id);
            targetLeftHandRot.Remove(id);
            targetRightHandPos.Remove(id);
            targetRightHandRot.Remove(id);
        }
    }

    public void ClearPlayers()
    {
        foreach (var kvp in spawnedRemotePlayers)
        {
            if (kvp.Value != null) Destroy(kvp.Value);
        }

        spawnedRemotePlayers.Clear();
        targetPositions.Clear();
        targetRotations.Clear();
        targetLeftHandPos.Clear();
        targetLeftHandRot.Clear();
        targetRightHandPos.Clear();
        targetRightHandRot.Clear();
        activeSessionPlayerIds.Clear();
    }
}