using AlternaDataModel;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Alterna
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IMobileServiceTable<Address> addressTable = App.MobileService.GetTable<Address>();

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Popups.MessageDialog messageDialog;
            if (nameInput.Text.Equals(String.Empty))
            {
                messageDialog = new Windows.UI.Popups.MessageDialog("Alle Felder müssen ausgefüllt sein");
            }
            else
            {
                messageDialog = new Windows.UI.Popups.MessageDialog("Dein Ort namens " + nameInput.Text + " wurde gespeichert");
                var address = new Address { Name = nameInput.Text, Street = streetInput.Text, Number = numberInput.Text, Code=codeInput.Text, City=townInput.Text};

                await InsertAddress(address);
                  
            }
            await messageDialog.ShowAsync();
        }

        private async Task InsertAddress(Address address)
        {
            // This code inserts a new address into the database.
            await addressTable.InsertAsync(address);

            //await SyncAsync(); // offline sync
        }



        private void MapButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MapPage));

        }

        private void MicroButton_Click(object sender, RoutedEventArgs e)
        {
          //TODO
        }
    }
}
