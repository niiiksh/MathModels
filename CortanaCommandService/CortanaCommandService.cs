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

            try
            {
                voiceServiceConnection = VoiceCommandServiceConnection.FromAppServiceTriggerDetails(triggerDetails);
                voiceServiceConnection.VoiceCommandCompleted += VoiceCommandCompleted;
                VoiceCommand voiceCommand = await voiceServiceConnection.GetVoiceCommandAsync();

                switch (voiceCommand.CommandName)
                {
                    case "graphParams":
                        var modelnumber = voiceCommand.Properties["modelnumber"][0];
                        SendConfirmationSuccess(modelnumber);
                        
                        break;
                    // As a last resort launch the app in the foreground
                    default:
                        LaunchAppInForeground();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
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

        private async void SendConfirmationSuccess(string modelnumber)
        {
            VoiceCommandUserMessage userMessage;
            VoiceCommandResponse response;

            var responseMessage = new VoiceCommandUserMessage()
            {
                DisplayMessage = String.Format("Get likelihood results for the model {0}?", modelnumber),
                SpokenMessage = String.Format("Do you want me to get likelihood results for the model {0}?", modelnumber)
            };

            var repeatMessage = new VoiceCommandUserMessage()
            {
                DisplayMessage = String.Format("Do you still want me to get likelihood results for the model {0}?", modelnumber),
                SpokenMessage = String.Format("Do you still want me to get likelihood results for the model {0}?", modelnumber)
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
                userMessage = new VoiceCommandUserMessage()
                {
                    DisplayMessage = String.Format("Here is your likelihood results for the model {0}.", modelnumber),
                    SpokenMessage = "Done and Done! Here is your results."
                };
                response = VoiceCommandResponse.CreateResponse(userMessage);

                var resultContentTiles = GetLikelihoodForSelectedModel(modelnumber);

                response.AppLaunchArgument = modelnumber.ToString();
                await voiceServiceConnection.ReportSuccessAsync(response);
            }
            else
            {
                userMessage = new VoiceCommandUserMessage()
                {
                    DisplayMessage = "OK then",
                    SpokenMessage = "OK, then"
                };
                response = VoiceCommandResponse.CreateResponse(userMessage);
                await voiceServiceConnection.ReportSuccessAsync(response);
            }
        }

        private List<VoiceCommandContentTile> GetLikelihoodForSelectedModel(string modelnumber)
        {
            var resultContentTiles = new List<VoiceCommandContentTile>();
            int model = Models.Point.GetNumberByModel(Models.Point.GetModelByNumber(modelnumber));

            //for ()
            //{
            //    var modelTile = new VoiceCommandContentTile();
            //
            //    modelTile.ContentTileType = VoiceCommandContentTileType.TitleOnly;
            //
            //    modelTile.Title = ;
            //    resultContentTiles.Add(modelTile);
            //}
            return resultContentTiles;
        }

        private async void LaunchAppInForeground()
        {
            var userMessage = new VoiceCommandUserMessage();
            userMessage.SpokenMessage = "Launching Queueing Theory Calculator";

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
