using Aevix.Core.Models;
using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aevix_App.Pages;

public sealed partial class PlaylistFormPage : Page
{
    public PlaylistFormViewModel Vm { get; }

    public PlaylistFormPage()
    {
        Vm = App.Services.GetRequiredService<PlaylistFormViewModel>();
        InitializeComponent();
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Vm.StatusText)) StatusText.Text = Vm.StatusText;
        };
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        Vm.Name = NameBox.Text;
        Vm.Url = UrlBox.Text;
        Vm.Username = UserBox.Text;
        Vm.Password = PassBox.Password;
        Vm.MacAddress = MacBox.Text;
        Vm.Type = (TypeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
        {
            "M3UFile" => PlaylistType.M3UFile,
            "Xtream"  => PlaylistType.Xtream,
            "Stalker" => PlaylistType.Stalker,
            _         => PlaylistType.M3UUrl,
        };
        var saved = await Vm.SaveAsync();
        if (saved is not null && Frame.CanGoBack) Frame.GoBack();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }
}
