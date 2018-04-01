using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using Windows.UI.Xaml.Data;

namespace MIDIPiano
{
    public class PanThumbConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int temp = int.Parse(value.ToString());

            if (temp == 64)
                return "0";
            else if (temp < 64)
                return "-" + (100 - (100 * temp / 64)) + "%";
            else
                return (100 * (temp - 63) / 64) + "%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public sealed partial class PianoPage : Page
    {
        MidiDeviceWatcher inputDeviceWatcher;
        MidiDeviceWatcher outputDeviceWatcher;

        ControlMessageHelper msgHelper;
        private MidiInPort midiInPort;
        private IMidiOutPort midiOutPort;

        private KeyWidth currentWidth = KeyWidth.Normal;

        // Default value for PitchBend slider, half of its range
        private ushort PitchBendDefault = 8192;

        // Bool to let us know if there's an input MIDI device selected
        private bool hasMidiInputSelected = false;

        // Bool to let us know if ScrollView scrolling is locked/unlocked
        private bool ScrollViewScrollLocked = true;

        public PianoPage()
        {
            this.InitializeComponent();

            // Setup our device watchers for input and output MIDI devices.
            // Let's us know if devices are connected/disconnected while we're running
            // (And hopefully catches these gracefully so that we don't crash!)
            inputDeviceWatcher = new MidiDeviceWatcher(MidiInPort.GetDeviceSelector(), midiInPortComboBox, Dispatcher);
            inputDeviceWatcher.StartWatcher();

            outputDeviceWatcher = new MidiDeviceWatcher(MidiOutPort.GetDeviceSelector(), midiOutPortComboBox, Dispatcher);
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
            ScrollKeyboard();
        }

        // Button which scrolls to Middle-C, for testing purposes ;)
        private void BtnScroll_Click(object sender, RoutedEventArgs e)
        {
            ScrollKeyboard();
        }

        private void ScrollKeyboard()
        {
            switch (KB.ThisKeyWidth)
            {
                case KeyWidth.Narrow:
                    SV_Keyboard.ChangeView(400, null, null, false);
                    break;
                case KeyWidth.Normal:
                    SV_Keyboard.ChangeView(800, null, null, false);
                    break;
                case KeyWidth.Touch:
                    SV_Keyboard.ChangeView(2200, null, null, false);
                    break;
            }
        }

        /// <summary>
        /// KeyTapped event passed from Keyboard object for when user taps key on screen
        /// </summary>
        /// <param name="sender">MidiNoteOnMessage containing MIDI Note of selected key</param>
        /// <param name="e"></param>
        private void KB_K_KeyTapped(object sender, RoutedEventArgs e)
        {
            // If we weren't sent a valid Key, or there's no output device selected, or we're scrolling, do nothing
            if (sender == null || midiOutPort == null || ScrollViewScrollLocked == false)
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
            // Again, no valid Key, no output or scrolling means we do nothing
            if (sender == null || midiOutPort == null || ScrollViewScrollLocked == false)
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
        private async void MidiInPortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var deviceInformationCollection = inputDeviceWatcher.DeviceInformationCollection;

            // If we have selected a device, disable Sliders & Buttons
            if (midiInPortComboBox.SelectedIndex > -1)
                UpdateUserInputs(false);
            else
            {
                UpdateUserInputs(true);
                return;
            }

            if (deviceInformationCollection == null)
                return;

            DeviceInformation devInfo = deviceInformationCollection[midiInPortComboBox.SelectedIndex];

            if (devInfo == null)
                return;

            midiInPort = await MidiInPort.FromIdAsync(devInfo.Id);

            if (midiInPort == null)
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

        // Handler for SelectionChanged on MIDI Output ComboBox
        private async void MidiOutPortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var deviceInformationCollection = outputDeviceWatcher.DeviceInformationCollection;

            if (deviceInformationCollection == null)
                return;

            DeviceInformation devInfo;
            if (midiOutPortComboBox.SelectedIndex < 0)
                devInfo = null;
            else
                devInfo = deviceInformationCollection[midiOutPortComboBox.SelectedIndex];

            if (devInfo == null)
                return;

            midiOutPort = await MidiOutPort.FromIdAsync(devInfo.Id);

            if (midiOutPort == null)
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

            midiInPortComboBox.SelectedIndex = -1;
        }

        private void BtnLockScroll_Click(object sender, RoutedEventArgs e)
        {
            ScrollViewScrollLocked = !ScrollViewScrollLocked;
            BtnLockScroll.Content = ScrollViewScrollLocked ? "Un-Lock" : "Lock";
            SV_Keyboard.HorizontalScrollMode = ScrollViewScrollLocked ? ScrollMode.Disabled : ScrollMode.Enabled;
        }

        private void BtnKeyWidth_Click(object sender, RoutedEventArgs e)
        {
            if (currentWidth == KeyWidth.Narrow)
                currentWidth = KeyWidth.Normal;
            else if (currentWidth == KeyWidth.Normal)
                currentWidth = KeyWidth.Touch;
            else if (currentWidth == KeyWidth.Touch)
                currentWidth = KeyWidth.Narrow;

            KB.UpdateKeyWidth(currentWidth);
            ScrollKeyboard();
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }
    }
}