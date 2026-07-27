using System;
using UnityEngine;
using valvy;

namespace valvy
{
    [DisallowMultipleComponent]
    public class ValvyView : MonoBehaviour
    {
        [Header("Valvy View Settings")]
        [SerializeField] private int viewID = 0;
        [SerializeField] private string ownerID = "";
        [SerializeField] private bool isMine = false;

        [Header("Sync Settings")]
        public bool syncTransform = true;
        public bool syncRotation = true;
        public float positionLerpSpeed = 15f;
        public float rotationLerpSpeed = 15f;

        // Target state for networking interpolation
        private Vector3 targetPosition;
        private Quaternion targetRotation;

        public int ViewID => viewID;
        public string OwnerID => ownerID;
        public bool IsMine => isMine;

        private void Awake()
        {
            targetPosition = transform.position;
            targetRotation = transform.rotation;
        }

        private void Update()
        {
            // If this object belongs to another network client, smooth toward received target
            if (!isMine && syncTransform)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionLerpSpeed);
                if (syncRotation)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationLerpSpeed);
                }
            }
        }

        /// <summary>
        /// Sets up the ownership and Network View ID for this object.
        /// </summary>
        public void Initialize(int id, string owner, bool mine)
        {
            viewID = id;
            ownerID = owner;
            isMine = mine;
        }

        /// <summary>
        /// Update target transform received from network packet.
        /// </summary>
        public void OnNetworkTransformReceived(Vector3 pos, Quaternion rot)
        {
            if (isMine) return; // Ignore incoming transform updates if local player controls it

            targetPosition = pos;
            targetRotation = rot;
        }

        /// <summary>
        /// Transfer ownership of this ValvyView to a different client.
        /// </summary>
        public void TransferOwnership(string newOwnerID, bool isLocalPlayerNewOwner)
        {
            ownerID = newOwnerID;
            isMine = isLocalPlayerNewOwner;
        }
    }
}