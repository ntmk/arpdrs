// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoloToolkit.Unity;

#if UNITY_WSA
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Persistence;
using UnityEngine.XR.WSA.Sharing;
#else
using UnityEngine.VR.WSA;
using UnityEngine.VR.WSA.Persistence;
using UnityEngine.VR.WSA.Sharing;
#endif
#endif


namespace HoloToolkit.Sharing
{
    /// <summary>
    /// Wrapper around world anchor store to streamline some of the persistence API busy work.
    /// </summary>
    public class SharingWorldAnchorManager : WorldAnchorManager
    {
#if UNITY_WSA
        /// <summary>
        /// Text for displaying information to the user.
        /// </summary>
        [SerializeField]
        private TextMesh NetworkFeedbackText;

        /// <summary>
        /// The menu controller
        /// </summary>
        [SerializeField]
        private MenuController menuController;

        /// <summary>
        /// Called once the anchor has fully uploaded.
        /// </summary>
        public event Action<bool> AnchorUploaded;

        /// <summary>
        /// Called when the anchor has been downloaded.
        /// </summary>
        public event Action<bool, GameObject> AnchorDownloaded;

        /// <summary>
        /// Sometimes we'll see a really small anchor blob get generated.
        /// These tend to not work, so we have a minimum trustworthy size.
        /// </summary>
        private const uint MinTrustworthySerializedAnchorDataSize = 100000;

        /// <summary>
        /// WorldAnchorTransferBatch is the primary object in serializing/deserializing anchors.
        /// <remarks>Only available on device.</remarks>
        /// </summary>
        private WorldAnchorTransferBatch currentAnchorTransferBatch;

        private bool isExportingAnchors;
        private bool shouldExportAnchors;

        /// <summary>
        /// The data blob of the anchor.
        /// </summary>
        private List<byte> rawAnchorUploadData = new List<byte>(0);

        private bool canUpdate;
        private bool isImportingAnchors;
        private bool shouldImportAnchors;

        /// <summary>
        /// The current download anchor data blob.
        /// </summary>
        private byte[] rawAnchorDownloadData;

        #region Unity Methods

        protected override void Start()
        {
            base.Start();

            if (SharingStage.Instance != null)
            {
                ShowDetailedLogs = SharingStage.Instance.ShowDetailedLogs;

                // SharingStage should be valid at this point, but we may not be connected.
                if (SharingStage.Instance.IsConnected)
                {
                    Connected();
                }
                else
                {
                    Disconnected();
                }
            }
            else
            {
                Debug.LogError("SharingWorldAnchorManager requires the SharingStage.");
            }

            if (NetworkFeedbackText != null) {
                NetworkFeedbackText.text = "Initializing Network Connection";
            }
        }

        protected override void Update()
        {
            if (AnchorStore == null ||
                SharingStage.Instance == null ||
                SharingStage.Instance.CurrentRoom == null ||
                !canUpdate)
            {
                return;
            }

            if (LocalAnchorOperations.Count > 0)
            {
                if (!isExportingAnchors && !isImportingAnchors)
                {
                    DoAnchorOperation(LocalAnchorOperations.Dequeue());
                }
            }
            else
            {
                if (shouldImportAnchors && !isImportingAnchors && !isExportingAnchors)
                {
                    if (AnchorDebugText != null)
                    {
                        AnchorDebugText.text += "\nStarting Anchor Download...";
                    }

                    if(NetworkFeedbackText != null) {
                        NetworkStatusMessage("\nDownloading an alternate dimension from another user");
                    }

                    isImportingAnchors = true;
                    shouldImportAnchors = false;
                    WorldAnchorTransferBatch.ImportAsync(rawAnchorDownloadData, ImportComplete);
                }

                if (shouldExportAnchors && !isExportingAnchors && !isImportingAnchors)
                {
                    if (AnchorDebugText != null)
                    {
                        AnchorDebugText.text += "\nStarting Anchor Upload...";
                    }

                    if (NetworkFeedbackText != null) {
                        NetworkStatusMessage("\nUploading alternate dimension");
                    }

                    isExportingAnchors = true;
                    shouldExportAnchors = false;
                    WorldAnchorTransferBatch.ExportAsync(currentAnchorTransferBatch, WriteBuffer, ExportComplete);
                }
            }
        }

        protected override void OnDestroy()
        {
            Disconnected();
            base.OnDestroy();
        }

        private void NetworkStatusMessage(string message) {
            NetworkFeedbackText.text += HelperFunctions.ResolveTextSize(message);
        }

        #endregion // Unity Methods

        #region Event Callbacks

        /// <summary>
        /// Callback function that contains the WorldAnchorStore object.
        /// </summary>
        /// <param name="anchorStore">The WorldAnchorStore to cache.</param>
        protected override void AnchorStoreReady(WorldAnchorStore anchorStore)
        {
            AnchorStore = anchorStore;

            if (!SharingStage.Instance.KeepRoomAlive || !PersistentAnchors)
            {
                if (AnchorDebugText != null)
                {
                    AnchorDebugText.text += "\nClearing Anchor Store...";
                }

                if (NetworkFeedbackText != null) {
                    NetworkStatusMessage("\nReseting alternate dimension");
                }

                anchorStore.Clear();
            }
        }

        /// <summary>
        /// Called when the sharing stage connects to a server.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Events Arguments.</param>
        private void Connected(object sender = null, EventArgs e = null)
        {
            SharingStage.Instance.SharingManagerConnected -= Connected;
            SharingStage.Instance.SharingManagerDisconnected += Disconnected;

            SharingStage.Instance.RoomManagerAdapter.UserJoinedRoomEvent += RoomManagerListener_OnRoomJoined;
            SharingStage.Instance.RoomManagerAdapter.UserLeftRoomEvent += RoomManagerListener_OnLeftRoom;

            if (ShowDetailedLogs)
            {
                Debug.Log("[SharingWorldAnchorManager] Connected to server.");
            }

            if (AnchorDebugText != null)
            {
                AnchorDebugText.text += "\nConnected to Server.";
            }

            if (NetworkFeedbackText != null) {
                NetworkStatusMessage("\nConnected to Server");
            }
        }

        /// <summary>
        /// Called when the sharing stage is disconnected from a server.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event Arguments.</param>
        private void Disconnected(object sender = null, EventArgs e = null)
        {
            if (SharingStage.Instance != null)
            {
                SharingStage.Instance.SharingManagerConnected += Connected;
                SharingStage.Instance.SharingManagerDisconnected -= Disconnected;

                SharingStage.Instance.RoomManagerAdapter.UserJoinedRoomEvent -= RoomManagerListener_OnRoomJoined;
                SharingStage.Instance.RoomManagerAdapter.UserLeftRoomEvent -= RoomManagerListener_OnLeftRoom;
            }

            if (ShowDetailedLogs)
            {
                Debug.Log("[SharingWorldAnchorManager] Disconnected from server.");
            }

            if (AnchorDebugText != null)
            {
                AnchorDebugText.text += "\nDisconnected from Server.";
            }

            if (NetworkFeedbackText != null) {
                NetworkStatusMessage("\nDisconnected from Server.");
            }

            canUpdate = false;

        }

        private void RoomManagerListener_OnRoomJoined(Room room, int userId)
        {
            if (SharingStage.Instance.Manager.GetLocalUser().GetID() == userId &&
                SharingStage.Instance.CurrentRoom.GetID() == room.GetID())
            {
                SharingStage.Instance.RoomManagerAdapter.AnchorUploadedEvent += RoomManagerListener_AnchorUploaded;
                SharingStage.Instance.RoomManagerAdapter.AnchorsChangedEvent += RoomManagerListener_AnchorsChanged;
                SharingStage.Instance.RoomManagerAdapter.AnchorsDownloadedEvent += RoomManagerListener_AnchorDownloaded;

                canUpdate = true;

                if (ShowDetailedLogs)
                {
                    Debug.LogFormat("[SharingWorldAnchorManager] In room {0} with {1} anchors.",
                        SharingStage.Instance.CurrentRoom.GetName().GetString(),
                        SharingStage.Instance.CurrentRoom.GetAnchorCount());
                }

                if (AnchorDebugText != null)
                {
                    AnchorDebugText.text += string.Format("\nIn room {0} with {1} anchors.",
                        SharingStage.Instance.CurrentRoom.GetName().GetString(),
                        SharingStage.Instance.CurrentRoom.GetAnchorCount());
                }
            }
        }

        private void RoomManagerListener_OnLeftRoom(Room room, int userId)
        {
            if (SharingStage.Instance.Manager.GetLocalUser().GetID() == userId)
            {
                SharingStage.Instance.RoomManagerAdapter.AnchorUploadedEvent -= RoomManagerListener_AnchorUploaded;
                SharingStage.Instance.RoomManagerAdapter.AnchorsChangedEvent -= RoomManagerListener_AnchorsChanged;
                SharingStage.Instance.RoomManagerAdapter.AnchorsDownloadedEvent -= RoomManagerListener_AnchorDownloaded;

                canUpdate = false;

                if (ShowDetailedLogs)
                {
                    Debug.LogFormat("\n[SharingWorldAnchorManager] Left room {0}", room.GetName().GetString());
                }
                if (AnchorDebugText != null)
                {
                    AnchorDebugText.text += string.Format("\nLeft room {0}", room.GetName().GetString());
                }
            }
        }

        /// <summary>
        /// Called when anchor upload operations complete.
        /// </summary>
        private void RoomManagerListener_AnchorUploaded(bool successful, XString failureReason)
        {
            if (successful)
            {
                string[] anchorIds = currentAnchorTransferBatch.GetAllIds();

                for (int i = 0; i < anchorIds.Length; i++)
                {
                    if (ShowDetailedLogs)
                    {
                        Debug.LogFormat("[SharingWorldAnchorManager] Successfully uploaded anchor \"{0}\".", anchorIds[i]);
                    }

                    if (AnchorDebugText != null)
                    {
                        AnchorDebugText.text += string.Format("\nSuccessfully uploaded anchor \"{0}\".", anchorIds[i]);
                    }

                    if(NetworkFeedbackText != null) {
                        NetworkStatusMessage("\nSuccessfully uploaded alternate dimension");
                    }
                }

                // Let UI know network is ready
                menuController.CloseMenu();
            }
            else
            {
                Debug.LogError("[SharingWorldAnchorManager] Upload failed: " + failureReason);
                if (AnchorDebugText != null)
                {
                    AnchorDebugText.text += string.Format("\nUpload failed: " + failureReason);
                }

                if (NetworkFeedbackText != null) {
                    //NetworkStatusMessage(string.Format("\nUpload failed: " + failureReason);
                    // Throw fatal error to the user
                    menuController.ShowErrorMessage(string.Format("Network Error: Upload failed:\n" + failureReason), true);
                }
            }

            rawAnchorUploadData.Clear();
            currentAnchorTransferBatch.Dispose();
            currentAnchorTransferBatch = null;
            isExportingAnchors = false;

            if (AnchorUploaded != null)
            {
                AnchorUploaded(successful);
            }
        }

        /// <summary>
        /// Called when anchor download operations complete.
        /// </summary>
        private void RoomManagerListener_AnchorDownloaded(bool successful, AnchorDownloadRequest request, XString failureReason)
        {
            // If we downloaded anchor data successfully we should import the data.
            if (successful)
            {
                int dataSize = request.GetDataSize();

                if (ShowDetailedLogs)
                {
                    Debug.LogFormat("[SharingWorldAnchorManager] Downloaded {0} bytes.", dataSize.ToString());
                }

                if (AnchorDebugText != null)
                {
                    AnchorDebugText.text += string.Format("\nDownloaded {0} bytes.", dataSize.ToString());
                }

                if (NetworkFeedbackText != null) {
                    NetworkStatusMessage("\nDownloaded alternate dimension");
                }

                rawAnchorDownloadData = new byte[dataSize];
                request.GetData(rawAnchorDownloadData, dataSize);
                shouldImportAnchors = true;
            }
            else
            {
                Debug.LogWarning("[SharingWorldAnchorManager] Anchor DL failed " + failureReason);
                if (AnchorDebugText != null)
                {
                    AnchorDebugText.text += string.Format("\nAnchor DL failed " + failureReason);
                }

                if (NetworkFeedbackText != null) {
                    NetworkStatusMessage(string.Format("\nProblem downloading alternate dimension" + failureReason));
                    menuController.ShowErrorMessage("Network Error: Problem downloading alternate dimension", true);
                }
            }
        }

        /// <summary>
        /// Called when anchors are changed in the room.
        /// </summary>
        /// <param name="room">The room where the anchors were changed.</param>
        private void RoomManagerListener_AnchorsChanged(Room room)
        {
            if (SharingStage.Instance.CurrentRoom.GetID() == room.GetID())
            {
                if (AnchorDebugText != null)
                {
                    AnchorDebugText.text += "\nRoom Anchors Updated!  Clearing the local Anchor Store and attempting to download the update...";
                }

                if (NetworkFeedbackText != null) {
                    NetworkStatusMessage("\nChange in alternate dimension detected!");
                }

                // Clear our local anchor store, and download all our shared anchors again.
                // TODO: Only download the anchors that changed. Currently there's no way to know which anchor changed.
                AnchorStore.Clear();

                if (ShowDetailedLogs)
                {
                    Debug.LogFormat("[SharingWorldAnchorManager] Anchors updated for room \"{0}\".\nClearing the local Anchor Store and attempting to download the update...", room.GetName().GetString());
                }

                if (AnchorDebugText != null)
                {
                    AnchorDebugText.text += string.Format("\nAnchors updated for room \"{0}\".\nClearing the local Anchor Store and attempting to download the update...", room.GetName().GetString());
                }

                int roomAnchorCount = SharingStage.Instance.CurrentRoom.GetAnchorCount();

                for (int i = 0; i < roomAnchorCount; i++)
                {
                    GameObject anchoredObject;
                    string roomAnchorId = SharingStage.Instance.CurrentRoom.GetAnchorName(i).GetString();

                    if (AnchorGameObjectReferenceList.TryGetValue(roomAnchorId, out anchoredObject))
                    {
                        if (ShowDetailedLogs)
                        {
                            Debug.LogFormat("[SharingWorldAnchorManager] Found cached GameObject reference for \"{0}\".", roomAnchorId);
                        }

                        if (AnchorDebugText != null)
                        {
                            AnchorDebugText.text += string.Format("\nFound cached GameObject reference for \"{0}\".", roomAnchorId);
                        }

                        AttachAnchor(anchoredObject, roomAnchorId);
                    }
                    else
                    {
                        anchoredObject = GameObject.Find(roomAnchorId);

                        if (anchoredObject != null)
                        {
                            if (ShowDetailedLogs)
                            {
                                Debug.LogFormat("[SharingWorldAnchorManager] Found a GameObject reference form scene for \"{0}\".", roomAnchorId);
                            }

                            if (AnchorDebugText != null)
                            {
                                AnchorDebugText.text += string.Format("\nFound a GameObject reference form scene for \"{0}\".", roomAnchorId);
                            }

                            AttachAnchor(anchoredObject, roomAnchorId);
                        }
                        else
                        {
                            Debug.LogWarning("[SharingWorldAnchorManager] Unable to find a matching GameObject for anchor!");
                            if (AnchorDebugText != null)
                            {
                                AnchorDebugText.text += "\nUnable to find a matching GameObject for anchor!";
                            }
                        }
                    }
                }
            }
        }

        #endregion // Event Callbacks

        /// <summary>
        /// Called before creating anchor.  Used to check if import required.
        /// </summary>
        /// <param name="anchorId">Name of the anchor to import.</param>
        /// <param name="objectToAnchor">GameObject </param>
        /// <returns>Success.</returns>
        protected override bool ImportAnchor(string anchorId, GameObject objectToAnchor)
        {
            if (SharingStage.Instance == null ||
                SharingStage.Instance.Manager == null ||
                SharingStage.Instance.CurrentRoom == null)
            {
                Debug.LogErrorFormat("[SharingWorldAnchorManager] Failed to import anchor \"{0}\"!  The sharing service was not ready.", anchorId);
                if (AnchorDebugText != null)
                {
                    AnchorDebugText.text += string.Format("\nFailed to import anchor \"{0}\"!  The sharing service was not ready.", anchorId);
                }

                menuController.ShowErrorMessage("Network Error: There was a problem with the Sharing Service", true);

                if (NetworkFeedbackText != null) {
                    NetworkStatusMessage("\nThere was a problem with the Sharing Service");
                }

                return false;
            }

            int roomAnchorCount = SharingStage.Instance.CurrentRoom.GetAnchorCount();

            for (int i = 0; i < roomAnchorCount; i++)
            {
                XString roomAnchorId = SharingStage.Instance.CurrentRoom.GetAnchorName(i);

                if (roomAnchorId.GetString().Equals(anchorId))
                {
                    bool downloadStarted = SharingStage.Instance.CurrentRoomManager.DownloadAnchor(SharingStage.Instance.CurrentRoom, anchorId);

                    if (downloadStarted)
                    {
                        if (ShowDetailedLogs)
                        {
                            Debug.Log("[SharingWorldAnchorManager] Found a match! Attempting to download anchor...");
                        }

                        if (AnchorDebugText != null)
                        {
                            AnchorDebugText.text += "\nFound a match! Attempting to download anchor...";
                        }

                        if (NetworkFeedbackText != null) {
                            NetworkStatusMessage("\nFound alternate dimension! \nAttempting to download...");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[SharingWorldAnchorManager] Found a match, but we've failed to start download!");
                        if (AnchorDebugText != null)
                        {
                            AnchorDebugText.text += "\nFound a match, but we've failed to start download!";
                        }

                        if (NetworkFeedbackText != null) {
                            NetworkStatusMessage("\nFound alternate dimension, but unable to download!");
                        }

                        menuController.ShowErrorMessage("Network Error: Found alternate dimension,\n but unable to download!", true);

                    }

                    return downloadStarted;
                }
            }

            if (ShowDetailedLogs)
            {
                Debug.LogFormat("[SharingWorldAnchorManager] No matching anchor found for \"{0}\" in room {1}.", anchorId, SharingStage.Instance.CurrentRoom.GetName().GetString());
            }

            if (AnchorDebugText != null)
            {
                AnchorDebugText.text += string.Format("\nNo matching anchor found for \"{0}\" in room {1}.", anchorId, SharingStage.Instance.CurrentRoom.GetName().GetString());
            }

            if (NetworkFeedbackText != null) {
                NetworkStatusMessage("\nNo alternate dimension found.");
            }

            return false;
        }

        /// <summary>
        /// Exports and uploads the anchor to the sharing service.
        /// </summary>
        /// <param name="anchor">The anchor to export.</param>
        /// <returns>Success</returns>
        protected override void ExportAnchor(WorldAnchor anchor)
        {
            if (SharingStage.Instance == null ||
                SharingStage.Instance.Manager == null ||
                SharingStage.Instance.CurrentRoom == null)
            {
                Debug.LogErrorFormat("[SharingWorldAnchorManager] Failed to export anchor \"{0}\"!  The sharing service was not ready.", anchor.name);
                if (AnchorDebugText != null)
                {
                    AnchorDebugText.text += string.Format("\nFailed to export anchor \"{0}\"!  The sharing service was not ready.", anchor.name);
                }

                if (NetworkFeedbackText != null) {
                    NetworkStatusMessage("\nFailed to export alternate dimension");
                }

                menuController.ShowErrorMessage("Network Error: Failed to export alternate dimension", true);


                return;
            }

            if (!shouldExportAnchors)
            {
                if (ShowDetailedLogs)
                {
                    Debug.LogWarningFormat("[SharingWorldAnchorManager] Attempting to export anchor \"{0}\".", anchor.name);
                }

                if (AnchorDebugText != null)
                {
                    AnchorDebugText.text += string.Format("\nAttempting to export anchor \"{0}\".", anchor.name);
                }

                if (NetworkFeedbackText != null) {
                    NetworkStatusMessage("\nAttempting to export alternate dimension.");
                }

                if (currentAnchorTransferBatch == null)
                {
                    currentAnchorTransferBatch = new WorldAnchorTransferBatch();
                    if (AnchorDebugText != null)
                    {
                        AnchorDebugText.text += "\nCreating a new World Anchor Transfer Batch...";
                    }
                }
                else
                {
                    Debug.LogWarning("[SharingWorldAnchorManager] We didn't properly cleanup our WorldAnchorTransferBatch!");
                    if (AnchorDebugText != null)
                    {
                        AnchorDebugText.text += "\nWe didn't properly cleanup our WorldAnchorTransferBatch!";
                    }
                }

                currentAnchorTransferBatch.AddWorldAnchor(anchor.name, anchor);
                shouldExportAnchors = true;
            }
        }

        /// <summary>
        /// Called by the WorldAnchorTransferBatch when anchor exporting is complete.
        /// </summary>
        /// <param name="status">Serialization Status.</param>
        private void ExportComplete(SerializationCompletionReason status)
        {
            if (status == SerializationCompletionReason.Succeeded &&
                rawAnchorUploadData.Count > MinTrustworthySerializedAnchorDataSize)
            {
                if (ShowDetailedLogs)
                {
                    Debug.LogFormat("[SharingWorldAnchorManager] Exporting {0} anchors with {1} bytes.", currentAnchorTransferBatch.anchorCount.ToString(), rawAnchorUploadData.ToArray().Length.ToString());
                }

                if (AnchorDebugText != null)
                {
                    AnchorDebugText.text += string.Format("\nExporting {0} anchors with {1} bytes.",
                        currentAnchorTransferBatch.anchorCount.ToString(),
                        rawAnchorUploadData.ToArray().Length.ToString());
                }

                string[] anchorNames = currentAnchorTransferBatch.GetAllIds();

                for (var i = 0; i < anchorNames.Length; i++)
                {
                    SharingStage.Instance.Manager.GetRoomManager().UploadAnchor(
                        SharingStage.Instance.CurrentRoom,
                        new XString(anchorNames[i]),
                        rawAnchorUploadData.ToArray(),
                        rawAnchorUploadData.Count);
                }
            }
            else
            {
                Debug.LogWarning("[SharingWorldAnchorManager] Failed to upload anchor!");

                if (AnchorDebugText != null)
                {
                    AnchorDebugText.text += "\nFailed to upload anchor!";
                }

                if (NetworkFeedbackText != null) {
                    NetworkStatusMessage("\nFailed to upload alternate dimension!");
                }

                menuController.ShowErrorMessage("Network Error: Failed to upload alternate dimension!", true);

                if (rawAnchorUploadData.Count < MinTrustworthySerializedAnchorDataSize)
                {
                    Debug.LogWarning("[SharingWorldAnchorManager] Anchor data was not valid.  Try creating the anchor again.");

                    if (AnchorDebugText != null)
                    {
                        AnchorDebugText.text += "\nAnchor data was not valid. \nTry creating the anchor again.";
                    }

                    if (NetworkFeedbackText != null) {
                        NetworkStatusMessage("\nAnchor data was not valid.");
                    }

                }
            }
        }

        /// <summary>
        /// Called when a remote anchor has been deserialized.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="anchorBatch"></param>
        private void ImportComplete(SerializationCompletionReason status, WorldAnchorTransferBatch anchorBatch)
        {
            bool successful = status == SerializationCompletionReason.Succeeded;
            GameObject objectToAnchor = null;

            if (successful)
            {
                if (ShowDetailedLogs)
                {
                    Debug.LogFormat("[SharingWorldAnchorManager] Successfully imported \"{0}\" anchors.", anchorBatch.anchorCount.ToString());
                }

                if (AnchorDebugText != null)
                {
                    AnchorDebugText.text += string.Format("\nSuccessfully imported \"{0}\" anchors.", anchorBatch.anchorCount.ToString());
                }

                if (NetworkFeedbackText != null) {
                    NetworkStatusMessage("\nSuccessfully imported alternate dimension!");
                }

                //We are connected. Clients can also close network menu now
                menuController.CloseMenu();

                string[] anchorNames = anchorBatch.GetAllIds();

                for (var i = 0; i < anchorNames.Length; i++)
                {
                    if (AnchorGameObjectReferenceList.TryGetValue(anchorNames[i], out objectToAnchor))
                    {
                        AnchorStore.Save(anchorNames[i], anchorBatch.LockObject(anchorNames[i], objectToAnchor));
                    }
                    else
                    {
                        //TODO: Figure out how to get the GameObject reference from across the network.  For now it's best to use unique GameObject names.
                        Debug.LogWarning("[SharingWorldAnchorManager] Unable to import anchor!  We don't know which GameObject to anchor!");

                        if (AnchorDebugText != null)
                        {
                            AnchorDebugText.text += "\nUnable to import anchor!  We don\'t know which GameObject to anchor!";
                        }

                        if (NetworkFeedbackText != null) {
                            NetworkStatusMessage("\nProblem downloading alternate dimension.");
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("[SharingWorldAnchorManager] Import failed!");

                if (AnchorDebugText != null)
                {
                    AnchorDebugText.text += "\nImport failed!";
                }

                if (NetworkFeedbackText != null) {
                    NetworkStatusMessage("\nFailed to import alternate dimension!");
                }

                menuController.ShowErrorMessage("Network Error: Failed to import alternate dimension!", true);
            }

            if (AnchorDownloaded != null)
            {
                AnchorDownloaded(successful, objectToAnchor);
            }

            anchorBatch.Dispose();
            rawAnchorDownloadData = null;
            isImportingAnchors = false;
        }

        /// <summary>
        /// Called by the WorldAnchorTransferBatch as anchor data is available.
        /// </summary>
        /// <param name="data"></param>
        private void WriteBuffer(byte[] data)
        {
            rawAnchorUploadData.AddRange(data);
        }
#endif
    }
}
