using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using SharpDX.DirectInput;

namespace DeviceChangeFix
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "DeviceChangeFix";

        private int controllerCount;
        private DirectInput directInput;

        private DalamudPluginInterface PluginInterface { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

        public delegate IntPtr DeviceChangeDelegate(IntPtr inputDeviceManager);
        private Hook<DeviceChangeDelegate>? deviceChangeDelegateHook;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] SigScanner sigScanner)
        {
            this.PluginInterface = pluginInterface;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            this.PluginUi = new PluginUI(this.Configuration);

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            // Find the function that is called when a device change happens
            var signature = "48 83 EC 38 0F B6 81";

            IntPtr renderAddress;

            if (sigScanner.TryScanText(signature, out renderAddress))
            {
                this.deviceChangeDelegateHook = new Hook<DeviceChangeDelegate>(renderAddress, this.DeviceChangeDetour);
                this.deviceChangeDelegateHook.Enable();
            }
            else
            {
                PluginLog.Error("Signature could not be found");
            }

            this.directInput = new DirectInput();
            this.controllerCount = this.GetControllerCount();
        }

        private unsafe IntPtr DeviceChangeDetour(IntPtr inputDeviceManager)
        {
            if (this.Configuration.BypassLogic == true)
            {
                PluginLog.Information($"triggered");
                return this.deviceChangeDelegateHook?.Original(inputDeviceManager) ?? IntPtr.Zero;
            }

            var res = IntPtr.Zero;

            // Only call the polling function if the number of controllers
            // actually changed since the last time the hook was called
            int newControllerCount = this.GetControllerCount();
            if (newControllerCount != this.controllerCount)
            {
                res = this.deviceChangeDelegateHook?.Original(inputDeviceManager) ?? IntPtr.Zero;

                if (newControllerCount > this.controllerCount)
                {
                    PluginLog.Information($"{newControllerCount - this.controllerCount} devices added, polling started");
                }
                else
                {
                    PluginLog.Information($"{this.controllerCount - newControllerCount} devices removed, polling started");
                }

                this.controllerCount = newControllerCount;
            }
            else
            {
                PluginLog.Information("No input devices changed, polling skipped");
            }

            return res;
        }

        private int GetControllerCount()
        {
            if (this.directInput != null)
            {
                return this.directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AllDevices).Count;
            }

            return 0;
        }

        public void Dispose()
        {
            this.directInput.Dispose();
            this.deviceChangeDelegateHook?.Disable();
            this.deviceChangeDelegateHook?.Dispose();
            this.PluginUi.Dispose();
        }

        private void DrawUI()
        {
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
    }
}
