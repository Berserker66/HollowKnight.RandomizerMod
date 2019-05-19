﻿using System;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using RandomizerMod.Extensions;
using RandomizerMod.FsmStateActions;
using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;
using Random = System.Random;

namespace RandomizerMod
{
    internal static class MiscSceneChanges
    {
        private static Random rnd = new Random();
        private static int rndNum;

        public static void Hook()
        {
            UnHook();

            ModHooks.Instance.ObjectPoolSpawnHook += FixExplosionPogo;
            On.EnemyHitEffectsArmoured.RecieveHitEffect += FalseKnightNoises;
            On.PlayMakerFSM.OnEnable += FixDreamNail;
        }

        public static void UnHook()
        {
            ModHooks.Instance.ObjectPoolSpawnHook -= FixExplosionPogo;
            On.EnemyHitEffectsArmoured.RecieveHitEffect -= FalseKnightNoises;
            On.PlayMakerFSM.OnEnable -= FixDreamNail;
        }

        public static void SceneChanged(Scene newScene)
        {
            RecalculateRandom();

            ApplyGeneralChanges(newScene);

            if (RandomizerMod.Instance.Settings.Randomizer)
            {
                ApplyRandomizerChanges(newScene);
            }

            if (RandomizerMod.Instance.Settings.NoClaw)
            {
                ApplyNoClawChanges(newScene);
            }
        }

        private static void RecalculateRandom()
        {
            rndNum = rnd.Next(25);
        }

        private static void ApplyRandomizerChanges(Scene newScene)
        {
            string sceneName = newScene.name;

            // Remove quake floors in Soul Sanctum to prevent soft locks
            if (PlayerData.instance.quakeLevel <= 0 && PlayerData.instance.killedMageLord && (sceneName == SceneNames.Ruins1_23 || sceneName == SceneNames.Ruins1_30 || sceneName == SceneNames.Ruins1_32))
            {
                PlayerData.instance.SetBool(nameof(PlayerData.brokenMageWindow), true);
                PlayerData.instance.SetBool(nameof(PlayerData.brokenMageWindowGlass), true);

                foreach (GameObject obj in newScene.GetRootGameObjects())
                {
                    if (obj.name.Contains("Quake Floor"))
                    {
                        Object.Destroy(obj);
                    }
                }
            }

            // Make baldurs always able to spit rollers
            if (sceneName == SceneNames.Crossroads_11_alt || sceneName == SceneNames.Crossroads_ShamanTemple || sceneName == SceneNames.Fungus1_28)
            {
                foreach (GameObject obj in Object.FindObjectsOfType<GameObject>())
                {
                    if (obj.name.Contains("Blocker"))
                    {
                        PlayMakerFSM fsm = FSMUtility.LocateFSM(obj, "Blocker Control");
                        if (fsm != null)
                        {
                            fsm.GetState("Can Roller?").RemoveActionsOfType<IntCompare>();
                        }
                    }
                }
            }

            switch (sceneName)
            {
                case SceneNames.Room_Ouiji:
                    if (RandomizerMod.Instance.Settings.Jiji)
                    {
                        PlayMakerFSM jijiFsm = GameObject.Find("Jiji NPC").LocateMyFSM("Conversation Control");
                        FsmState BoxUp = jijiFsm.GetState("Box Up");
                        BoxUp.ClearTransitions();
                        BoxUp.AddFirstAction(jijiFsm.GetState("Convo Choice").GetActionsOfType<GetPlayerDataInt>()[0]);
                        BoxUp.AddTransition("FINISHED", "Offer");
                        FsmState SendText = jijiFsm.GetState("Send Text");
                        SendText.RemoveTransitionsTo("Yes");
                        SendText.AddTransition("YES", "Check Location");
                        FsmState CheckLocation = jijiFsm.GetState("Check Location");
                        CheckLocation.AddFirstAction(BoxUp.GetActionsOfType<SendEventByName>()[0]);
                        CheckLocation.AddFirstAction(jijiFsm.GetState("Convo Choice").GetActionsOfType<GetPlayerDataInt>()[0]);
                        CheckLocation.AddFirstAction(jijiFsm.GetState("Yes").GetActionsOfType<PlayerDataIntAdd>()[0]);
                        CheckLocation.AddFirstAction(jijiFsm.GetState("Yes").GetActionsOfType<SendEventByName>()[0]);
                    }
                    break;
                case SceneNames.Grimm_Main_Tent:
                    PlayerData.instance.metGrimm = true;
                    break;
                case SceneNames.Abyss_06_Core:
                    // Opens door to LBC
                    if (PlayerData.instance.healthBlue > 0 || PlayerData.instance.joniHealthBlue > 0 || GameManager.instance.entryGateName == "left1")
                    {
                        PlayerData.instance.SetBoolInternal("blueVineDoor", true);
                        PlayMakerFSM BlueDoorFSM = GameObject.Find("Blue Door").LocateMyFSM("Control");
                        BlueDoorFSM.GetState("Init").RemoveTransitionsTo("Got Charm");
                    }
                    break;
                case SceneNames.Abyss_12:
                    // Destroy shriek pickup if the player doesn't have wraiths
                    if (PlayerData.instance.screamLevel == 0)
                    {
                        Object.Destroy(GameObject.Find("New Shiny"));
                    }
                    break;
                case SceneNames.Abyss_15:
                    GameObject.Find("Dream Enter Abyss").LocateMyFSM("Control").GetState("Init").RemoveTransitionsTo("Idle");
                    GameObject.Find("Dream Enter Abyss").LocateMyFSM("Control").GetState("Init").AddTransition("FINISHED", "Inactive");
                    if (!PlayerData.instance.hasDreamNail) Object.Destroy(GameObject.Find("New Shiny"));
                    break;
                /* Removed because it's awkward to have to reload the room to spawn the item
                   case SceneNames.GG_Waterways:
                    if (!PlayerData.instance.godseekerUnlocked)
                    {
                        Object.Destroy(GameObject.Find("Randomizer Shiny"));
                    }
                    break;*/
                case SceneNames.Crossroads_ShamanTemple:
                    // Remove gate in shaman hut
                    Object.Destroy(GameObject.Find("Bone Gate"));

                    // Add hard save to shaman shiny
                    FSMUtility.LocateFSM(GameObject.Find("Randomizer Shiny"), "Shiny Control").GetState("Finish").AddAction(new RandomizerSetHardSave());

                    break;
                case SceneNames.Dream_Nailcollection:
                    // Make picking up shiny load new scene
                    FSMUtility.LocateFSM(GameObject.Find("Randomizer Shiny"), "Shiny Control").GetState("Finish").AddAction(new RandomizerChangeScene("RestingGrounds_07", "right1"));
                    break;
                case SceneNames.Fungus2_21:
                    // Make city crest gate openable infinite times and not hard save
                    FSMUtility.LocateFSM(GameObject.Find("City Gate Control"), "Conversation Control").GetState("Activate").RemoveActionsOfType<SetPlayerDataBool>();

                    FsmState gateSlam = FSMUtility.LocateFSM(GameObject.Find("Ruins_gate_main"), "Open").GetState("Slam");
                    gateSlam.RemoveActionsOfType<SetPlayerDataBool>();
                    gateSlam.RemoveActionsOfType<CallMethodProper>();
                    gateSlam.RemoveActionsOfType<SendMessage>();
                    break;
                case SceneNames.Fungus2_26:
                    // Prevent leg eater from doing anything but opening the shop
                    PlayMakerFSM legEater = FSMUtility.LocateFSM(GameObject.Find("Leg Eater"), "Conversation Control");
                    FsmState legEaterChoice = legEater.GetState("Convo Choice");
                    legEaterChoice.RemoveTransitionsTo("Convo 1");
                    legEaterChoice.RemoveTransitionsTo("Convo 2");
                    legEaterChoice.RemoveTransitionsTo("Convo 3");
                    legEaterChoice.RemoveTransitionsTo("Infected Crossroad");
                    legEaterChoice.RemoveTransitionsTo("Bought Charm");
                    legEaterChoice.RemoveTransitionsTo("Gold Convo");
                    legEaterChoice.RemoveTransitionsTo("All Gold");
                    legEaterChoice.RemoveTransitionsTo("Ready To Leave");
                    legEater.GetState("All Gold?").RemoveTransitionsTo("No Shop");

                    // Just in case something other than the "Ready To Leave" state controls this
                    PlayerData.instance.legEaterLeft = false;
                    break;
                case SceneNames.Mines_33:
                    // Make tolls always interactable
                    if (RandomizerMod.Instance.Settings.MiscSkips && !RandomizerMod.Instance.Settings.RandomizeKeys)
                    {
                        GameObject[] tolls = new GameObject[] { GameObject.Find("Toll Gate Machine"), GameObject.Find("Toll Gate Machine (1)") };
                        foreach (GameObject toll in tolls)
                        {
                            Object.Destroy(FSMUtility.LocateFSM(toll, "Disable if No Lantern"));
                        }
                    }
                    break;
                case SceneNames.RestingGrounds_07:
                    // Make Moth NPC not give items since those are now shinies
                    PlayerData.instance.dreamReward4 = true;
                    PlayerData.instance.dreamReward5b = true;

                    PlayMakerFSM moth = FSMUtility.LocateFSM(GameObject.Find("Dream Moth"), "Conversation Control");
                    moth.FsmVariables.GetFsmBool("Got Reward 4").Value = true;
                    moth.FsmVariables.GetFsmBool("Got Reward 5b").Value = true;
                    break;
                case SceneNames.Room_Colosseum_02:
                    // Move the upward loads in colo downward to prevent bench soft lock
                    GameObject coloTransition1 = GameObject.Find("top1");
                    GameObject coloTransition2 = GameObject.Find("top2");

                    coloTransition1.transform.SetPositionY(coloTransition1.transform.position.y - 9f);
                    coloTransition2.transform.SetPositionY(coloTransition2.transform.position.y - 9f);
                    break;
                case SceneNames.Room_Sly_Storeroom:
                    // Make Sly pickup send Sly back upstairs
                    FsmState slyFinish = FSMUtility.LocateFSM(GameObject.Find("Randomizer Shiny"), "Shiny Control").GetState("Finish");
                    slyFinish.AddAction(new RandomizerSetBool("SlyCharm", true));

                    // The game breaks if you leave the storeroom after this, so just send the player out of the shop completely
                    // People will think it's an intentional feature to cut out pointless walking anyway
                    slyFinish.AddAction(new RandomizerChangeScene("Town", "door_sly"));
                    break;
                case SceneNames.Ruins1_05:
                    // Slight adjustment to breakable so wings is enough to progress, just like on old patches
                    GameObject chandelier = GameObject.Find("ruind_dressing_light_02 (10)");
                    chandelier.transform.SetPositionX(chandelier.transform.position.x - 2);
                    chandelier.GetComponent<NonBouncer>().active = false;

                    break;
                case SceneNames.Ruins1_24:
                    // Pickup (Quake Pickup) -> Idle -> GetPlayerDataInt (quakeLevel)
                    // Quake (Quake Item) -> Get -> SetPlayerDataInt (quakeLevel)
                    // Stop spell container from destroying itself
                    PlayMakerFSM quakePickup = FSMUtility.LocateFSM(GameObject.Find("Quake Pickup"), "Pickup");
                    quakePickup.GetState("Idle").RemoveActionsOfType<IntCompare>();

                    foreach (PlayMakerFSM childFSM in quakePickup.gameObject.GetComponentsInChildren<PlayMakerFSM>(true))
                    {
                        if (childFSM.FsmName == "Shiny Control")
                        {
                            // Make spell container spawn shiny instead
                            quakePickup.GetState("Appear").GetActionsOfType<ActivateGameObject>()[1].gameObject.GameObject.Value = childFSM.gameObject;

                            // Make shiny open gates on pickup/destroy
                            SendEvent openGate = new SendEvent
                            {
                                eventTarget = new FsmEventTarget()
                                {
                                    target = FsmEventTarget.EventTarget.BroadcastAll,
                                    excludeSelf = true
                                },
                                sendEvent = FsmEvent.FindEvent("BG OPEN"),
                                delay = 0,
                                everyFrame = false
                            };
                            childFSM.GetState("Destroy").AddFirstAction(openGate);
                            childFSM.GetState("Finish").AddFirstAction(openGate);

                            // Add hard save after picking up item
                            childFSM.GetState("Finish").AddFirstAction(new RandomizerSetHardSave());

                            break;
                        }
                    }

                    // Stop the weird invisible floor from appearing if dive has been obtained
                    // I don't think it really serves any purpose, so destroying it should be fine
                    if (PlayerData.instance.quakeLevel > 0)
                    {
                        Object.Destroy(GameObject.Find("Roof Collider Battle"));
                    }

                    // Change battle gate to be destroyed if Soul Master is dead instead of it the player has quake
                    FsmState checkQuake = FSMUtility.LocateFSM(GameObject.Find("Battle Gate (1)"), "Destroy if Quake").GetState("Check");
                    checkQuake.RemoveActionsOfType<FsmStateAction>();
                    checkQuake.AddAction(new RandomizerBoolTest(nameof(PlayerData.killedMageLord), null, "DESTROY", true));
                    break;
                case SceneNames.Ruins1_32 when !PlayerData.instance.hasWalljump:
                    // Platform after soul master
                    GameObject plat = Object.Instantiate(GameObject.Find("ruind_int_plat_float_02 (3)"));
                    plat.SetActive(true);
                    plat.transform.position = new Vector2(40.5f, 72f);
                    break;
                case SceneNames.Ruins2_04:
                    // Shield husk doesn't walk as far as on old patches, making something pogoable to make up for this
                    GameObject.Find("Direction Pole White Palace").GetComponent<NonBouncer>().active = false;
                    break;
                case SceneNames.Room_Wyrm:
                    //Make King's Brand cutscene function properly
                    //This stops only stops the cutscene, not the avalanche itself
                    Object.Destroy(GameObject.Find("Avalanche End"));
                    RandomizerMod.Instance.Log("End of section");
                    break;
            }
        }

        private static void ApplyGeneralChanges(Scene newScene)
        {
            switch (newScene.name)
            {
                case SceneNames.Crossroads_38:
                    // Faster daddy
                    PlayMakerFSM grubDaddy = FSMUtility.LocateFSM(GameObject.Find("Grub King"), "King Control");
                    grubDaddy.GetState("Final Reward?").RemoveTransitionsTo("Recover");
                    grubDaddy.GetState("Final Reward?").AddTransition("FINISHED", "Recheck");
                    grubDaddy.GetState("Recheck").RemoveTransitionsTo("Gift Anim");
                    grubDaddy.GetState("Recheck").AddTransition("FINISHED", "Activate Reward");

                    // Terrible code to make a terrible fsm work not terribly
                    int geoTotal = 0;
                    grubDaddy.GetState("All Given").AddAction(new RandomizerAddGeo(grubDaddy.gameObject, 0, true));
                    grubDaddy.GetState("Recheck").AddAction(new RandomizerExecuteLambda(() => grubDaddy.GetState("All Given").GetActionsOfType<RandomizerAddGeo>()[0].SetGeo(geoTotal)));

                    foreach (PlayMakerFSM grubFsm in grubDaddy.gameObject.GetComponentsInChildren<PlayMakerFSM>(true))
                    {
                        if (grubFsm.FsmName == "grub_reward_geo")
                        {
                            FsmState grubGeoState = grubFsm.GetState("Remaining?");
                            int geo = grubGeoState.GetActionsOfType<IntCompare>()[0].integer1.Value;

                            grubGeoState.RemoveActionsOfType<FsmStateAction>();
                            grubGeoState.AddAction(new RandomizerExecuteLambda(() => geoTotal += geo));
                            grubGeoState.AddTransition("FINISHED", "End");
                        }
                    }

                    break;
                case SceneNames.Deepnest_East_16:
                    // Great Hopper%
                    GameObject hopper1 = newScene.FindGameObject("Giant Hopper");
                    GameObject hopper2 = newScene.FindGameObject("Giant Hopper (1)");

                    for (int i = 0; i < 10; i++)
                    {
                        GameObject newHopper1 = Object.Instantiate(hopper1, hopper1.transform.parent);
                        GameObject newHopper2 = Object.Instantiate(hopper2, hopper2.transform.parent);

                        // Don't want people abusing the easter egg as a geo farm
                        newHopper1.GetComponent<HealthManager>().SetGeoSmall(0);
                        newHopper1.GetComponent<HealthManager>().SetGeoMedium(0);
                        newHopper1.GetComponent<HealthManager>().SetGeoLarge(0);

                        newHopper2.GetComponent<HealthManager>().SetGeoSmall(0);
                        newHopper2.GetComponent<HealthManager>().SetGeoMedium(0);
                        newHopper2.GetComponent<HealthManager>().SetGeoLarge(0);

                        newHopper1.transform.localPosition = new Vector3(
                            newHopper1.transform.localPosition.x + rnd.Next(5),
                            newHopper1.transform.localPosition.y,
                            newHopper1.transform.localPosition.z);

                        newHopper2.transform.localPosition = new Vector3(
                            newHopper2.transform.localPosition.x + rnd.Next(5) - 4,
                            newHopper2.transform.localPosition.y,
                            newHopper2.transform.localPosition.z);
                    }

                    break;
                case SceneNames.Fungus1_04:
                    // Open gates after Hornet fight
                    foreach (PlayMakerFSM childFSM in GameObject.Find("Cloak Corpse").GetComponentsInChildren<PlayMakerFSM>(true))
                    {
                        if (childFSM.FsmName == "Shiny Control")
                        {
                            SendEvent openGate = new SendEvent
                            {
                                eventTarget = new FsmEventTarget()
                                {
                                    target = FsmEventTarget.EventTarget.BroadcastAll,
                                    excludeSelf = true
                                },
                                sendEvent = FsmEvent.FindEvent("BG OPEN"),
                                delay = 0,
                                everyFrame = false
                            };
                            childFSM.GetState("Destroy").AddFirstAction(openGate);
                            childFSM.GetState("Finish").AddFirstAction(openGate);

                            break;
                        }
                    }

                    // Destroy everything relating to the dreamer cutscene
                    // This stuff is in another scene and doesn't exist immediately, so I can't use Object.Destroy
                    Components.ObjectDestroyer.Destroy("Dreamer Scene 1");
                    Components.ObjectDestroyer.Destroy("Hornet Saver");
                    Components.ObjectDestroyer.Destroy("Cutscene Dreamer");
                    Components.ObjectDestroyer.Destroy("Dream Scene Activate");

                    // Fix the camera lock zone by removing the FSM that destroys it
                    if (!PlayerData.instance.hornet1Defeated)
                    {
                        Object.Destroy(FSMUtility.LocateFSM(GameObject.Find("Camera Locks Boss"), "FSM"));
                    }

                    break;
                case SceneNames.Mines_35:
                    // Make descending dark spikes pogoable like on old patches
                    foreach (NonBouncer nonBounce in Object.FindObjectsOfType<NonBouncer>())
                    {
                        if (nonBounce.gameObject.name.StartsWith("Spike Collider"))
                        {
                            nonBounce.active = false;
                            nonBounce.gameObject.AddComponent<Components.RandomizerTinkEffect>();
                        }
                    }

                    break;
                case SceneNames.RestingGrounds_04:
                    // Make dream nail plaque not take 20 years to activate
                    FsmState dreamerPlaqueInspect = FSMUtility.LocateFSM(GameObject.Find("Dreamer Plaque Inspect"), "Conversation Control").GetState("Hero Anim");
                    dreamerPlaqueInspect.RemoveActionsOfType<ActivateGameObject>();
                    dreamerPlaqueInspect.RemoveTransitionsTo("Fade Up");
                    dreamerPlaqueInspect.AddTransition("FINISHED", "Map Msg?");

                    PlayMakerFSM dreamerScene2 = FSMUtility.LocateFSM(GameObject.Find("Dreamer Scene 2"), "Control");
                    dreamerScene2.GetState("Take Control").RemoveTransitionsTo("Blast");
                    dreamerScene2.GetState("Take Control").AddTransition("FINISHED", "Fade Out");
                    dreamerScene2.GetState("Fade Out").RemoveTransitionsTo("Dial Wait");
                    dreamerScene2.GetState("Fade Out").AddTransition("FINISHED", "Set Compass Point");
                    break;
                case SceneNames.Ruins1_05b when RandomizerMod.Instance.Settings.Lemm:
                    // Lemm sell all
                    PlayMakerFSM lemm = FSMUtility.LocateFSM(GameObject.Find("Relic Dealer"), "npc_control");
                    lemm.GetState("Convo End").AddAction(new RandomizerSellRelics());
                    break;

            }
        }

        private static void ApplyNoClawChanges(Scene newScene)
        {
            switch (newScene.name)
            {
                case SceneNames.Abyss_04: // Hidden Station
                    GameObject.Find("Direction Pole White Palace (1)").GetComponent<NonBouncer>().active = false;
                    break;
                case SceneNames.Deepnest_Spider_Town: // Beast's Den
                    Components.StickyWall.Create(26.75f, 70f, 2.6f, 15f);
                    Components.StickyWall.Create(5.75f, 92f, 2.6f, 3f);
                    Components.StickyWall.Create(15.75f, 105f, 2.6f, 3f);
                    Components.StickyWall.Create(2.7f, 125f, 2.6f, 10f);
                    Components.StickyWall.Create(15.7f, 145f, 2.6f, 5f);
                    Components.StickyWall.Create(79.85f, 75f, 2.6f, 2f);
                    break;
                case SceneNames.Fungus1_09: // Great slash
                    Object.Instantiate(GameObject.Find("fung_plat_float_05")).transform.position = new Vector3(205f, 11f, 0f);
                    Object.Instantiate(GameObject.Find("fung_plat_float_05")).transform.position = new Vector3(175f, 12.25f, 0f);
                    Object.Instantiate(GameObject.Find("fung_plat_float_05")).transform.position = new Vector3(116.5f, 12f, 0f);
                    Object.Instantiate(GameObject.Find("fung_plat_float_05")).transform.position = new Vector3(4.75f, 17f, 0f);
                    break;
                case SceneNames.Fungus3_archive_02: // Monomon
                    Components.StickyWall.Create(49.75f, 55f, 2.6f, 5f);
                    Components.StickyWall.Create(76.75f, 78.5f, 2.6f, 50f);
                    Components.StickyWall.Create(49.75f, 140f, 2.6f, 5f);
                    break;
                case SceneNames.Mines_37: // Crystal peak chest
                    Components.StickyWall.Create(0.7f, 8.5f, 2.6f, 2f);
                    break;
                case SceneNames.Ruins2_03: // Watcher Knights
                    Components.StickyWall.Create(67.9f, 90f, 2.6f, 25f);

                    // Add a platform in addition to the sticky wall to get up to Lurien
                    GameObject plat = Object.Instantiate(GameObject.Find("ruins_mage_building_0011_a_royal_plat"));
                    plat.transform.position = new Vector3(44f, 112f, plat.transform.position.z);
                    break;
                case SceneNames.Town: // Dirtmouth
                    Components.StickyWall.Create(11.66f, 26.3f, 2.6f, 30.8f);
                    break;
                case SceneNames.Tutorial_01: // King's Pass
                    Components.StickyWall.Create(5.7f, 18f, 2.6f, 2f);
                    Components.StickyWall.Create(3.75f, 41.5f, 2.6f, 2f);
                    break;
            }
        }

        private static void FalseKnightNoises(On.EnemyHitEffectsArmoured.orig_RecieveHitEffect orig, EnemyHitEffectsArmoured self, float dir)
        {
            orig(self, dir);

            if (rndNum == 17 && self.gameObject.name == "False Knight New")
            {
                AudioPlayerOneShot hitPlayer = FSMUtility.LocateFSM(self.gameObject, "FalseyControl").GetState("Hit").GetActionsOfType<AudioPlayerOneShot>()[0];
                AudioClip clip = hitPlayer.audioClips[rnd.Next(hitPlayer.audioClips.Length)];

                AudioClip temp = self.enemyDamage.Clip;
                self.enemyDamage.Clip = clip;
                self.enemyDamage.SpawnAndPlayOneShot(self.audioPlayerPrefab, self.transform.position);
                self.enemyDamage.Clip = temp;
            }
        }

        private static GameObject FixExplosionPogo(GameObject go)
        {
            if (go.name.StartsWith("Gas Explosion Recycle M"))
            {
                go.layer = (int)GlobalEnums.PhysLayers.ENEMIES;
                NonBouncer noFun = go.GetComponent<NonBouncer>();
                if (noFun)
                {
                    noFun.active = false;
                }
            }

            return go;
        }

        private static void FixDreamNail(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);

            if (self.gameObject.name == "Knight" && self.FsmName == "Dream Nail")
            {
                self.GetState("Cancelable").GetActionsOfType<ListenForDreamNail>()[0].activeBool = true;
                self.GetState("Cancelable Dash").GetActionsOfType<ListenForDreamNail>()[0].activeBool = true;
                self.GetState("Queuing").GetActionsOfType<ListenForDreamNail>()[0].activeBool = true;
                self.GetState("Queuing").RemoveActionsOfType<BoolTest>();
            }
        }
    }
}
