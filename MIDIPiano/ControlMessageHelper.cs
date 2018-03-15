using MIDIPiano.Controls;
using System;
using System.Threading.Tasks;
using Windows.Devices.Midi;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace MIDIPiano
{
    class ControlMessageHelper
    {
        Keyboard keyboard;
        Slider pitchBend;
        Slider modulation;
        Slider volume;
        Slider pan;

        CoreDispatcher coreDispatcher;

        // Constructor, set up Slider & object references
        public ControlMessageHelper(Keyboard k, Slider p, Slider m, Slider v, Slider pa, CoreDispatcher dispatcher)
        {
            keyboard = k;
            pitchBend = p;
            modulation = m;
            volume = v;
            pan = pa;
            coreDispatcher = dispatcher;
        }

        public async Task<bool> UseMessage(IMidiMessage receivedMessage)
        {
            // If we understand and utilise the receivedMessage, we return True
            // This then passes the receivedMessage to the MIDI Output to be processed and used by it
            // If we don't want to utilise the receivedMessage (for reasons...), we simply return False
            switch (receivedMessage.Type)
            {
                // Key/Note pressed
                case MidiMessageType.NoteOn:
                    var msgOn = (MidiNoteOnMessage)receivedMessage;
                    // Dispatcher is used to make sure these UI changes occur on the correct thread
                    await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {
                        keyboard.SetPress(msgOn.Note);
                    });
                    return true;
                // Key/Note released
                case MidiMessageType.NoteOff:
                    var msgOff = (MidiNoteOffMessage)receivedMessage;
                    await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {
                        keyboard.SetPress(msgOff.Note, true);
                    });
                    return true;
                // Pitch Bend changed
                case MidiMessageType.PitchBendChange:
                    // Range: 0 -> 16383
                    // Def: 8192
                    var msgPb = (MidiPitchBendChangeMessage)receivedMessage;
                    await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                    {
                        pitchBend.Value = msgPb.Bend;
                    });
                    return true;
                // Control Change, some other button/slider/function on your controller
                // (These values only tested on Alesis Q49 MIDI Keyboard Controller)
                case MidiMessageType.ControlChange:
                    // Controller - effect
                    // 1 - Modulation wheel
                    // 7 - slider (volume, CC Data?)
                    // 10 - slider (pan pot)
                    // 91 - slider (reverb depth)
                    // 93 - slider (chorus depth)
                    var msgCc = (MidiControlChangeMessage)receivedMessage;
                    switch (msgCc.Controller)
                    {
                        case 1:
                            await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                            {
                                modulation.Value = msgCc.ControlValue;
                            });
                            return true;
                        case 7:
                            await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                            {
                                volume.Value = msgCc.ControlValue;
                            });
                            return true;
                        case 10:
                            await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                            {
                                pan.Value = msgCc.ControlValue;
                            });
                            return true;
                        case 91:
                            return true;
                        case 93:
                            return true;
                    }
                    return false;
            }

            return false;
        }
    }
}
