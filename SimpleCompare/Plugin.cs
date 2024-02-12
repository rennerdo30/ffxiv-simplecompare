using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Runtime.InteropServices;
using Dalamud.Plugin.Services;

namespace SimpleCompare
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "SimpleCompare";

        private const string commandName = "/simplecompare";

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            pluginInterface.Create<Service>();


            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            this.PluginUi = new PluginUI(this.Configuration);

            this.PluginInterface.UiBuilder.Draw += DrawUI;

            
            Service.GameGui.HoveredItemChanged += this.OnItemHover;
        }

        private void OnItemHover(object? sender, ulong itemId)
        {
            if (itemId > 2_000_000)
            {
                this.PluginUi.InvItem = null;
                return;
            }

            bool wasHQ = false;
            if (itemId > 1_000_000)
            {
                wasHQ = true;
                itemId -= 1_000_000;
            }

            var item = Service.Data.GetExcelSheet<Item>().GetRow((uint)itemId);
            if (item == null)
            {
                this.PluginUi.InvItem = null;
                return;
            }

            this.PluginUi.InvItem = new InvItem(item, wasHQ);
        }

        public void Dispose()
        {
            this.CommandManager.RemoveHandler(commandName);
            this.PluginUi.Dispose();
            Service.GameGui.HoveredItemChanged -= this.OnItemHover;
        }


        private void DrawUI()
        {
            if (Service.ClientState != null && Service.ClientState.IsLoggedIn)
            {
                // dont crash game!
                try
                {
                    this.PluginUi.Draw();
                }
                catch (System.Exception ex)
                {
                    PluginLog.LogFatal(ex.ToString());
                }
            }
        }

    }
}
