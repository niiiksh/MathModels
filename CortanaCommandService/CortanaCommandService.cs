using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Storage;
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

            VoiceCommandUserMessage userMessage;
            VoiceCommandResponse response;
            try
            {
                voiceServiceConnection = VoiceCommandServiceConnection.FromAppServiceTriggerDetails(triggerDetails);
                voiceServiceConnection.VoiceCommandCompleted += VoiceCommandCompleted;
                VoiceCommand voiceCommand = await voiceServiceConnection.GetVoiceCommandAsync();

                switch (voiceCommand.CommandName)
                {
                    case "graphParams":
                        await ShowProgressScreen("Working on it...");
                        var modelnumber = voiceCommand.Properties["modelnumber"][0];
                        double lambda = 0;
                        double mu = 0;
                        int model = Models.Point.GetNumberByModel(Models.Point.GetModelByNumber(modelnumber));
                        
                        if (GetAllParameters(model, voiceCommand, ref lambda, ref mu))
                        {
                            bool allowed = false;
                            bool unsupported = false;
                            if (model.Equals(1) || model.Equals(2))
                            {
                                var responseMessage = new VoiceCommandUserMessage()
                                {
                                    DisplayMessage = String.Format("Get likelihood results for the model {0} with λ={1} and μ={2}?", modelnumber, lambda, mu),
                                    SpokenMessage = String.Format("Do you want me to get likelihood results for the model {0} with these input data?", modelnumber)
                                };
                                var repeatMessage = new VoiceCommandUserMessage()
                                {
                                    DisplayMessage = String.Format("Do you still want me to get likelihood results for the model {0} with λ={1} and μ={2}?", modelnumber, lambda, mu),
                                    SpokenMessage = String.Format("Do you still want me to get likelihood results for the model {0} with these input data?", modelnumber)
                                };

                                response = VoiceCommandResponse.CreateResponseForPrompt(responseMessage, repeatMessage);
                                try
                                {
                                    var confirmation = await voiceServiceConnection.RequestConfirmationAsync(response);
                                    allowed = confirmation.Confirmed;
                                }
                                catch
                                { }
                            }
                            else if (model > 2)
                            {
                                unsupported = true;
                            }

                            if (allowed)
                            {
                                await ShowProgressScreen("Calculating...");
                                List<VoiceCommandContentTile> resultContentTiles = GetLikelihoodForSelectedModel(lambda, mu, model);
                                userMessage = new VoiceCommandUserMessage()
                                {
                                    DisplayMessage = String.Format("Here is your likelihood results for the model {0}", modelnumber),
                                    SpokenMessage = "Done and Done! Here is your results"
                                };
                                response = VoiceCommandResponse.CreateResponse(userMessage, resultContentTiles);
                                response.AppLaunchArgument = modelnumber;
                                await voiceServiceConnection.ReportSuccessAsync(response);
                            }
                            else if (unsupported)
                            {
                                userMessage = new VoiceCommandUserMessage()
                                {
                                    DisplayMessage = String.Format("Model {0} is not supported now", modelnumber),
                                    SpokenMessage = "Sorry, this model is not supported now"
                                };
                                response = VoiceCommandResponse.CreateResponse(userMessage);
                                response.AppLaunchArgument = modelnumber;
                                await voiceServiceConnection.ReportFailureAsync(response);
                            }
                            else
                            {
                                userMessage = new VoiceCommandUserMessage()
                                {
                                    DisplayMessage = "Okay then",
                                    SpokenMessage = "Okay, then"
                                };
                                response = VoiceCommandResponse.CreateResponse(userMessage);
                                await voiceServiceConnection.ReportSuccessAsync(response);
                            }
                        }
                        else
                        {
                            userMessage = new VoiceCommandUserMessage()
                            {
                                DisplayMessage = "The arguments is incorrect",
                                SpokenMessage = "Sorry, it seems the arguments is incorrect"
                            };
                            response = VoiceCommandResponse.CreateResponse(userMessage);
                            response.AppLaunchArgument = "";
                            await voiceServiceConnection.ReportFailureAsync(response);
                        }
                        break;
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
                if (serviceDeferral != null)
                {
                    //Complete the service deferral
                    serviceDeferral.Complete();
                }
            }
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
        private bool GetAllParameters(int model, VoiceCommand voiceCommand, ref double Lambda, ref double Mu)
        {
            bool valid = true;
            switch (model)
            {
                case 1:
                    if (!double.TryParse(voiceCommand.Properties["vLambda"][0], out Lambda)
                        || !double.TryParse(voiceCommand.Properties["vMu"][0], out Mu))
                    { 
                        valid = false;
                        break;
                    }
                    if (double.Parse(voiceCommand.Properties["vLambda"][0]) / double.Parse(voiceCommand.Properties["vMu"][0]) >= 1
                    || double.Parse(voiceCommand.Properties["vLambda"][0]) / double.Parse(voiceCommand.Properties["vMu"][0]) <= 0)
                        valid = false;
                    break;
                case 2:
                    if (!double.TryParse(voiceCommand.Properties["vLambda"][0], out Lambda)
                        || !double.TryParse(voiceCommand.Properties["vMu"][0], out Mu))
                    { 
                        valid = false;
                        break;
                    }
                    if (double.Parse(voiceCommand.Properties["vLambda"][0]) / double.Parse(voiceCommand.Properties["vMu"][0]) <= 0)
                        valid = false;
                    break;
                case 3:
                case 4:
                case 5:
                    break;
                default:
                    valid = false;
                    break;
            }
            return valid;
        }
        private List<VoiceCommandContentTile> GetLikelihoodForSelectedModel(double Lambda,  double Mu, int model)
        {
            var resultContentTiles = new List<VoiceCommandContentTile>();
            switch (model)
            {
                case 1:
                    for (int k = 0; k <= 9; k++)
                    {
                        var modelTile = new VoiceCommandContentTile();
                        modelTile.ContentTileType = VoiceCommandContentTileType.TitleOnly;
                        modelTile.Title = Models.MM1.CortanaCalkPk(Lambda, Mu, k);
                        resultContentTiles.Add(modelTile);
                    }
                    break;
                case 2:
                    for (int k = 0; k <= 9; k++)
                    {
                        var modelTile = new VoiceCommandContentTile();
                        modelTile.ContentTileType = VoiceCommandContentTileType.TitleOnly;
                        modelTile.Title = Models.MMinf.CortanaCalkPk(Lambda, Mu, k);
                        resultContentTiles.Add(modelTile);
                    }
                    break;
                case 3:
                case 4:
                case 5:
                    break;
                default:
                    break;
            }
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
            if (serviceDeferral != null)
            {
                serviceDeferral.Complete();
            }
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (serviceDeferral != null)
            {
                serviceDeferral.Complete();
            }
        }
    }
}
