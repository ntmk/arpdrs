using HoloToolkit.Sharing;
using HoloToolkit.Sharing.Spawning;
using HoloToolkit.Unity.Buttons;
using HoloToolkit.Unity.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/*
 * A class for constructing the Main Menu buttons for each "Prefab_" prototype in 
 * the Resources Folder. ***NOTE*** The "Prefab_" Prefix is required in order for 
 * it to be displayed in our menu. Empty buttons will fill left over gaps for
 * Aethetic purposes of our application.
 */
public class BuildModelMenu : MonoBehaviour {
    [SerializeField]
    public GameObject NetworkController;
    [SerializeField]
    public GameObject mainMenuCollection;
    [SerializeField]
    public GameObject mainMenuButton;
    [SerializeField]
    public GameObject emptyMainMenuButton;
    [SerializeField]
    public GameObject receiver;
    [SerializeField]
    public GameObject menuController;

    //Max models is set due too PrefabSpawnManager requiring unique SyncDataModel classes (Found in SyncSpawnTestSphere.cs)
    private int maxModels = 10;

	// Builds the menu with options to select desired prototype
    void Start() {

        GameObject[] models = Resources.LoadAll<GameObject>("");
        PrefabSpawnManager psm = NetworkController.GetComponent<PrefabSpawnManager>();
        psm.spawnablePrefabs = new List<PrefabToDataModel>();

        //Counter for models
        int modelCounter = 0;
        string prefabNumber;

        foreach (GameObject model in models) {

            if (model.name.StartsWith("Prefab")) {

                modelCounter++;

                if (modelCounter > maxModels) {
                    //Exceeded maximum number of supported models
                    menuController.GetComponent<MenuController>().ShowErrorMessage("Warning: Found more than the supported amount of models in resources. Only Displaying " + maxModels, false);
                    break;
                }

                GameObject Button = Instantiate(mainMenuButton) as GameObject;
                Button.GetComponent<OnNewModelSelected>().buttonIdentfier = modelCounter;
                Button.transform.parent = mainMenuCollection.transform;                
                System.String name = model.name.Split('_').Last();

                // Set text
                Button.GetComponent<CompoundButtonText>().Text = name;

                // All prefabs need a sync accessor
                if (model.GetComponent<DefaultSyncModelAccessor>() == null) {
                    model.AddComponent<DefaultSyncModelAccessor>();
                }

                PrefabToDataModel myModel = new PrefabToDataModel();
                myModel.Prefab = model;

                //Naming format will be 'SyncPrefab##' for our sync scripts. 01 - 10 only
                prefabNumber = "0" + modelCounter.ToString();

                //Change Number Format if above 9 models
                if (modelCounter > 9) {
                    prefabNumber = modelCounter.ToString();
                }

                myModel.DataModelClassName = "SyncPrefab" + prefabNumber; 
                psm.spawnablePrefabs.Add(myModel);
            }
        }

        // Fill in gaps in collection for aesthetic reasons
        while(modelCounter < maxModels) {
            GameObject Button = Instantiate(emptyMainMenuButton) as GameObject;
            Button.transform.parent = mainMenuCollection.transform;
            modelCounter++;
        }

        // Tell collection to update visually
        mainMenuCollection.GetComponent<ObjectCollection>().UpdateCollection();

        // init interaction receiver
        receiver.GetComponent<MenuInteractionReceiver>().Initialize();

         // Register with Sync
        psm.InitializePrefabs();
    }
}
