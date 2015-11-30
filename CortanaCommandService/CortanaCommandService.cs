using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.VoiceCommands;

namespace CortanaCommandService
{
    public sealed class CortanaCommandService : IBackgroundTask
    {
        private BackgroundTaskDeferral serviceDeferral;
        VoiceCommandServiceConnection voiceServiceConnection;
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            serviceDeferral = taskInstance.GetDeferral();
            taskInstance.Canceled += OnTaskCanceled;
            var triggerDetails = taskInstance.TriggerDetails as AppServiceTriggerDetails;

            if (triggerDetails != null && triggerDetails.Name == "MathModelsCortanaCommandService")
            {
                VoiceCommandResponse response;
                try
                {
                    voiceServiceConnection = VoiceCommandServiceConnection.FromAppServiceTriggerDetails(triggerDetails);
                    voiceServiceConnection.VoiceCommandCompleted += VoiceCommandCompleted;
                    VoiceCommand voiceCommand = await voiceServiceConnection.GetVoiceCommandAsync();
                    VoiceCommandUserMessage userMessage = new VoiceCommandUserMessage();

                    switch (voiceCommand.CommandName)
                    {
                        case "graphParams":
                            {
                                var modelnumber = voiceCommand.Properties["modelnumber"][0];
                                var responseMessage = new VoiceCommandUserMessage()
                                {
                                    DisplayMessage = String.Format("Draw queueing model {0} graph?", modelnumber),
                                    SpokenMessage = String.Format("Do you want me to draw the graph for model {0}?", modelnumber)
                                };

                                var repeatMessage = new VoiceCommandUserMessage()
                                {
                                    DisplayMessage = String.Format("Are you sure you want me to draw the graph for model {0}?", modelnumber),
                                    SpokenMessage = String.Format("Are you sure you want me to draw the graph for model {0}?", modelnumber)
                                };

                                bool confirmed = false;
                                response = VoiceCommandResponse.CreateResponseForPrompt(responseMessage, repeatMessage);
                                try
                                {
                                    var confirmation = await voiceServiceConnection.RequestConfirmationAsync(response);
                                    confirmed = confirmation.Confirmed;
                                }
                                catch
                                {

                                }
                                if (confirmed)
                                {
                                    userMessage.DisplayMessage = userMessage.SpokenMessage = "Done and Done! Here are your graph.";
                                    
                                    response = VoiceCommandResponse.CreateResponse(userMessage);
                                    await voiceServiceConnection.ReportSuccessAsync(response);
                                }
                                else
                                {
                                    userMessage.DisplayMessage = userMessage.SpokenMessage = "Okay then";
                                    response = VoiceCommandResponse.CreateResponse(userMessage);
                                    await voiceServiceConnection.ReportSuccessAsync(response);
                                }
                                //SendCompletionMessageForModelNumber(modelnumber);
                                break;
                            }
                        // As a last resort launch the app in the foreground
                        default:
                            LaunchAppInForeground();
                            break;
                    }
                }
                finally
                {
                    if (this.serviceDeferral != null)
                    {
                        //Complete the service deferral
                        this.serviceDeferral.Complete();
                    }
                }
            }
        }
        private async void SendCompletionMessageForModelNumber(string modelnumber)
        {
            // Take action and determine when the next trip to destination
            // Inset code here

            // Replace the hardcoded strings used here with strings 
            // appropriate for your application.

            // First, create the VoiceCommandUserMessage with the strings 
            // that Cortana will show and speak.
            var userMessage = new VoiceCommandUserMessage();
            userMessage.DisplayMessage = "Here’s your graph.";
            userMessage.SpokenMessage = String.Format("Your graph for model {0}", modelnumber);
       

            // Create the VoiceCommandResponse from the userMessage and list    
            // of content tiles.
            var response = VoiceCommandResponse.CreateResponse(userMessage);

            // Cortana will present a “Go to app_name” link that the user 
            // can tap to launch the app. 
            // Pass in a launch to enable the app to deep link to a page 
            // relevant to the voice command.
            response.AppLaunchArgument = string.Format("modelname={0}", modelnumber);

            // Ask Cortana to display the user message and content tile and 
            // also speak the user message.
            await voiceServiceConnection.ReportSuccessAsync(response);
        }
        private async void LaunchAppInForeground()
        {
            var userMessage = new VoiceCommandUserMessage();
            userMessage.SpokenMessage = "Launching Queueing Models Calculator";

            var response = VoiceCommandResponse.CreateResponse(userMessage);
            response.AppLaunchArgument = "";
            await voiceServiceConnection.RequestAppLaunchAsync(response);
        }
        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            System.Diagnostics.Debug.WriteLine("Task cancelled, clean up");
        }
        private void VoiceCommandCompleted(VoiceCommandServiceConnection sender, VoiceCommandCompletedEventArgs args)
        {
            if (this.serviceDeferral != null)
            {
                //Complete the service deferral
                this.serviceDeferral.Complete();
            }
        }
    }
}
