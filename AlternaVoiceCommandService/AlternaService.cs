using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources.Core;
using Windows.ApplicationModel.VoiceCommands;
using AlternaDataModel;
using Windows.Storage;

namespace AlternaVoiceCommandService
{
    public sealed class AlternaService : IBackgroundTask
    {
        private BackgroundTaskDeferral serviceDeferral;
        private VoiceCommandServiceConnection voiceServiceConnection;

        private MobileServiceCollection<Address, Address> items;
        private IMobileServiceTable<Address> addressTable;

        private MobileServiceClient MobileService = new MobileServiceClient(
            "https://alternamobileservice.azure-mobile.net/",
            "LMsohMAovxzQVNyHwgEimanMgjfect72"
        );

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            
            addressTable = MobileService.GetTable<Address>();
            serviceDeferral = taskInstance.GetDeferral();
            taskInstance.Canceled += OnTaskCanceled;

            var triggerDetails = taskInstance.TriggerDetails as AppServiceTriggerDetails;

            if (triggerDetails != null &&
              triggerDetails.Name == "AlternaService")
            {
                try
                {
                    voiceServiceConnection =
                      VoiceCommandServiceConnection.FromAppServiceTriggerDetails(
                        triggerDetails);

                    voiceServiceConnection.VoiceCommandCompleted +=
                      VoiceCommandCompleted;

                    VoiceCommand voiceCommand = await
                    voiceServiceConnection.GetVoiceCommandAsync();

                    switch (voiceCommand.CommandName)
                    {
                        case "showLocations":
                            {
                                SendCompletionMessageForLocations();
                                break;
                            }

                        // As a last resort launch the app in the foreground
                        default:
                            LaunchAppInForeground();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Handling Voice Command failed " + ex.ToString());
                }
            }
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            Debug.WriteLine("App service closed due to an unexpected failure; Cleaning up");
            if (this.serviceDeferral != null)
            {
                //Complete the service deferral
                this.serviceDeferral.Complete();
            }
        }

        private void VoiceCommandCompleted(VoiceCommandServiceConnection sender, VoiceCommandCompletedEventArgs args)
        {
            if (this.serviceDeferral != null)
            {
                // Insert your code here
                //Complete the service deferral
                this.serviceDeferral.Complete();
            }
        }

        private async void SendCompletionMessageForLocations()
        {

            // First, create the VoiceCommandUserMessage with the strings 
            // that Cortana will show and speak.
            var userMessage = new VoiceCommandUserMessage();
            

            userMessage.DisplayMessage = "Hier sind ein paar Orte in Deiner Nähe.";
            userMessage.SpokenMessage = "Orte in Deiner Nähe.";

            string loadingLocations = "Suche nach Orte";
            
            
            await ShowProgressScreen(loadingLocations);
                   
            
            var locationsContentTiles = new List<VoiceCommandContentTile>();

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
                Debug.WriteLine("Error loading items");

            }
            else
            {
                if (items.Count() == 0)
                {
                    string foundNoTripToDestination = "Keine Orte gefunden";
                    userMessage.DisplayMessage = foundNoTripToDestination;
                    userMessage.SpokenMessage = foundNoTripToDestination;
                    
                }
                else
                {
                    foreach (var item in items)
                    {
                        int i = 1;

                        var locationTile = new VoiceCommandContentTile();

                        // To handle UI scaling, Cortana automatically looks up files with FileName.scale-<n>.ext formats based on the requested filename.
                        // See the VoiceCommandService\Images folder for an example.
                        locationTile.ContentTileType = VoiceCommandContentTileType.TitleWith68x68IconAndText;
                        locationTile.Image = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///AlternaVoiceCommandService/Images/GreyTile.png"));

                        locationTile.Title = item.Name;
                        if (item.Street != null)
                        {
                            locationTile.TextLine1 = item.Street.ToString();
                        }
                        if (item.Number != null)
                        {
                            locationTile.TextLine2 = item.Number.ToString();
                        }

                        locationsContentTiles.Add(locationTile);
                        i++;
                    }
                }
            }


            // Create the VoiceCommandResponse from the userMessage and list    
            // of content tiles.
            var response = VoiceCommandResponse.CreateResponse(userMessage, locationsContentTiles);


            // Ask Cortana to display the user message and content tile and 
            // also speak the user message.
            await voiceServiceConnection.ReportSuccessAsync(response);
        }

        private async void LaunchAppInForeground()
        {
            var userMessage = new VoiceCommandUserMessage();
            userMessage.SpokenMessage = "Alterna wird gestartet";

            var response = VoiceCommandResponse.CreateResponse(userMessage);

            // When launching the app in the foreground, pass an app 
            // specific launch parameter to indicate what page to show.
            response.AppLaunchArgument = "map";
            await voiceServiceConnection.RequestAppLaunchAsync(response);
        }

        /// <summary>
        /// Show a progress screen. These should be posted at least every 5 seconds for a 
        /// long-running operation, such as accessing network resources over a mobile 
        /// carrier network.
        /// </summary>
        /// <param name="message">The message to display, relating to the task being performed.</param>
        /// <returns></returns>
        private async Task ShowProgressScreen(string message)
        {
            var userProgressMessage = new VoiceCommandUserMessage();
            userProgressMessage.DisplayMessage = userProgressMessage.SpokenMessage = message;

            VoiceCommandResponse response = VoiceCommandResponse.CreateResponse(userProgressMessage);
            await voiceServiceConnection.ReportProgressAsync(response);
        }

    }
}
