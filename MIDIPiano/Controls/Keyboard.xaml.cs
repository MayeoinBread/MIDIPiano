using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MIDIPiano.Controls
{
    public sealed partial class Keyboard : UserControl
    {
        // UserControl to handle the multiple Octave objects that make up a full Keyboard

        // Event handlers to pass up the KeyTapped/KeyReleased events from the Octaves and Keys themselves
        public event RoutedEventHandler K_KeyTapped;
        public event RoutedEventHandler K_KeyReleased;

        public Keyboard()
        {
            this.InitializeComponent();
            // Set MiddleC key in o_5 Octave
            o_5.SetMiddleC();
        }

        // If a key is pressed/released on the MIDI controller, the chain starts here to pass the note to the relevant Octave and Key so that the colour is changed in the app
        public void SetPress(int number, bool isOff = false)
        {
            // Get the octave by dividing the note by 12
            int oct = number / 12;
            // The pitch, or the position in the octave is found by using the modulus
            int pitch = number % 12;

            switch (oct)
            {
                case 0:
                    o_0.SetKey(pitch, isOff);
                    break;
                case 1:
                    o_1.SetKey(pitch, isOff);
                    break;
                case 2:
                    o_2.SetKey(pitch, isOff);
                    break;
                case 3:
                    o_3.SetKey(pitch, isOff);
                    break;
                case 4:
                    o_4.SetKey(pitch, isOff);
                    break;
                case 5:
                    o_5.SetKey(pitch, isOff);
                    break;
                case 6:
                    o_6.SetKey(pitch, isOff);
                    break;
                case 7:
                    o_7.SetKey(pitch, isOff);
                    break;
                case 8:
                    o_8.SetKey(pitch, isOff);
                    break;
                case 9:
                    o_9.SetKey(pitch, isOff);
                    break;
                case 10:
                    o_10.SetKey(pitch, isOff);
                    break;
            }
        }

        // If the user taps a key in the app, pass the NoteOn message from the Octave to the PianoPage
        private void O_KeyTapped(object sender, RoutedEventArgs e)
        {
            K_KeyTapped?.Invoke(sender, e);
        }

        // If the user releases a key in the app, pass the NoteOff message form the Octave to the PianoPage
        private void O_KeyReleased(object sender, RoutedEventArgs e)
        {
            K_KeyReleased?.Invoke(sender, e);
        }
    }
}
