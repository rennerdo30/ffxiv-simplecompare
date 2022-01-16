using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using XivCommon;
using XivCommon.Functions.Tooltips;

namespace SimpleCompare
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "SimpleCompare";

        private const string commandName = "/simplecompare";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private XivCommonBase CommonBase { get; set; }
        private PluginUI PluginUi { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            pluginInterface.Create<Service>();


            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            this.PluginUi = new PluginUI(this.Configuration);

            this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            this.CommonBase = new XivCommonBase(Hooks.Tooltips);
            this.CommonBase.Functions.Tooltips.OnItemTooltip += this.OnItemTooltip;


            this.PluginInterface.UiBuilder.Draw += DrawUI;

        }

        public void Dispose()
        {
            this.CommandManager.RemoveHandler(commandName);
            this.CommonBase.Functions.Tooltips.OnItemTooltip -= this.OnItemTooltip;
            this.CommonBase.Dispose();
            this.PluginUi.Dispose();
        }

        private void OnCommand(string command, string args)
        {

        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern short GetKeyState(int keyCode);

        private void DrawUI()
        {
            if ((GetKeyState((int)0x10) & 0x8000) != 0)
            {
                this.PluginUi.Draw();
            }
        }


        private void OnItemTooltip(ItemTooltip tooltip, ulong itemId)
        {
#if false
            if (!tooltip.Fields.HasFlag(ItemTooltipFields.Description))
            {
                return;
            }
#endif
            if (itemId > 2_000_000)
            {
                return;
            }

            if (itemId > 1_000_000)
            {
                itemId -= 1_000_000;
            }

            var item = Service.Data.GetExcelSheet<Item>().GetRow((uint)itemId);
            if (item == null)
            {
                return;
            }
            
            this.PluginUi.Item = item;

            // TODO!
        }

    }
}
