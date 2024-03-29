// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.UX;
using HoloToolkit.Unity.InputModule.Utilities.Interactions;
using HoloToolkit.Unity.SpatialMapping;

namespace HoloToolkit.Sharing.Spawning
{
    /// <summary>
    /// Structure linking a prefab and a data model class.
    /// </summary>
    /// 

    [Serializable]
    public struct PrefabToDataModel
    {
        // TODO Should this be a Type? Or at least have a custom editor to have a dropdown list
        public string DataModelClassName;
        public GameObject Prefab;
    }

    /// <summary>
    /// Spawn manager that creates a GameObject based on a prefab when a new
    /// SyncSpawnedObject is created in the data model.
    /// </summary>
    public class PrefabSpawnManager : SpawnManager<SyncSpawnedObject>
    {
        /// <summary>
        /// List of prefabs that can be spawned by this application.
        /// </summary>
        /// <remarks>It is assumed that this list is the same on all connected applications.</remarks>
        //[SerializeField]
        public List<PrefabToDataModel> spawnablePrefabs = null;

        [SerializeField]
        Material scaleHandleMaterial;
        [SerializeField]
        Material rotateHandleMaterial;
        [SerializeField]
        Material interactingMaterial;
        [SerializeField]
        BoundingBox boundingBoxPrefab;
        [SerializeField]
        AppBar appBarPrefab;

        [SerializeField]
        GameObject menuController;

        private Dictionary<string, GameObject> typeToPrefab;

        /// <summary>
        /// Counter used to create objects and make sure that no two objects created
        /// by the local application have the same name.
        /// </summary>
        private int objectCreationCounter;

        protected override void Start()
        {
            base.Start();

            // We call this manually to resolve runtime order issues -- see BuildModelMenu.cs
            //InitializePrefabs();
        }

        public void InitializePrefabs()
        {
            typeToPrefab = new Dictionary<string, GameObject>(spawnablePrefabs.Count);
            for (int i = 0; i < spawnablePrefabs.Count; i++)
            {
                typeToPrefab.Add(spawnablePrefabs[i].DataModelClassName, spawnablePrefabs[i].Prefab);
            }
        }

        protected override void InstantiateFromNetwork(SyncSpawnedObject spawnedObject)
        {
            GameObject prefab = GetPrefab(spawnedObject, null);
            if (!prefab)
            {
                return;
            }

            // Find the parent object
            GameObject parent = null;
            if (!string.IsNullOrEmpty(spawnedObject.ParentPath.Value))
            {
                parent = GameObject.Find(spawnedObject.ParentPath.Value);
                if (parent == null)
                {
                    Debug.LogErrorFormat("Parent object '{0}' could not be found to instantiate object.", spawnedObject.ParentPath);
                    return;
                }
            }

            CreatePrefabInstance(spawnedObject, prefab, parent, spawnedObject.Name.Value);
        }

        protected override void RemoveFromNetwork(SyncSpawnedObject removedObject)
        {
            if (removedObject.GameObject != null)
            {
                print("Deleting from network");
                Destroy(removedObject.GameObject);
                removedObject.GameObject = null;
            }
        }

        protected virtual string CreateInstanceName(string baseName)
        {
            string instanceName = string.Format("{0}{1}_{2}", baseName, objectCreationCounter.ToString(), NetworkManager.AppInstanceUniqueId);
            objectCreationCounter++;
            return instanceName;
        }

        protected virtual string GetPrefabLookupKey(SyncSpawnedObject dataModel, string baseName)
        {
            return dataModel.GetType().Name;
        }

        protected virtual GameObject GetPrefab(SyncSpawnedObject dataModel, string baseName)
        {
            GameObject prefabToSpawn;
            string dataModelTypeName = GetPrefabLookupKey(dataModel, baseName);
            if (dataModelTypeName == null || !typeToPrefab.TryGetValue(dataModelTypeName, out prefabToSpawn))
            {
                Debug.LogErrorFormat("Trying to instantiate an object from unregistered data model {0}.", dataModelTypeName);
                if (dataModelTypeName == null) {
                    Debug.LogError("dataModelTypeName was null");
                }
                if(typeToPrefab == null) {
                    Debug.LogError("typeToPrefab is null");
                }
                if(!typeToPrefab.TryGetValue(dataModelTypeName, out prefabToSpawn)){
                    Debug.LogError("typeToPrefab failed");
                }
                return null;
            } 
            return prefabToSpawn;
        }

        /// <summary>
        /// Spawns content with the given parent. If no parent is specified it will be parented to the spawn manager itself.
        /// </summary>
        /// <param name="dataModel">Data model to use for spawning.</param>
        /// <param name="localPosition">Local position for the new instance.</param>
        /// <param name="localRotation">Local rotation for the new instance.</param>
        /// <param name="localScale">optional local scale for the new instance. If not specified, uses the prefabs scale.</param>
        /// <param name="parent">Parent to assign to the object.</param>
        /// <param name="baseName">Base name to use to name the created game object.</param>
        /// <param name="isOwnedLocally">
        /// Indicates if the spawned object is owned by this device or not.
        /// An object that is locally owned will be removed from the sync system when its owner leaves the session.
        /// </param>
        /// <returns>True if spawning succeeded, false if not.</returns>
        public bool Spawn(SyncSpawnedObject dataModel, Vector3 localPosition, Quaternion localRotation, Vector3? localScale, GameObject parent, string baseName, bool isOwnedLocally)
        {
            if (SyncSource == null)
            {
                //Display Error to user
                GameObject.Find("MenuController").GetComponent<MenuController>().ShowErrorMessage("No Network Found.  \n\nDid you turn it on?");
                Debug.LogError("Can't spawn an object: PrefabSpawnManager is not initialized.");
                return false;
            }

            if (dataModel == null)
            {
                Debug.LogError("Can't spawn an object: dataModel argument is null.");
                return false;
            }

            if (parent == null)
            {
                parent = gameObject;
            }

            // Validate that the prefab is valid
            GameObject prefabToSpawn = GetPrefab(dataModel, baseName);
            if (!prefabToSpawn)
            {
                return false;
            }

            // Get a name for the object to create
            string instanceName = CreateInstanceName(baseName);

            // Add the data model object to the networked array, for networking and history purposes
            dataModel.Initialize(instanceName, parent.transform.GetFullPath("/"));
            dataModel.Transform.Position.Value = localPosition;
            dataModel.Transform.Rotation.Value = localRotation;
            if (localScale.HasValue)
            {
                dataModel.Transform.Scale.Value = localScale.Value;
            }
            else
            {
                dataModel.Transform.Scale.Value = prefabToSpawn.transform.localScale;
            }

            User owner = null;
            if (isOwnedLocally)
            {
                owner = SharingStage.Instance.Manager.GetLocalUser();
            }

            SyncSource.AddObject(dataModel, owner);
            return true;
        }

        /// <summary>
        /// Instantiate data model on the network with the given parent. If no parent is specified it will be parented to the spawn manager itself.
        /// </summary>
        /// <param name="dataModel">Data model to use for spawning.</param>
        /// <param name="localPosition">Local space position for the new instance.</param>
        /// <param name="localRotation">Local space rotation for the new instance.</param>
        /// <param name="parent">Parent to assign to the object.</param>
        /// <param name="baseName">Base name to use to name the created game object.</param>
        /// <param name="isOwnedLocally">
        /// Indicates if the spawned object is owned by this device or not.
        /// An object that is locally owned will be removed from the sync system when its owner leaves the session.
        /// </param>
        /// <returns>True if the function succeeded, false if not.</returns>
        public bool Spawn(SyncSpawnedObject dataModel, Vector3 localPosition, Quaternion localRotation, GameObject parent, string baseName, bool isOwnedLocally)
        {
            return Spawn(dataModel, localPosition, localRotation, null, parent, baseName, isOwnedLocally);
        }

        protected override void SetDataModelSource()
        {
            SyncSource = NetworkManager.Root.InstantiatedPrefabs;
        }

        public override void Delete(SyncSpawnedObject objectToDelete)
        {
            print("Deleting locally");
            SyncSource.RemoveObject(objectToDelete);
        }

        /// <summary>
        /// Create a prefab instance in the scene, in reaction to data being added to the data model.
        /// </summary>
        /// <param name="dataModel">Object to spawn's data model.</param>
        /// <param name="prefabToInstantiate">Prefab to instantiate.</param>
        /// <param name="parentObject">Parent object under which the prefab should be.</param>
        /// <param name="objectName">Name of the object.</param>
        /// <returns></returns>
        protected virtual GameObject CreatePrefabInstance(SyncSpawnedObject dataModel, GameObject prefabToInstantiate, GameObject parentObject, string objectName)
        {
            GameObject instance = Instantiate(prefabToInstantiate, dataModel.Transform.Position.Value, dataModel.Transform.Rotation.Value);
            instance.transform.localScale = dataModel.Transform.Scale.Value;
            instance.transform.SetParent(parentObject.transform, false);
            instance.gameObject.name = objectName;

            // **************************
            // ****** START BMR CODE ****
            // **************************


            // Find the empty prototypes manager
            GameObject prototypes = GameObject.Find("Prototypes") as GameObject;

            // Close Menu
            menuController.GetComponent<MenuController>().CloseMenu();

            // Load the Bounding box with app bar
            instance.AddComponent<BoundingBoxRig>();

            // Fetch the bounding box script attached to game object
            BoundingBoxRig bbr = instance.GetComponent<BoundingBoxRig>();

            // Set the prefab and materials with the reference attached to load script
            bbr.ScaleHandleMaterial = scaleHandleMaterial;
            bbr.RotateHandleMaterial = rotateHandleMaterial;
            bbr.InteractingMaterial = interactingMaterial;
            bbr.BoundingBoxPrefab = boundingBoxPrefab;
            bbr.AppBarPrefab = appBarPrefab;

            // Adds the two hand manip script on load
            instance.AddComponent<TwoHandManipulatable>();

            // Adds box collider which allows the model to be selected and moved
            instance.AddComponent<BoxCollider>();

            // Adds Default Values
            instance.AddComponent<DefaultValues>();

            // Handles deleting related objects when networked
            instance.AddComponent<GarbageCollector>();

            //*****Network Debugging Tool*****//
            //instance.AddComponent<NetworkPositionDebugClient>().manager = GameObject.Find("NetworkDebug").GetComponent<NetworkPositionDebugManager>();

            /** TAP TO PLACE DISABLED DUE TO NETWORKING BUG SEE #32 **/
            //instance.AddComponent<TapToPlace>();
            //instance.GetComponent<TapToPlace>().IsBeingPlaced = false;
            //instance.GetComponent<TapToPlace>().AllowMeshVisualizationControl = false;

            // Setup Heiarchy to make deletion easier later on
            GameObject prototype = new GameObject();
            prototype.name = "Prototype";

            instance.transform.parent = prototype.transform;
            prototype.transform.parent = prototypes.transform;

            // Match the world stage matrix
            prototype.transform.localPosition = new Vector3(0, 0, 3);
            prototype.transform.rotation = prototypes.transform.rotation;

            // **************************
            // ***** END BMR CODE *******
            // **************************


            // Makes the loaded model a child of prototypes manager
            //instance.transform.parent = prototypes.transform;

            dataModel.GameObject = instance;

            // Set the data model on the various ISyncModelAccessor components of the spawned GameObject
            ISyncModelAccessor[] syncModelAccessors = instance.GetComponentsInChildren<ISyncModelAccessor>(true);
            if (syncModelAccessors.Length <= 0)
            {
                // If no ISyncModelAccessor component exists, create a default one that gives access to the SyncObject instance
                ISyncModelAccessor defaultAccessor = instance.EnsureComponent<DefaultSyncModelAccessor>();
                defaultAccessor.SetSyncModel(dataModel);
            }

            for (int i = 0; i < syncModelAccessors.Length; i++)
            {
                syncModelAccessors[i].SetSyncModel(dataModel);
            }

            // Setup the transform synchronization
            TransformSynchronizer transformSynchronizer = instance.EnsureComponent<TransformSynchronizer>();
            transformSynchronizer.TransformDataModel = dataModel.Transform;

            return instance;
        }
    }
}
