﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Modding;
using RandomizerMod.Actions;
using UnityEngine;

namespace RandomizerMod
{
    public class SaveSettings : IModSettings, ISerializationCallbackReceiver
    {
        public List<string> hintItems = new List<string>();

        public List<RandomizerAction> actions = new List<RandomizerAction>();
        public Dictionary<string, string> itemPlacements = new Dictionary<string, string>();

        private static Type[] types;

        public int howManyHints
        {
            get => GetInt(0);
            set => SetInt(value);
        }

        public bool AllBosses
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool AllSkills
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool AllCharms
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool CharmNotch
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool Lemm
        {
            get => GetBool(false);
            set => SetBool(value);
        }
        public bool Jiji
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool PleasureHouse
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool Randomizer
        {
            get => GetBool(false);
            set => SetBool(value);
        }
        public bool SlyCharm
        {
            get => GetBool(false);
            set => SetBool(value);
        }
        public bool RandomizeDreamers
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeSkills
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeCharms
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeKeys
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeGeoChests
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeMaskShards
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeVesselFragments
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeCharmNotches
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizePaleOre
        {
            get => GetBool(true);
            set => SetBool(value);
        }
        public bool RandomizeRancidEggs
        {
            get => GetBool(false);
            set => SetBool(value);
        }
        public bool RandomizeRelics
        {
            get => GetBool(false);
            set => SetBool(value);
        }
        public int LongItemTier
        {
            get => GetInt(1);
            set => SetInt(value);
        }
        public bool ShadeSkips
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool AcidSkips
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool SpikeTunnels
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool MiscSkips
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool FireballSkips
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public bool MagSkips
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        public int Seed
        {
            get => GetInt(-1);
            set => SetInt(value);
        }

        public bool NoClaw
        {
            get => GetBool(false);
            set => SetBool(value);
        }

        // Serialize actions list into string dict because Unity serializer can't handle inheritance
        public void OnBeforeSerialize()
        {
            for (int i = 0; i < actions.Count; i++)
            {
                string json = JsonUtility.ToJson(actions[i]);
                StringValues.Add($"RandomizerAction:{i}:{actions[i].GetType()}", json);
            }

            foreach (string key in itemPlacements.Keys)
            {
                StringValues.Add($"itemPlacements:{key}", itemPlacements[key]);
            }

            /*foreach (string item in hintItems)
            {
                StringValues.Add($"hintItems:{item}", ".");
            }*/

        }

        // Load the actions back into their list
        public void OnAfterDeserialize()
        {
            if (types == null)
            {
                types = Assembly.GetAssembly(typeof(RandomizerAction)).GetTypes().Where(t => t.IsSubclassOf(typeof(RandomizerAction))).ToArray();
            }

            Dictionary<int, RandomizerAction> dict = new Dictionary<int, RandomizerAction>();

            // Load the actions into a dict with numbers as keys
            foreach (string key in StringValues.Keys.ToList())
            {
                if (key.StartsWith("RandomizerAction"))
                {
                    string type = key.Split(':')[2];
                    int num = Convert.ToInt32(key.Split(':')[1]);
                    foreach (Type t in types)
                    {
                        if (type == t.ToString())
                        {
                            dict.Add(num, (RandomizerAction)JsonUtility.FromJson(StringValues[key], t));
                            break;
                        }
                    }

                    StringValues.Remove(key);
                }
                else if (key.StartsWith("itemPlacements"))
                {
                    itemPlacements.Add(key.Split(':')[1], StringValues[key]);
                    StringValues.Remove(key);
                }

                else if (key.StartsWith("hintItems"))
                {
                    hintItems.Add(key.Split(':')[1]);
                    StringValues.Remove(key);
                }
            }

            // Put them back into the list in order
            // This should be unnecessary in theory but I was having issues with order
            for (int i = 0; i < dict.Count; i++)
            {
                actions.Add(dict[i]);
            }
        }
    }
}
