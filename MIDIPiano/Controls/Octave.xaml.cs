using Windows.Devices.Midi;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MIDIPiano.Controls
{
    public sealed partial class Octave : UserControl
    {
        // Since Keyboards are simply a repition of 12 notes at increasing Octaves (chromatic scale),
        // We create an Octave UserControl that can be stacked together to form a keyboard of any size.
        // Changing the OctaveInt changes the octave which this particular set of keys plays at

        // Event handlers to pass Key tapped/released up from the keys when called
        public event RoutedEventHandler O_KeyTapped;
        public event RoutedEventHandler O_KeyReleased;

        // DependencyProperty OctaveInt used to set the Octave of this set of keys
        // DependencyProperty allows the value to be set in XAML (see "Keyboard.xaml")
        public int OctaveInt
        {
            get { return (int)GetValue(OctaveIntProperty); }
            set { SetValue(OctaveIntProperty, value); }
        }
        public static readonly DependencyProperty OctaveIntProperty =
            DependencyProperty.Register("OctaveInt", typeof(int), typeof(Octave), new PropertyMetadata(0));

        public Octave()
        {
            this.InitializeComponent();
        }

        // Function to call SetMiddleC on White key
        public void SetMiddleC()
        {
            k_0.SetMiddleC();
        }

        // Function to Press/Depress (change colour on UI) piano key
        public void SetKey(int number, bool isOff = false)
        {
            // Get the UserControl by name
            UserControl key = (UserControl)FindName("k_" + number);
            if(key != null)
            {
                switch (number)
                {
                    // 1, 3, 6, 8, 10 are all black keys
                    case 1:
                    case 3:
                    case 6:
                    case 8:
                    case 10:
                        if (isOff)
                            ((BlackKey)key).DepressKey();
                        else
                            ((BlackKey)key).PressKey();
                        break;
                    // Rest are white, no need to list them all out
                    default:
                        if (isOff)
                            ((WhiteKey)key).DepressKey();
                        else
                            ((WhiteKey)key).PressKey();
                        break;
                }
            }
        }

        // KeyTapped event to handle creation of MidiNoteOnMessages
        private void K_KeyTapped(object sender, RoutedEventArgs e)
        {
            // Use the OctaveInt to set the note to the correct pitch
            // (Number of octaves * 12 notes in an octave + note of current octave)
            byte note = (byte)(OctaveInt * 12 + (int)sender);
            // Set channel to 0 (default), note to calculated value, and velocity to 100
            MidiNoteOnMessage msg = new MidiNoteOnMessage(0, note, 100);
            // Then send MidiNoteOnMessage as object/sender of event
            O_KeyTapped?.Invoke(msg, null);
        }

        private void K_KeyReleased(object sender, RoutedEventArgs e)
        {
            // Same as K_KeyTapped but with MidiNoteOffMessage
            byte note = (byte)(OctaveInt * 12 + (int)sender);
            MidiNoteOffMessage msgO = new MidiNoteOffMessage(0, note, 100);
            O_KeyReleased?.Invoke(msgO, null);
        }
    }
}
