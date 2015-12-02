using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.VoiceCommands;
using Windows.UI.Xaml.Media.Imaging;

namespace CortanaCommandService
{
    public sealed class CortanaCommandService : XamlRenderingBackgroundTask
    {
        private BackgroundTaskDeferral serviceDeferral;
        VoiceCommandServiceConnection voiceServiceConnection;
        protected override async void OnRun(IBackgroundTaskInstance taskInstance)
        {
            serviceDeferral = taskInstance.GetDeferral();
            taskInstance.Canceled += OnTaskCanceled;
            var triggerDetails = taskInstance.TriggerDetails as AppServiceTriggerDetails;

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

                            bool allowed = false;
                            response = VoiceCommandResponse.CreateResponseForPrompt(responseMessage, repeatMessage);
                            try
                            {
                                var confirmation = await voiceServiceConnection.RequestConfirmationAsync(response);
                                allowed = confirmation.Confirmed;
                            }
                            catch
                            {

                            }
                            if (allowed)
                            {
                                userMessage.DisplayMessage = userMessage.SpokenMessage = "Done and Done! Here is your graph.";

                                response = VoiceCommandResponse.CreateResponse(userMessage);
                                await voiceServiceConnection.ReportSuccessAsync(response);
                            }
                            else
                            {
                                userMessage.DisplayMessage = userMessage.SpokenMessage = "OK then";
                                response = VoiceCommandResponse.CreateResponse(userMessage);
                                await voiceServiceConnection.ReportSuccessAsync(response);
                            }
                            break;
                    // As a last resort launch the app in the foreground
                    default:
                        LaunchAppInForeground();
                        break;
                }
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
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

        private async void LaunchAppInForeground()
        {
            var userMessage = new VoiceCommandUserMessage();
            userMessage.SpokenMessage = "Launching Queueing Models Calculator";

            var response = VoiceCommandResponse.CreateResponse(userMessage);
            response.AppLaunchArgument = "";
            await voiceServiceConnection.RequestAppLaunchAsync(response);
        }
        private void VoiceCommandCompleted(VoiceCommandServiceConnection sender, VoiceCommandCompletedEventArgs args)
        {
            if (this.serviceDeferral != null)
            {
                this.serviceDeferral.Complete();
            }
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (this.serviceDeferral != null)
            {
                this.serviceDeferral.Complete();
            }
        }
    }
}
