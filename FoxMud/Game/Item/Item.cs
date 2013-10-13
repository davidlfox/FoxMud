﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Db;
using Newtonsoft.Json;

namespace FoxMud.Game.Item
{
    /// <summary>
    /// a templated item; these items won't be 'owned' by anything, but merely serve
    /// as a template from which to create objects
    /// </summary>
    class Template : Storable
    {
        public string Key
        {
            get { return Name.ToLower(); }
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Keywords { get; set; }
        public int Weight { get; set; }
        public int Value { get; set; }
        public Wearlocation WearLocation { get; set; }

        // in-game item attributes
        public int HpBonus { get; set; }
        public int ArmorBonus { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public Dictionary<string, string> ContainedItems { get; set; }
    }


    /// <summary>
    /// a unique item, keyed by guid; these objects are used to store specific instances
    /// of an item so items can be renamed, have different attributes, etc 
    /// </summary>
    class PlayerItem : Storable, Equipable
    {
        private Guid _guid;

        public string Key
        {
            get { return _guid.ToString(); }
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Keywords { get; set; }
        public int Weight { get; set; }
        public int Value { get; set; }
        public Wearlocation WearLocation { get; set; }

        // in-game item attributes
        public int HpBonus { get; set; }
        public int ArmorBonus { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public Dictionary<string, string> ContainedItems { get; set; }

        [JsonConstructor]
        private PlayerItem(string key, string name, string description, string[] keywords, int weight, int value, Wearlocation wearLocation,
            int hpBonus, int armorBonus, int mindDamage, int maxDamage)
        {
            _guid = new Guid(key);
            Name = name;
            Description = description;
            Keywords = keywords;
            Weight = weight;
            Value = value;
            WearLocation = wearLocation;
            HpBonus = hpBonus;
            ArmorBonus = armorBonus;
            MinDamage = mindDamage;
            MaxDamage = maxDamage;
        }

        // need this empty constructor for automapper
        public PlayerItem()
        {
            Guid guid = Guid.NewGuid();
            while (itemExists(guid))
            {
                guid = Guid.NewGuid();
            }

            _guid = guid;
        }

        protected bool itemExists(Guid guid)
        {
            return Server.Current.Database.Get<PlayerItem>(guid.ToString()) != null;
        }

        public void LookAt(Session session)
        {
            session.WriteLine(Description);

            if (WearLocation == Wearlocation.Container)
            {
                if (ContainedItems.Count == 0)
                {
                    session.WriteLine("\tEmpty");
                    return;
                }

                foreach (var itemLine in ContainedItems
                    .GroupBy(i => i.Value)
                    .Select(group => new
                {
                    ItemName = group.Key,
                    Count = group.Count()
                }))
                {
                    session.WriteLine("\t{0} ({1})", itemLine.ItemName, itemLine.Count);
                }
            }
        }

        public virtual void Equip(Player player)
        {
            throw new NotImplementedException();
        }

        public virtual void Unequip(Player player)
        {
            throw new NotImplementedException();
        }
    }
}
