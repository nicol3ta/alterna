using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Controls.Maps;
using Windows.Devices.Geolocation;
using Windows.Media.SpeechRecognition;
using Alterna.DataModel;
using Microsoft.WindowsAzure.MobileServices;
using Windows.UI.Popups;
using Windows.Services.Maps;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Alterna
{
    /// <summary>
    /// This page shows the map with the alternative spots.
    /// </summary>
    public sealed partial class MapPage : Page
    {
        private MobileServiceCollection<Address, Address> items;
        private IMobileServiceTable<Address> addressTable = App.MobileService.GetTable<Address>();
        private ICollection<Geopoint> points = new Collection<Geopoint>();

        public MapPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SpeechRecognitionResult vcResult = e.Parameter as SpeechRecognitionResult;
            if(vcResult != null)
            {
                string commandMode = vcResult.SemanticInterpretation.Properties["commandMode"][0];

                if(commandMode == "voice") //Did the user speak or type the command?
                {
                    //Handle voice command
                }
                else if (commandMode == "text")
                {
                    // Handle text command
                }
            }

            // Specify a known location
            BasicGeoposition cityPosition = new BasicGeoposition() { Latitude = 48.1333, Longitude = 11.5615 };
            Geopoint cityCenter = new Geopoint(cityPosition);
            // Get saved locations
            this.GetLocations(addressTable);
            // Set map location
            MapControl1.Center = cityCenter;
            MapControl1.ZoomLevel = 14;
            MapControl1.LandmarksVisible = true;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        /// <summary>
        /// Gets saved locations from table and adds them to the map
        /// <param name="addressTable"></param>
        /// </summary>
        private async void GetLocations(IMobileServiceTable<Address> addressTable)
       {
            MobileServiceInvalidOperationException exception = null;
            try
            {
                // This code is querying the address table.
                items = await addressTable.ToCollectionAsync();
            }
            catch (MobileServiceInvalidOperationException e)
            {
                exception = e;
            }

            if (exception != null)
            {
                await new MessageDialog(exception.Message, "Error loading items").ShowAsync();
            }
            else
            {
                foreach(var item in items)
                {
                    var newLocation = (await Geocode(item.Name + ", " + item.Street));

                    if (!newLocation.Equals(null))
                    {
                        // Save location
                        points.Add(newLocation);
                        // Add a map icon of the location to the map
                        AddMapIcon(newLocation, item.Name);
                    }                
                }
            }

        }

        /// <summary>
        /// Adds a map icon to the map
        /// </summary>
        /// <param name="point">Location of the map icon</param>
        /// <param name="title">Title of the map icon</param>
        private void AddMapIcon(Geopoint point, String title)
        {
            MapIcon mapIcon = new MapIcon();
            mapIcon.Location = point;
            mapIcon.NormalizedAnchorPoint = new Point(0.5, 1.0);
            mapIcon.Title = title;
            MapControl1.MapElements.Add(mapIcon);
        }


        /// <summary>
        /// Converts the specified address to a geographic location
        /// </summary>
        /// <param name="address">The address to geocode</param>
        /// <returns>Location of the address as Geopoint</returns>
        private async Task<Geopoint> Geocode(string address)
        {

            // Nearby location to use as a query hint.
            BasicGeoposition queryHint = new BasicGeoposition();
            queryHint.Latitude = 48.1333;
            queryHint.Longitude = 11.5667;
            Geopoint hintPoint = new Geopoint(queryHint);

            // Geocode the specified address, using the specified reference point
            // as a query hint. Return no more than 3 results.
            MapLocationFinderResult result =
                  await MapLocationFinder.FindLocationsAsync(
                                    address,
                                    hintPoint,
                                    3);

            // If the query returns results, display the coordinates
            // of the first result.
            if (result.Status == MapLocationFinderStatus.Success)
            {

                var output = "result = (" +
                      result.Locations[0].Point.Position.Latitude.ToString() + "," +
                      result.Locations[0].Point.Position.Longitude.ToString() + ")";
                Debug.Write(output);
                BasicGeoposition point = new BasicGeoposition();
                point.Latitude = result.Locations[0].Point.Position.Latitude;
                point.Longitude = result.Locations[0].Point.Position.Longitude;
                return new Geopoint(point);
            }
            return null;
        }


    }
}
