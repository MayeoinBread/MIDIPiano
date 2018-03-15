using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using System.Threading.Tasks;

namespace MIDIPiano
{
    public sealed partial class PianoPage : Page
    {
        MidiDeviceWatcher inputDeviceWatcher;
        MidiDeviceWatcher outputDeviceWatcher;

        ControlMessageHelper msgHelper;
        private MidiInPort midiInPort;
        private IMidiOutPort midiOutPort;

        // Default value for PitchBend slider, half of its range
        private ushort PitchBendDefault = 8192;

        // Bool to let us know if there's an input MIDI device selected
        private bool hasMidiInputSelected = false;

        public PianoPage()
        {
            this.InitializeComponent();

            // Setup our device watchers for input and output MIDI devices.
            // Let's us know if devices are connected/disconnected while we're running
            // (And hopefully catches these gracefully so that we don't crash!)
            inputDeviceWatcher = new MidiDeviceWatcher(MidiInPort.GetDeviceSelector(), midiInPortListBox, Dispatcher);
            inputDeviceWatcher.StartWatcher();

            outputDeviceWatcher = new MidiDeviceWatcher(MidiOutPort.GetDeviceSelector(), midiOutPortListBox, Dispatcher);
            outputDeviceWatcher.StartWatcher();

            // Helper class to take care of MIDI Control messages, set it up here with the sliders
            msgHelper = new ControlMessageHelper(KB, SliderPitch, SliderMod, SliderVolume, SliderPan, Dispatcher);

            // Register Suspending to clean up any connections we have
            Application.Current.Suspending += Current_Suspending;

            // Register event handlers for KeyTapped and KeyReleased
            // (These events only occur when user taps/clicks on keys on screen)
            KB.K_KeyTapped += KB_K_KeyTapped;
            KB.K_KeyReleased += KB_K_KeyReleased;

            // Wait until page has finished loading before doing some UI/layout changes
            Loaded += PianoPage_Loaded;
        }

        private void PianoPage_Loaded(object sender, RoutedEventArgs e)
        {
            // When the page is loaded, we can scroll the keyboard to put Middle-C in view
            SV_Keyboard.ChangeView(800, null, null, true);
        }

        // Button which scrolls to Middle-C, for testing purposes ;)
        private void BtnScroll_Click(object sender, RoutedEventArgs e)
        {
            SV_Keyboard.ChangeView(800, null, null, false);
        }

        /// <summary>
        /// KeyTapped event passed from Keyboard object for when user taps key on screen
        /// </summary>
        /// <param name="sender">MidiNoteOnMessage containing MIDI Note of selected key</param>
        /// <param name="e"></param>
        private void KB_K_KeyTapped(object sender, RoutedEventArgs e)
        {
            // If we weren't sent a valid Key, or there's no output device selected, do nothing
            if (sender == null || midiOutPort == null)
                return;

            // Key colour change on press handled by actual key
            midiOutPort.SendMessage((MidiNoteOnMessage)sender);
        }

        /// <summary>
        /// KeyReleased event passed from Keyboard object for when user releases key on screen
        /// </summary>
        /// <param name="sender">MidiNoteOffMessage containing MIDI Note of selected key</param>
        private void KB_K_KeyReleased(object sender, RoutedEventArgs e)
        {
            // Again, no valid Key or no output means we do nothing
            if (sender == null || midiOutPort == null)
                return;

            // Key colour change on release handled by actual key
            midiOutPort.SendMessage((MidiNoteOffMessage)sender);
        }

        // Called when app is suspending
        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            // Clean up out watchers
            inputDeviceWatcher.StopWatcher();
            inputDeviceWatcher = null;

            outputDeviceWatcher.StopWatcher();
            outputDeviceWatcher = null;

            // Remove EventHandlers and try dispose of input & output ports
            try
            {
                midiInPort.MessageReceived -= MidiInPort_MessageReceived;
                midiInPort.Dispose();
                midiInPort = null;
            }
            catch
            {

            }

            try
            {
                midiOutPort.Dispose();
                midiOutPort = null;
            }
            catch
            {

            }
        }

        // Not sure this is ever used
        private async Task EnumerateMidiInputDevices()
        {
            string midiInputQueryString = MidiInPort.GetDeviceSelector();
            DeviceInformationCollection midiInputDevices = await DeviceInformation.FindAllAsync(midiInputQueryString);

            midiInPortListBox.Items.Clear();

            if(midiInputDevices.Count == 0)
            {
                this.midiInPortListBox.Items.Add("No MIDI input devices found!");
                this.midiInPortListBox.IsEnabled = false;
                return;
            }

            foreach (DeviceInformation deviceInfo in midiInputDevices)
            {
                this.midiInPortListBox.Items.Add(deviceInfo.Name);
                this.midiInPortListBox.IsEnabled = true;
            }
        }
        
        // Not sure this is ever used
        private async Task EnumerateMidiOutputDevices()
        {
            string midiOutportQueryString = MidiOutPort.GetDeviceSelector();
            DeviceInformationCollection midiOutputDevices = await DeviceInformation.FindAllAsync(midiOutportQueryString);

            midiOutPortListBox.Items.Clear();

            if(midiOutputDevices.Count == 0)
            {
                this.midiOutPortListBox.Items.Add("No MIDI output devices found!");
                this.midiOutPortListBox.IsEnabled = false;
                return;
            }

            foreach (DeviceInformation deviceInfo in midiOutputDevices)
            {
                this.midiOutPortListBox.Items.Add(deviceInfo.Name);
                this.midiOutPortListBox.IsEnabled = true;
            }
        }

        // Update Sliders & Buttons, disable when MIDI input device is connected
        // (May be unnecessary, but helps confusion as physical controllers don't necessarily update by themselves when values changed programmatically)
        private void UpdateUserInputs(bool areActive = true)
        {
            hasMidiInputSelected = !areActive;

            SliderPitch.IsEnabled = areActive;
            SliderMod.IsEnabled = areActive;
            SliderPan.IsEnabled = areActive;
            SliderVolume.IsEnabled = areActive;
            BtnPanL.IsEnabled = areActive;
            BtnPanC.IsEnabled = areActive;
            BtnPanR.IsEnabled = areActive;
        }

        // Handler for SelectionChanged on MIDI Input ListBox
        private async void MidiInPortListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var deviceInformationCollection = inputDeviceWatcher.DeviceInformationCollection;

            // If we have selected a device, disable Sliders & Buttons
            if (midiInPortListBox.SelectedIndex > -1)
                UpdateUserInputs(false);
            else
            {
                UpdateUserInputs(true);
                return;
            }

            if (deviceInformationCollection == null)
                return;

            DeviceInformation devInfo = deviceInformationCollection[midiInPortListBox.SelectedIndex];

            if (devInfo == null)
                return;

            midiInPort = await MidiInPort.FromIdAsync(devInfo.Id);

            if(midiInPort == null)
            {
                System.Diagnostics.Debug.WriteLine("Unable to create MidiInPort from input device");
                return;
            }

            // Attach a handler to take care of any MIDI messages received from input
            midiInPort.MessageReceived += MidiInPort_MessageReceived;
        }

        private async void MidiInPort_MessageReceived(MidiInPort sender, MidiMessageReceivedEventArgs args)
        {
            IMidiMessage receivedMidiMessage = args.Message;

            // Use helper class to check message, and if it's something we can deal with, send the message to MIDI out if connected
            bool sendMessage = await msgHelper.UseMessage(receivedMidiMessage);

            if (midiOutPort != null && sendMessage)
                midiOutPort.SendMessage(receivedMidiMessage);
        }

        // Handler for SelectionChanged on MIDI Output ListBox
        private async void MidiOutPortListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var deviceInformationCollection = outputDeviceWatcher.DeviceInformationCollection;

            if (deviceInformationCollection == null)
                return;

            DeviceInformation devInfo;
            if(midiOutPortListBox.SelectedIndex < 0)
                devInfo = null;
            else
                devInfo = deviceInformationCollection[midiOutPortListBox.SelectedIndex];

            if (devInfo == null)
                return;

            midiOutPort = await MidiOutPort.FromIdAsync(devInfo.Id);

            if(midiOutPort == null)
            {
                System.Diagnostics.Debug.WriteLine("Unable to create MidiOutPort from output device");
                return;
            }
        }

        // Handler for all Sliders ValueChanged events
        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            // If we're using the App keyboard, and have an output, send the correct messages
            if (!hasMidiInputSelected && midiOutPort != null)
            {
                MidiControlChangeMessage mccmsg = null;
                MidiPitchBendChangeMessage mpcmsg = null;
                if(sender == SliderPitch)
                {
                    mpcmsg = new MidiPitchBendChangeMessage(0, (ushort)e.NewValue);
                }
                else if(sender == SliderMod)
                {
                    mccmsg = new MidiControlChangeMessage(0, 1, (byte)e.NewValue);
                }
                else if(sender == SliderPan)
                {
                    mccmsg = new MidiControlChangeMessage(0, 10, (byte)e.NewValue);
                }
                else if(sender == SliderVolume)
                {
                    mccmsg = new MidiControlChangeMessage(0, 7, (byte)e.NewValue);
                }

                if (mccmsg != null)
                    midiOutPort.SendMessage(mccmsg);
                else if (mpcmsg != null)
                    midiOutPort.SendMessage(mpcmsg);
            }
        }

        // Generally, Pitch Bend wheels are spring-loaded, and return to center when released.
        // So do that here with PointerCaptureLost event
        private void SliderPitch_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            if(!hasMidiInputSelected)
            {
                SliderPitch.Value = PitchBendDefault;
                if(midiOutPort != null)
                {
                    MidiPitchBendChangeMessage mpcmsg = new MidiPitchBendChangeMessage(0, PitchBendDefault);
                    midiOutPort.SendMessage(mpcmsg);
                }
            }
        }

        // Buttons to set Pan to Left, Center, or Right
        private void BtnPan_Click(object sender, RoutedEventArgs e)
        {
            if (!hasMidiInputSelected)
            {
                string content = ((Button)sender).Content as string;
                byte value = 64;
                switch (content)
                {
                    case "L":
                        value = 0;
                        break;
                    case "R":
                        value = 127;
                        break;
                    default:
                        break;
                }

                SliderPan.Value = value;
                if (midiOutPort != null)
                {
                    MidiControlChangeMessage msg = new MidiControlChangeMessage(0, 10, value);
                    midiOutPort.SendMessage(msg);
                }
            }
        }

        private void BtnDisconnectInput_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                midiInPort.MessageReceived -= MidiInPort_MessageReceived;
                midiInPort.Dispose();
                midiInPort = null;
            }catch{}

            midiInPortListBox.SelectedIndex = -1;
        }
    }
}