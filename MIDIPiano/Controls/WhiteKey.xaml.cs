using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace MIDIPiano.Controls
{
    public sealed partial class WhiteKey : UserControl
    {
        // UserControl for White piano key

        // Event handlers for tap/release of this key
        public event RoutedEventHandler KeyTapped;
        public event RoutedEventHandler KeyReleased;

        // DependencyProperty to set the KeyPitch (0-11 ie. 1 Octave)
        // DependencyProperty allows this to be set in XAML (see "Octave.xaml")
        public int KeyPitch
        {
            get { return (int)GetValue(KeyPitchProperty); }
            set { SetValue(KeyPitchProperty, value); }
        }
        public static readonly DependencyProperty KeyPitchProperty =
            DependencyProperty.Register("KeyPitch", typeof(int), typeof(WhiteKey), new PropertyMetadata(0));

        private bool isMiddleC = false;

        public WhiteKey()
        {
            this.InitializeComponent();
        }

        // Function to alter default colour of key if it is Middle C
        // Purely aesthetics
        public void SetMiddleC()
        {
            isMiddleC = true;
            DepressKey();
        }

        // If key is pressed on MIDI controller, this function is called so that the app reflects the key being pressed
        // Purely aesthetics
        public void PressKey()
        {
            WKey.Background = new SolidColorBrush(Colors.LightSkyBlue);
        }

        // If key is released on MIDI controller, this function is called to reset the key colour
        public void DepressKey()
        {
            WKey.Background = isMiddleC ? new SolidColorBrush(Colors.Gray) : new SolidColorBrush(Colors.White);
        }

        // If key is tapped/clicked through the app, call the KeyTapped event so that the NoteOn is sent to the MIDI output
        // The key colour is also set here instead of through the main ControlMessageHelper class
        private void WKey_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            KeyTapped?.Invoke(KeyPitch, null);
            e.Handled = true;
            PressKey();
        }

        // If key is released through the app, call the KeyReleased event so that NoteOff is sent to the MIDI output
        // The key colour is also reset here instead of through the main ControlMessageHelper class
        // PointerCanceled, PointerCaptureLost and PointerExited also use this function to release the key when it should be released
        private void WKey_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            KeyReleased?.Invoke(KeyPitch, null);
            e.Handled = true;
            DepressKey();
        }

        // This event is needed for cases when user taps on a note and slides to another note
        private void WKey_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // We get the current point and check if mouse/pointer is actually clicked
            // This prevents the keys from being registered as pressed when the mouse is simply hovering over the keys
            // The rest of the function is the same as BKey_PointerPressed
            var ptPt = e.GetCurrentPoint(WKey);
            if (ptPt.Properties.IsLeftButtonPressed)
            {
                KeyTapped?.Invoke(KeyPitch, null);
                e.Handled = true;
                PressKey();
            }
        }
    }
}
