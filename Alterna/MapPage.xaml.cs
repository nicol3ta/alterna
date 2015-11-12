using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Controls.Maps;
using Windows.Devices.Geolocation;
using Windows.Media.SpeechRecognition;
using Microsoft.WindowsAzure.MobileServices;
using Windows.UI.Popups;
using Windows.Services.Maps;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Storage.Streams;
using AlternaDataModel;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Xaml.Media;
using System.Linq;

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

        private int numberOfLocations;
        private SpeechRecognizer recognizer;

        public MapPage()
        {
            this.InitializeComponent();

            numberOfLocations = 0;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Specify the location
            Geoposition currentPosition = await getCurrentLocation();
            BasicGeoposition cityPosition = new BasicGeoposition() { Latitude = currentPosition.Coordinate.Point.Position.Latitude, Longitude = currentPosition.Coordinate.Point.Position.Longitude };
            Geopoint cityCenter = new Geopoint(cityPosition);

            // Get saved locations
            await GetLocations(addressTable);
            // Set map location
            MapControl1.Center = cityCenter;
            MapControl1.ZoomLevel = 14;
            MapControl1.LandmarksVisible = true;

            SpeechRecognitionResult vcResult = e.Parameter as SpeechRecognitionResult;
            if (vcResult != null)
            {
                string commandMode = vcResult.SemanticInterpretation.Properties["commandMode"][0];

                if (commandMode == "voice") //Did the user speak or type the command?
                {
                    HandleVoiceCommand();
                }
                else if (commandMode == "text")
                {
                    //Handle text command
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        /// <summary>
        /// Gets saved locations from table and adds them to the map
        /// <param name="addressTable"></param>
        /// </summary>
        private async Task GetLocations(IMobileServiceTable<Address> addressTable)
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

                        numberOfLocations++;
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
            //mapIcon.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Images/gaspump_mapicon_small.png"));
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


        private async Task<Geoposition> getCurrentLocation()
        {
            try
            {
                // Request permission to access location
                var accessStatus = await Geolocator.RequestAccessAsync();

                switch (accessStatus)
                {
                    case GeolocationAccessStatus.Allowed:
                        Geolocator geolocator = new Geolocator();

                        // Carry out the operation
                        Geoposition pos = await geolocator.GetGeopositionAsync();
                        return pos;   
                    case GeolocationAccessStatus.Denied:
                        Debug.WriteLine("Access to location is denied.");
                        break;
                    case GeolocationAccessStatus.Unspecified:
                        Debug.WriteLine("Unspecified error.");
                        break;                   
                }
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("Cancelled");
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            return null;
        }

        private async void HandleVoiceCommand()
        { 
             try 
             { 
                 //Text to speech 
                 await SpeakText("Es befinden sich" + numberOfLocations+ "Alterna Orte in deiner Nähe"); 
             } 
             catch (Exception exception) 
             { 
                 Debug.WriteLine("Exception: " + exception.Message); 
             }
        } 

        //The object for controlling the speech synthesis object (voice) 
         private async Task SpeakText(string textToSpeak)
         { 
             using (var speech = new SpeechSynthesizer()) 
             { 
                 //Retrieve the first German female voice 
                 speech.Voice = SpeechSynthesizer.AllVoices.First(i => (i.Gender == VoiceGender.Female && i.Description.Contains("Germany"))); 
                 //Generate audio streams from plain text 
                 SpeechSynthesisStream ttsStream = await speech.SynthesizeTextToStreamAsync(textToSpeak); 
                 mediaPlayer.SetSource(ttsStream, ttsStream.ContentType); 
                 mediaPlayer.Play();
           
            } 
         }


        private async Task Listen(bool withUI)
         { 
             try 
             { 
                 //Perform speech recognition 
                 SpeechRecognitionResult speechRecognitionResult = await RecognizeSpeech(); 
                 //Check the confidence level of the specch recognition attempt 
                 if (speechRecognitionResult.Confidence == SpeechRecognitionConfidence.Rejected) 
                 { 
                     await SpeakText("Ich habe leider nicht verstanden. Kannst Du bitte wiederholen?"); 
                 } 
 
 
                 else 
                 { 
                     if (speechRecognitionResult.Text == "Welche Orte gibt es in meiner Nähe?" || speechRecognitionResult.Text == "Welche Orte genau?" ||
                         speechRecognitionResult.Text == "Welche sind diese?" || speechRecognitionResult.Text == "Welche Orte?" || 
                         speechRecognitionResult.Text == "Welcher ist dieser?") 
                     {
                        string locations = "";
                        foreach(var item in items)
                        {
                            if(item == items.First())
                            {
                                locations = string.Concat(locations, item.Name);
                            }
                            else
                            {
                                locations = string.Concat(locations," und ", item.Name);
                            }
                            
                      
                        }
                        Debug.WriteLine(locations);
                        await SpeakText(locations);
                    } 
                 } 
             } 
             catch (Exception e) 
             { 
                 Debug.WriteLine("Exception: " + e); 
             } 
         }

        private async Task<SpeechRecognitionResult> RecognizeSpeech()
        {
            try
            {
                if (recognizer == null)
                {
                    recognizer = new SpeechRecognizer();
                    string[] possibleAnswers = { "Welche Orte gibt es in meiner Nähe?", "Welche sind diese?", "Welcher ist dieser?", "Welche Orte?", "Welche Orte genau?" };
                    var listConstraint = new SpeechRecognitionListConstraint(possibleAnswers, "Answer");
                    recognizer.UIOptions.ExampleText = @"Bsp. 'Welche Orte gibt es in meiner Nähe?'";
                    recognizer.Constraints.Add(listConstraint);

                    await recognizer.CompileConstraintsAsync();
                }
                SpeechRecognitionResult result = await recognizer.RecognizeWithUIAsync();
                return result;
            }
            catch (Exception exception)
            {
                const uint HResultPrivacyStatementDeclined = 0x80045509;
                if ((uint)exception.HResult == HResultPrivacyStatementDeclined)
                {
                    var messageDialog = new Windows.UI.Popups.MessageDialog("You must accept the speech privacy policy");
                    messageDialog.ShowAsync().GetResults();
                }
                else
                {
                    Debug.WriteLine("Error: " + exception.Message);
                }
            }
            return null;
        }

        private async void OnStateChanged(object sender, RoutedEventArgs e)
        {

            // When media player finished streaming start listening to the user
            if ((mediaPlayer.CurrentState == MediaElementState.Closed) ||
            (mediaPlayer.CurrentState == MediaElementState.Stopped) ||
            (mediaPlayer.CurrentState == MediaElementState.Paused))
            {
                mediaPlayer.CurrentStateChanged -= OnStateChanged;
                await Listen(false);
            }
        }


        private async void MicroButton_Click(object sender, RoutedEventArgs e)
        {
            await Listen(true);
        }
        
    }
}
