using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Style;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SimpleCompare
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private Configuration configuration;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }
        internal Item Item { get; set; }
        private Vector2 LastMousePos { get; set; }

        // passing in the image here just for simplicity
        public PluginUI(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            if (this.Item == null)
                return;

            var equipSlot = this.Item.EquipSlotCategory;
            if (equipSlot == null)
                return;

            var mousePos = ImGui.GetMousePos();
            if (Math.Abs(mousePos.X - this.LastMousePos.X) > 200 || Math.Abs(mousePos.Y - this.LastMousePos.Y) > 200)
            {
                this.Item = null;
                this.LastMousePos = mousePos;
                return;
            }


            var ínventoryType = GetInventoryType(this.Item);
            if (ínventoryType == InventoryType.ArmorySoulCrystal)
            {
                return;
            }


            var equippedItems = GetEquippedItemsByType(ínventoryType);
            if (equippedItems.Count > 0)
            {
                mousePos.Y = mousePos.Y + 50;
                mousePos.X = mousePos.X - 350;
                ImGui.SetNextWindowPos(mousePos, ImGuiCond.Always);
                //ImGui.SetWindowPos(ImGui.GetCursorPos(), ImGuiCond.Always);
                //ImGui.SetNextWindowSize(new Vector2(250, 100), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSizeConstraints(new Vector2(250, 250), new Vector2(float.MaxValue, float.MaxValue));
                if (ImGui.Begin("SimpleCompare", ref this.visible, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    foreach (var item in equippedItems)
                    {
                        ImGui.Text($"{item.Name}:");
                        DrawItemCompare(item, this.Item);
                        ImGui.Separator();
                    }
                }

                ImGui.End();
            }

        }

        private List<Item> GetEquippedItemsByType(InventoryType ínventoryType)
        {
            List<Item> items = new List<Item>();
            unsafe
            {
                var equippedItems = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems);
                var count = equippedItems->Size;
                for (int i = 0; i < count; i++)
                {
                    var intentoryItem = equippedItems->Items[i];
                    var item = Service.Data.GetExcelSheet<Item>().GetRow(intentoryItem.ItemID);

                    if (GetInventoryType(this.Item) == GetInventoryType(item))
                    {
                        items.Add(item);
                    }
                }

            }
            return items;
        }

        private void DrawItemCompare(Item itemA, Item itemB)
        {
            DrawStat("Materia", itemB.MateriaSlotCount - itemA.MateriaSlotCount);

            var deltaDamageMag = itemB.DamageMag - itemA.DamageMag;
            DrawStat("Dmg Mag", deltaDamageMag);
            var deltaDamagePhys = itemB.DamagePhys - itemA.DamagePhys;
            DrawStat("Dmg Phys", deltaDamagePhys);

            var deltaDefenseMag = itemB.DefenseMag - itemA.DefenseMag;
            DrawStat("Def Mag", deltaDefenseMag);
            var deltaDefensePhys = itemB.DefensePhys - itemA.DefensePhys;
            DrawStat("Def Phys", deltaDefensePhys);



            var bonusesA = itemA.UnkData59;
            var bonusesB = itemB.UnkData59;

            // map bonus value to type for comparison
            Dictionary<byte, short> bonusMapA = new Dictionary<byte, short>();
            Dictionary<byte, short> bonusMapB = new Dictionary<byte, short>();
            HashSet<byte> bonusTypes = new HashSet<byte>();

            foreach (var bonus in bonusesA)
            {
                bonusMapA[bonus.BaseParam] = bonus.BaseParamValue;
                bonusTypes.Add(bonus.BaseParam);
            }

            foreach (var bonus in bonusesB)
            {
                bonusMapB[bonus.BaseParam] = bonus.BaseParamValue;
                bonusTypes.Add(bonus.BaseParam);
            }


            foreach (var bonusType in bonusTypes)
            {
                var valueA = bonusMapA.ContainsKey(bonusType) ? bonusMapA[bonusType] : 0;
                var valueB = bonusMapB.ContainsKey(bonusType) ? bonusMapB[bonusType] : 0;

                DrawStat(BaseParamToName(bonusType), valueB - valueA);
            }

            PluginLog.LogDebug($"bonus={itemA}");

        }

        private string BaseParamToName(byte baseParam)
        {
            switch (baseParam)
            {
                case 1:
                    return "Strength";
                case 2:
                    return "Dexterity";
                case 3:
                    return "Vitality";
                case 4:
                    return "Intelligence";
                case 5:
                    return "Mind";
                case 6:
                    return "Piety";
                case 11:
                    return "CP";
                case 19:
                    return "Tenacity";
                case 27:
                    return "Critical Hit";
                case 44:
                    return "Determination";
                case 45:
                    return "Skill Speed";
                case 46:
                    return "Spell Speed";
                case 22:
                    return "Direct Hit Rate";
                case 70:
                    return "Craftmanship";
                case 71:
                    return "Control";
                case 72:
                    return "Gathering";
                case 73:
                    return "Perception";
                case 0:
                    return "Skill Speed";

                default:
                    break;
            }

            return $"<unknown {baseParam}>";
        }

        private void DrawStat(string name, int value)
        {
            if (value != 0)
            {
                ImGui.TextColored(value > 0 ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed, $"{name}: {(value > 0 ? $"+{value}" : $"{value}")}");
            }
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

