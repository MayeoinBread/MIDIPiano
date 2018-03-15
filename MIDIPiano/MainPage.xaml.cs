using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MIDIPiano
{
    public sealed partial class MainPage : Page
    {
        // Just a generic landing page before we get into the good, juicy stuff!

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(PianoPage));
        }
    }
}
