using System;
using System.Linq;
using Windows.Devices.Enumeration;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace MIDIPiano
{
    class MidiDeviceWatcher
    {
        // MIDI Device Watcher class, as found on Microsoft Docs:
        // https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/midi
        // Check link for full details on what's going on
        // There may be some deviations here from the link, due to various requirements

        DeviceWatcher deviceWatcher;
        string deviceSelectorString;
        ComboBox deviceComboBox;
        CoreDispatcher coreDispatcher;

        public DeviceInformationCollection DeviceInformationCollection { get; set; }

        public MidiDeviceWatcher(string midiDeviceSelectorString, ComboBox midiDeviceComboBox, CoreDispatcher dispatcher)
        {
            deviceComboBox = midiDeviceComboBox;
            coreDispatcher = dispatcher;

            deviceSelectorString = midiDeviceSelectorString;

            deviceWatcher = DeviceInformation.CreateWatcher(deviceSelectorString);
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
        }

        private async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                // Update device list
                UpdateDevices();
            });
        }

        private async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                // Update device list
                UpdateDevices();
            });
        }

        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                // Update device list
                UpdateDevices();
            });
        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                // Update device list
                UpdateDevices();
            });
        }

        private async void UpdateDevices()
        {
            this.DeviceInformationCollection = await DeviceInformation.FindAllAsync(deviceSelectorString);

            deviceComboBox.Items.Clear();

            if (!this.DeviceInformationCollection.Any())
            {
                deviceComboBox.Items.Add("No MIDI devices found!");
                deviceComboBox.IsEnabled = false;
            }
            else
                deviceComboBox.IsEnabled = true;

            foreach (var deviceInformation in this.DeviceInformationCollection)
            {
                deviceComboBox.Items.Add(deviceInformation.Name);
            }

            if (deviceComboBox.IsEnabled && deviceComboBox.Items.Count > 0)
                deviceComboBox.SelectedIndex = 0;
        }

        public void StartWatcher()
        {
            deviceWatcher.Start();
        }

        public void StopWatcher()
        {
            deviceWatcher.Stop();
        }

        ~MidiDeviceWatcher()
        {
            deviceWatcher.Added -= DeviceWatcher_Added;
            deviceWatcher.Removed -= DeviceWatcher_Removed;
            deviceWatcher.Updated -= DeviceWatcher_Updated;

            deviceWatcher = null;
        }
    }
}
