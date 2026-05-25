using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aevix_App.Pages;

/// <summary>
/// First-launch landing page when no playlist exists yet. Routes the user
/// either into the add-playlist form or straight into the empty Home page.
/// </summary>
public sealed partial class OnboardingPage : Page
{
    public OnboardingPage() => InitializeComponent();

    private void Add_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(PlaylistFormPage));
    private void Skip_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(HomePage));
}
