﻿using System;
using Newtonsoft.Json;

namespace RandomizerLib.MultiWorld
{
    [Serializable]
    public class MWItem
    {
        public int PlayerId { get; }
        public string Item { get; }

        public MWItem()
        {
            PlayerId = -1;
            Item = "";
        }

        public MWItem(int playerId, string item)
        {
            PlayerId = playerId;
            Item = item;
        }

        public MWItem(string idItem)
        {
            (PlayerId, Item) = LogicManager.ExtractPlayerID(idItem);
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != GetType()) return false;
            MWItem other = (MWItem) obj;
            return PlayerId == other.PlayerId && Item == other.Item;
        }

        public override int GetHashCode()
        {
            return (PlayerId, Item).GetHashCode();
        }

        public override string ToString()
        {
            return "MW(" + (PlayerId + 1) + ")_" + Item;
        }

        public static explicit operator MWItem(string s)
        {
            return new MWItem(s);
        }
    }
}
