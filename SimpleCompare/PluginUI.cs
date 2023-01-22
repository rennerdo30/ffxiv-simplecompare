using Dalamud.Interface.Colors;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using static FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace SimpleCompare
{
    class InvItem
    {
        public bool IsHQ;
        public Item Item;

        public InvItem(Item item, bool isHQ)
        {
            Item = item;
            IsHQ = isHQ;
        }
    }


    class PluginUI : IDisposable
    {
        private Configuration configuration;

        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        internal InvItem InvItem { get; set; }

        public PluginUI(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public void Dispose()
        {
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern short GetKeyState(int keyCode);

        public void Draw()
        {
            if ((GetKeyState((int)0x10) & 0x8000) == 0)
            {
                return;
            }

            var hoveredItem = this.InvItem;
            if (hoveredItem == null || hoveredItem.Item == null)
            {
                return;
            }


            var equipSlot = hoveredItem.Item.EquipSlotCategory;
            if (equipSlot == null)
            {
                return;
            }

            var inventoryType = GetInventoryType(hoveredItem.Item);
            if (inventoryType == InventoryType.ArmorySoulCrystal || inventoryType == InventoryType.Inventory1)
            {
                return;
            }

            var equippedItems = GetEquippedItemsByType(inventoryType);
            if (equippedItems.Count > 0)
            {
                if (ImGui.Begin("SimpleCompare", ref this.visible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus))
                {
                    for (int i = 0; i < equippedItems.Count; i++)
                    {
                        var item = equippedItems[i];
                        ImGui.Text($"Equipped: {item.Item.Name} (iLvl {item.Item.LevelItem.Row}):");
                        DrawItemCompareEquipped(item, hoveredItem);
                        if (i + 1 < equippedItems.Count)
                        {
                            ImGui.Separator();
                        }
                    }
                }

                var size = ImGui.GetWindowSize();
                var mousePos = ImGui.GetMousePos();
                mousePos.X -= size.X + 25;
                ImGui.SetWindowPos(mousePos, ImGuiCond.Always);

                if (ImGui.Begin("SimpleCompare2", ref this.visible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus))
                {
                    for (int i = 0; i < equippedItems.Count; i++)
                    {
                        var item = equippedItems[i];
                        ImGui.Text($"{hoveredItem.Item.Name} (iLvl {hoveredItem.Item.LevelItem.Row}):");
                        DrawItemCompareHovered(item, hoveredItem);

                        if (i + 1 < equippedItems.Count)
                        {
                            ImGui.Separator();
                        }
                    }
                }

                mousePos.X += size.X + 50;
                ImGui.SetWindowPos(mousePos, ImGuiCond.Always);

                ImGui.End();
            }

        }

        private List<InvItem> GetEquippedItemsByType(InventoryType inventoryType)
        {
            List<InvItem> items = new List<InvItem>();
            unsafe
            {
                var equippedItems = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems);
                var count = equippedItems->Size;
                for (int i = 0; i < count; i++)
                {
                    var intentoryItem = equippedItems->Items[i];
                    var item = Service.Data.GetExcelSheet<Item>().GetRow(intentoryItem.ItemID);

                    if (inventoryType == GetInventoryType(item))
                    {
                        items.Add(new InvItem(item, (intentoryItem.Flags & ItemFlags.HQ) == ItemFlags.HQ));
                    }
                }

            }
            return items;
        }

        private void DrawItemCompareEquipped(InvItem itemA, InvItem itemB)
        {
            DrawStat("Materia", itemA.Item.MateriaSlotCount - itemB.Item.MateriaSlotCount);

            // map bonus value to type for comparison
            Dictionary<byte, short> bonusMapA = GetItemStats(itemA);
            Dictionary<byte, short> bonusMapB = GetItemStats(itemB);

            bonusMapA = BonusMapA(bonusMapA, itemA);
            bonusMapB = BonusMapB(bonusMapB, itemB);

            HashSet<byte> bonusTypes = new HashSet<byte>();
            bonusTypes = BonusTypes(bonusTypes, bonusMapA, bonusMapB);

            foreach (var bonusType in bonusTypes)
            {
                var valueA = bonusMapA.ContainsKey(bonusType) ? bonusMapA[bonusType] : 0;
                var valueB = bonusMapB.ContainsKey(bonusType) ? bonusMapB[bonusType] : 0;

                DrawStat(BaseParamToName(bonusType), valueA - valueB);
            }
        }

        private void DrawItemCompareHovered(InvItem itemA, InvItem itemB)
        {
            DrawStat("Materia", itemB.Item.MateriaSlotCount - itemA.Item.MateriaSlotCount);


            // map bonus value to type for comparison
            Dictionary<byte, short> bonusMapA = GetItemStats(itemA);
            Dictionary<byte, short> bonusMapB = GetItemStats(itemB);

            bonusMapA = BonusMapA(bonusMapA, itemA);
            bonusMapB = BonusMapB(bonusMapB, itemB);



            HashSet<byte> bonusTypes = new HashSet<byte>();
            bonusTypes = BonusTypes(bonusTypes, bonusMapA, bonusMapB);

            foreach (var bonusType in bonusTypes)
            {
                var valueA = bonusMapA.ContainsKey(bonusType) ? bonusMapA[bonusType] : 0;
                var valueB = bonusMapB.ContainsKey(bonusType) ? bonusMapB[bonusType] : 0;

                DrawStat(BaseParamToName(bonusType), valueB - valueA);
            }
        }

        private Dictionary<byte, short> BonusMapA(Dictionary<byte, short> bonusMapA, InvItem itemA)
        {
            if (!bonusMapA.TryAdd(((byte)ItemBonusType.DEFENSE), (short)itemA.Item.DefensePhys))
                bonusMapA[((byte)ItemBonusType.DEFENSE)] += (short)itemA.Item.DefensePhys;

            if (!bonusMapA.TryAdd(((byte)ItemBonusType.MAGIC_DEFENSE), (short)itemA.Item.DefenseMag))
                bonusMapA[((byte)ItemBonusType.MAGIC_DEFENSE)] += (short)itemA.Item.DefenseMag;

            if (!bonusMapA.TryAdd(((byte)ItemBonusType.PHYSICAL_DAMAGE), (short)itemA.Item.DamagePhys))
                bonusMapA[((byte)ItemBonusType.PHYSICAL_DAMAGE)] += (short)itemA.Item.DamagePhys;

            if (!bonusMapA.TryAdd(((byte)ItemBonusType.MAGIC_DAMAGE), (short)itemA.Item.DamageMag))
                bonusMapA[((byte)ItemBonusType.MAGIC_DAMAGE)] += (short)itemA.Item.DamageMag;

            if (!bonusMapA.TryAdd(((byte)ItemBonusType.BLOCK_STRENGTH), (short)itemA.Item.Block))
                bonusMapA[((byte)ItemBonusType.BLOCK_STRENGTH)] += (short)itemA.Item.Block;

            if (!bonusMapA.TryAdd(((byte)ItemBonusType.BLOCK_RATE), (short)itemA.Item.BlockRate))
                bonusMapA[((byte)ItemBonusType.BLOCK_RATE)] += (short)itemA.Item.BlockRate;

            return bonusMapA;
        }

        private Dictionary<byte, short> BonusMapB(Dictionary<byte, short> bonusMapB, InvItem itemB)
        {
            if (!bonusMapB.TryAdd(((byte)ItemBonusType.DEFENSE), (short)itemB.Item.DefensePhys))
                bonusMapB[((byte)ItemBonusType.DEFENSE)] += (short)itemB.Item.DefensePhys;

            if (!bonusMapB.TryAdd(((byte)ItemBonusType.MAGIC_DEFENSE), (short)itemB.Item.DefenseMag))
                bonusMapB[((byte)ItemBonusType.MAGIC_DEFENSE)] += (short)itemB.Item.DefenseMag;


            if (!bonusMapB.TryAdd(((byte)ItemBonusType.PHYSICAL_DAMAGE), (short)itemB.Item.DamagePhys))
                bonusMapB[((byte)ItemBonusType.PHYSICAL_DAMAGE)] += (short)itemB.Item.DamagePhys;

            if (!bonusMapB.TryAdd(((byte)ItemBonusType.MAGIC_DAMAGE), (short)itemB.Item.DamageMag))
                bonusMapB[((byte)ItemBonusType.MAGIC_DAMAGE)] += (short)itemB.Item.DamageMag;

            if (!bonusMapB.TryAdd(((byte)ItemBonusType.BLOCK_STRENGTH), (short)itemB.Item.Block))
                bonusMapB[((byte)ItemBonusType.BLOCK_STRENGTH)] += (short)itemB.Item.Block;

            if (!bonusMapB.TryAdd(((byte)ItemBonusType.BLOCK_RATE), (short)itemB.Item.BlockRate))
                bonusMapB[((byte)ItemBonusType.BLOCK_RATE)] += (short)itemB.Item.BlockRate;

            return bonusMapB;
        }

        private HashSet<byte> BonusTypes(HashSet<byte> bonusTypes, Dictionary<byte, short> bonusMapA, Dictionary<byte, short> bonusMapB)
        {
            bonusTypes.UnionWith(bonusMapA.Keys);
            bonusTypes.UnionWith(bonusMapB.Keys);

            return bonusTypes;
        }

        private Dictionary<byte, short> GetItemStats(InvItem invItem)
        {
            HashSet<byte> bonusTypes = new HashSet<byte>();
            Dictionary<byte, short> bonusMap = new Dictionary<byte, short>();

            foreach (var bonus in invItem.Item.UnkData59)
            {
                bonusMap[bonus.BaseParam] = bonus.BaseParamValue;
                bonusTypes.Add(bonus.BaseParam);
            }

            if (!invItem.IsHQ)
            {
                // We can return here, because no conversion is needed for nq items
                return bonusMap;
            }


            Dictionary<byte, short> result = new Dictionary<byte, short>();
            foreach (var bonus in invItem.Item.UnkData73)
            {
                if (bonusMap.ContainsKey(bonus.BaseParamSpecial))
                {
                    var baseVal = bonusMap[bonus.BaseParamSpecial];
                    baseVal += bonus.BaseParamValueSpecial;
                    result[bonus.BaseParamSpecial] = baseVal;
                }
                else
                {
                    result[bonus.BaseParamSpecial] = bonus.BaseParamValueSpecial;
                }
            }

            return result;
        }

        private void DrawStat(string name, int value)
        {
            if (value != 0)
            {
                ImGui.TextColored(value > 0 ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed, $"{name}: {(value > 0 ? $"+{value}" : $"{value}")}");
            }
        }

        private string BaseParamToName(byte baseParam)
        {
            if (Enum.IsDefined(typeof(ItemBonusType), baseParam))
            {
                ItemBonusType type = (ItemBonusType)baseParam;
                return type.ToDescriptionString();

            }

            return $"<unknown {baseParam}>";
        }

        private InventoryType GetInventoryType(Item item)
        {
            if (item.EquipSlotCategory.Value.MainHand == 1)
            {
                return InventoryType.ArmoryMainHand;
            }
            else if (item.EquipSlotCategory.Value.OffHand == 1)
            {
                return InventoryType.ArmoryOffHand;
            }
            else if (item.EquipSlotCategory.Value.Head == 1)
            {
                return InventoryType.ArmoryHead;
            }
            else if (item.EquipSlotCategory.Value.Body == 1)
            {
                return InventoryType.ArmoryBody;
            }
            else if (item.EquipSlotCategory.Value.Gloves == 1)
            {
                return InventoryType.ArmoryHands;
            }
            else if (item.EquipSlotCategory.Value.Waist == 1)
            {
                return InventoryType.ArmoryWaist;
            }
            else if (item.EquipSlotCategory.Value.Legs == 1)
            {
                return InventoryType.ArmoryLegs;
            }
            else if (item.EquipSlotCategory.Value.Feet == 1)
            {
                return InventoryType.ArmoryFeets;
            }
            else if (item.EquipSlotCategory.Value.Ears == 1)
            {
                return InventoryType.ArmoryEar;
            }
            else if (item.EquipSlotCategory.Value.Neck == 1)
            {
                return InventoryType.ArmoryNeck;
            }
            else if (item.EquipSlotCategory.Value.Wrists == 1)
            {
                return InventoryType.ArmoryWrist;
            }
            else if (item.EquipSlotCategory.Value.FingerL == 1)
            {
                return InventoryType.ArmoryRings;
            }
            else if (item.EquipSlotCategory.Value.FingerR == 1)
            {
                return InventoryType.ArmoryRings;
            }
            else if (item.EquipSlotCategory.Value.SoulCrystal == 1)
            {
                return InventoryType.ArmorySoulCrystal;
            }

            return InventoryType.Inventory1;
        }
    }
}

