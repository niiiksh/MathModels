using MathModels.Common;
using MathModels.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// Шаблон элемента пустой страницы задокументирован по адресу http://go.microsoft.com/fwlink/?LinkId=234238

namespace MathModels.View
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    /// 

    public sealed partial class SelectedModel : Page
    {
        const int minPenSize = 2;
        const int penSizeIncrement = 2;
        int penSize;
        private NavigationHelper navigationHelper;
        private SpeechRecognizer speechRecognizer;
        private SpeechRecognizer speechRecognizerContinuous;
        private ManualResetEvent manualResetEvent;

        bool listening = false;

        public SelectedModel()
        {
            InitializeComponent();
            penSize = minPenSize + penSizeIncrement * 1;
            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
            drawingAttributes.Color = Windows.UI.Colors.Orange;
            drawingAttributes.Size = new Size(penSize, penSize);
            drawingAttributes.IgnorePressure = false;
            drawingAttributes.FitToCurve = true;
            inkToolbar.PenSize = new Size(penSize, penSize);

            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
            inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen;

            navigationHelper = new NavigationHelper(this);
            navigationHelper.LoadState += navigationHelper_LoadState;
            navigationHelper.SaveState += navigationHelper_SaveState;
            var view = ApplicationView.GetForCurrentView();

            view.TitleBar.BackgroundColor = Color.FromArgb(255, 253, 115, 19);
            view.TitleBar.ForegroundColor = Colors.White;

            view.TitleBar.ButtonBackgroundColor = Color.FromArgb(255, 253, 115, 19);
            view.TitleBar.ButtonForegroundColor = Colors.White;

            view.TitleBar.ButtonHoverBackgroundColor = Colors.Orange;
            view.TitleBar.ButtonHoverForegroundColor = Colors.White;

            view.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 240, 115, 19);
            view.TitleBar.ButtonPressedForegroundColor = Colors.White;

            Window.Current.CoreWindow.SizeChanged += (ss, ee) =>
            {
                HubSectionGraph.MinWidth = ee.Size.Width - 500;
                listViewResults.Height = ee.Size.Height - 310;
                listView.Margin = new Thickness(264, Window.Current.Bounds.Height - 184, 0, 0);
                listView.Height = Window.Current.Bounds.Height - listViewResults.Height;
                LineChart.Height = ee.Size.Height - 119;
                if (ee.Size.Width - 531 > 0)
                    LineChart.MinWidth = ee.Size.Width - 531;
                inkCanvas.Height = LineChart.Height;
                if (Window.Current.Bounds.Width - 531 > 0)
                    inkCanvas.MinWidth = Window.Current.Bounds.Width - 531;
                inkToolbar.Margin = new Thickness(Window.Current.Bounds.Width - 156, 61, 0, 0);
                toggleSwitch.Margin = new Thickness(26, bResult.Margin.Top + 50, 0, 0);
                ProgressRing.Margin = new Thickness(85, toggleSwitch.Margin.Top + 60, 0, 0);
                bListen.Margin = new Thickness(26, bResult.Margin.Top + 120, 0, 0);
                sayText.Margin = new Thickness(86, bResult.Margin.Top + 120, 0, 0);
            };
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// Resets the state of the view model by passing in the parameters provided by the
        /// caller.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="Common.NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            if (e.NavigationParameter is QueueVoiceCommand)
            {
                // look up destination, set trip.
                QueueVoiceCommand voiceCommand = (QueueVoiceCommand)e.NavigationParameter;
                HubModels.Header = Models.Point.GetModelByNumber(voiceCommand.modelNumber);
                SetControlsPosition(Models.Point.GetModelByNumber(voiceCommand.modelNumber));
                SetRandomInput();
                bResult_Click(this, new RoutedEventArgs());

                // artificially populate the page backstack so we have something to
                // go back to to get to the main page.
                PageStackEntry backEntry = new PageStackEntry(typeof(View.MainPage), null, null);
                Frame.BackStack.Add(backEntry);
            }
            else if (e.NavigationParameter is string)
            {
                // We've been URI Activated, possibly by a user clicking on a tile in a Cortana session,
                // we should see an argument like destination=<Location>. 
                // This should handle finding all of the destinations that match, but currently it only
                // finds the first one.
                string arguments = e.NavigationParameter as string;
                if (arguments != null)
                {
                    string[] args = arguments.Split('=');
                    if (args.Length == 2 && args[0].ToLowerInvariant() == "modelnumber")
                    {
                        HubModels.Header = Models.Point.GetModelByNumber(args[1]);
                        SetControlsPosition(args[1]);
                        SetRandomInput();
                        bResult_Click(this, new RoutedEventArgs());

                        // artificially populate the page backstack so we have something to
                        // go back to to get to the main page.
                        PageStackEntry backEntry = new PageStackEntry(typeof(View.MainPage), null, null);
                        Frame.BackStack.Add(backEntry);
                    }
                }
            }
            else
            {
                Frame.Navigate(typeof(View.MainPage), "");
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="Common.SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="Common.NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper initialization

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="Common.NavigationHelper.LoadState"/>
        /// and <see cref="Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HubModels.Header = e.Parameter.ToString();
            HideScheme();
            //var i = new MessageDialog("You sent me: " + e.Parameter.ToString()).ShowAsync();

            manualResetEvent = new ManualResetEvent(false);
            Media.MediaEnded += Media_MediaEnded;
            //InitContiniousRecognition();

            HubSectionGraph.MinWidth = Window.Current.Bounds.Width - 500;
            listViewResults.Height = Window.Current.Bounds.Height - 310;
            listView.Margin = new Thickness(264, Window.Current.Bounds.Height - 184, 0, 0);
            listView.Height = Window.Current.Bounds.Height - listViewResults.Height;
            LineChart.Height = Window.Current.Bounds.Height - 119;
            LineChart.Width = Window.Current.Bounds.Width - 531;
            LineChart.Visibility = Visibility.Collapsed;
            inkCanvas.Height = LineChart.Height;
            inkCanvas.Width = Window.Current.Bounds.Width - 531;
            inkToolbar.Margin = new Thickness(Window.Current.Bounds.Width - 156, 61, 0, 0);
            inkCanvas.Visibility = Visibility.Collapsed;
            inkToolbar.Visibility = Visibility.Collapsed;
            tbError.Visibility = Visibility.Collapsed;
            toggleSwitch.Visibility = Visibility.Visible;
            SetControlsPosition(HubModels.Header.ToString());

            navigationHelper.OnNavigatedTo(e);
        }
  
        public void SetControlsPosition(string header)
        {
            tbLambda.Visibility = Visibility.Visible;
            tbMu.Visibility = Visibility.Visible;
            tbN.Visibility = Visibility.Visible;
            tbV.Visibility = Visibility.Visible;
            if (header != "M|M|V|K|N")
            {
                if (header == "M|M|1" || header == "M|M|∞"
                    || header == "M|M|V" || header == "M|M|V|K")
                {
                    if (header == "M|M|1" || header == "M|M|∞")
                    {
                        bResult.Margin = new Thickness(26, tbMu.Margin.Top + 84, 0, 0);
                        tbError.Margin = new Thickness(26, bResult.Margin.Top + 54, 0, 0);
                    }
                    if (header == "M|M|∞")
                    {
                        tbLambda.PlaceholderText = "0<λ/μ≤∞";
                        tbMu.PlaceholderText = "0<λ/μ≤∞";
                    }
                }
                else
                {
                    tbLambda.Visibility = Visibility.Collapsed;
                    tbMu.Visibility = Visibility.Collapsed;
                }

                if (header != "M|M|V" && header != "M|M|V|K")
                {
                    tbV.Visibility = Visibility.Collapsed;
                }
                else
                {
                    bResult.Margin = new Thickness(26, tbV.Margin.Top + 84, 0, 0);
                    tbError.Margin = new Thickness(26, bResult.Margin.Top + 54, 0, 0);
                    tbLambda.PlaceholderText = "0<λ/(v*μ)≤1";
                    tbMu.PlaceholderText = "0<λ/(v*μ)≤1";
                    tbV.PlaceholderText = "0<λ/(v*μ)≤1";
                }
                tbN.Visibility = Visibility.Collapsed;
            }
            else
            {
                tbLambda.Header = "Enter a"; //lambda is a in M|M|V|K|N
                tbLambda.PlaceholderText = "0<a<1";
                tbMu.PlaceholderText = "μ>0";
            }
            toggleSwitch.Margin = new Thickness(26, bResult.Margin.Top + 50, 0, 0);
            ProgressRing.Margin = new Thickness(85, toggleSwitch.Margin.Top + 60, 0, 0);
            bListen.Margin = new Thickness(26, bResult.Margin.Top + 120, 0, 0);
            sayText.Margin = new Thickness(86, bResult.Margin.Top + 120, 0, 0);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (listening)
            {
                SetListening(false, Models.Point.GetNumberByModel(HubModels.Header.ToString()));
            }
            resultsReady = false;
            tbLambda.Text = "";
            tbMu.Text = "";
            tbN.Text = "";
            tbV.Text = "";
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        public async Task RefreshSourceState()
        {
            SolidColorBrush whiteColorBrush = new SolidColorBrush();
            whiteColorBrush.Color = Color.FromArgb(255, 255, 255, 255);
            SolidColorBrush greenColorBrush = new SolidColorBrush();
            greenColorBrush.Color = Color.FromArgb(255, 0, 255, 0);

            Random rnd = new Random();
            switch (sourceScheme)
            {
                case 0:
                    customer1.Fill = whiteColorBrush;
                    customer2.Fill = whiteColorBrush;
                    customer3.Fill = whiteColorBrush;
                    break;
                case 1:
                    var c1 = rnd.Next(1, 4);
                    if (c1 == 1)
                    {
                        customer1.Fill = greenColorBrush;
                        customer2.Fill = whiteColorBrush;
                        customer3.Fill = whiteColorBrush;
                    }
                    else if (c1 == 2)
                    {
                        customer2.Fill = greenColorBrush;
                        customer1.Fill = whiteColorBrush;
                        customer3.Fill = whiteColorBrush;
                    }
                    else if (c1 == 3)
                    {
                        customer3.Fill = greenColorBrush;
                        customer1.Fill = whiteColorBrush;
                        customer2.Fill = whiteColorBrush;
                    }
                    break;
                case 2:
                    var c2 = rnd.Next(1, 4);
                    if (c2 == 1)
                    {
                        customer1.Fill = greenColorBrush;
                        customer2.Fill = greenColorBrush;
                        customer3.Fill = whiteColorBrush;
                    }
                    else if (c2 == 2)
                    {
                        customer2.Fill = greenColorBrush;
                        customer3.Fill = greenColorBrush;
                        customer1.Fill = whiteColorBrush;
                    }
                    else if (c2 == 3)
                    {
                        customer1.Fill = greenColorBrush;
                        customer3.Fill = greenColorBrush;
                        customer2.Fill = whiteColorBrush;
                    }
                    break;
                default:
                    customer1.Fill = greenColorBrush;
                    customer2.Fill = greenColorBrush;
                    customer3.Fill = greenColorBrush;
                    break;
            }
            customersCounter.Text = "Source (" + sourceScheme.ToString() + ")";
            await Task.Delay(10);
        }
        private async Task RefreshServiceState()
        {
            SolidColorBrush whiteColorBrush = new SolidColorBrush();
            whiteColorBrush.Color = Color.FromArgb(255, 255, 255, 255);

            SolidColorBrush greenColorBrush = new SolidColorBrush();
            greenColorBrush.Color = Color.FromArgb(255, 0, 255, 0);
            Random rnd = new Random();
            switch (inProcessServices)
            {
                case 0:
                    server1.Fill = whiteColorBrush;
                    server2.Fill = whiteColorBrush;
                    server3.Fill = whiteColorBrush;
                    if (Models.Point.GetNumberByModel(HubModels.Header.ToString()).Equals(3) && queue >= 3)
                    {
                        server1.Fill = greenColorBrush;
                        server2.Fill = greenColorBrush;
                        server3.Fill = greenColorBrush;
                    }
                    break;
                case 1:
                    if (Models.Point.GetNumberByModel(HubModels.Header.ToString()).Equals(1))
                    {
                        server1.Fill = whiteColorBrush;
                        server2.Fill = greenColorBrush;
                        server3.Fill = whiteColorBrush;
                    }
                    else
                    {
                        var s1 = rnd.Next(1, 4);
                        if (s1 == 1)
                        {
                            server1.Fill = greenColorBrush;
                            server2.Fill = whiteColorBrush;
                            server3.Fill = whiteColorBrush;
                        }
                        else if (s1 == 2)
                        {
                            server2.Fill = greenColorBrush;
                            server1.Fill = whiteColorBrush;
                            server3.Fill = whiteColorBrush;
                        }
                        else if (s1 == 3)
                        {
                            server3.Fill = greenColorBrush;
                            server1.Fill = whiteColorBrush;
                            server2.Fill = whiteColorBrush;
                        }
                        if (Models.Point.GetNumberByModel(HubModels.Header.ToString()).Equals(3) && queue >= 3)
                        {
                            server1.Fill = greenColorBrush;
                            server2.Fill = greenColorBrush;
                            server3.Fill = greenColorBrush;
                        }
                    }
                    break;
                case 2:
                    if (Models.Point.GetNumberByModel(HubModels.Header.ToString()).Equals(1))
                    {
                        server1.Fill = whiteColorBrush;
                        server2.Fill = greenColorBrush;
                        server3.Fill = whiteColorBrush;
                    }
                    else
                    {
                        var s2 = rnd.Next(1, 4);
                        if (s2 == 1)
                        {
                            server1.Fill = greenColorBrush;
                            server2.Fill = greenColorBrush;
                            server3.Fill = whiteColorBrush;
                        }
                        else if (s2 == 2)
                        {
                            server2.Fill = greenColorBrush;
                            server3.Fill = greenColorBrush;
                            server1.Fill = whiteColorBrush;
                        }
                        else if (s2 == 3)
                        {
                            server1.Fill = greenColorBrush;
                            server3.Fill = greenColorBrush;
                            server2.Fill = whiteColorBrush;
                        }
                        if (Models.Point.GetNumberByModel(HubModels.Header.ToString()).Equals(3) && queue >= 3)
                        {
                            server1.Fill = greenColorBrush;
                            server2.Fill = greenColorBrush;
                            server3.Fill = greenColorBrush;
                        }
                    }
                    break;
                default:
                    server1.Fill = greenColorBrush;
                    server2.Fill = greenColorBrush;
                    server3.Fill = greenColorBrush;
                    break;
            }
            service.Text = "Service (" + inProcessServices.ToString() + ")";
            await Task.Delay(10);
        }

        private async Task RefreshQueue()
        {
            SolidColorBrush greenLight = new SolidColorBrush();
            greenLight.Color = Color.FromArgb(255, 0, 255, 0);
            SolidColorBrush white = new SolidColorBrush();
            white.Color = Color.FromArgb(255, 255, 255, 255);
            Rectangle[] queueScheme = { queuepart1, queuepart2, queuepart3, queuepart4, queuepart5, queuepart6, queuepart7 };
            if (queue <= 7)
            {
                for (int i = 0; i < 7; i++)
                {
                    queueScheme[i].Fill = white;
                }
                for (int i = 0; i < queue; i++)
                {
                    queueScheme[i].Fill = greenLight;
                    queueCounter.Text = "Queue (" + i.ToString() + ")";
                    await Task.Delay(50);
                }
                sink.Fill = greenLight;
            }
            else if (queue == 0)
            {
                for (int i = 0; i < 7; i++)
                {
                    queueScheme[i].Fill = white;
                }
                sink.Fill = white;
            }
            else
            {
                for (int i = 0; i < 7; i++)
                {
                    queueScheme[i].Fill = greenLight;
                    queueCounter.Text = "Queue (" + i.ToString() + ")";
                    await Task.Delay(50);
                }
                sink.Fill = greenLight;
            }
            queueCounter.Text = "Queue (" + queue.ToString() + ")";
            await Task.Delay(50);
        }

        public bool CheckInput1M2M()
        {
            double input;
            bool edited = false;
            resultsReady = false;
            if (double.TryParse(tbLambda.Text, out input) && double.TryParse(tbMu.Text, out input))
            {
                if (tbLambda.Text == "" || tbMu.Text == "")
                {
                    tbError.Text = "One or more required fields is empty!";
                    tbError.Visibility = Visibility.Visible;
                    toggleSwitch.Visibility = Visibility.Collapsed;
                }
                else if (double.Parse(tbLambda.Text) / double.Parse(tbMu.Text) >= 1 && HubModels.Header.ToString() != "M|M|∞")
                {
                    tbError.Text = String.Format("λ/μ={0} is more or equal 1!", double.Parse(tbLambda.Text) / double.Parse(tbMu.Text));
                    tbError.Visibility = Visibility.Visible;
                    toggleSwitch.Visibility = Visibility.Collapsed;
                }
                else if (double.Parse(tbLambda.Text) / double.Parse(tbMu.Text) <= 0)
                {
                    tbError.Text = String.Format("λ/μ={0} is less or equal 0!", double.Parse(tbLambda.Text) / double.Parse(tbMu.Text));
                    tbError.Visibility = Visibility.Visible;
                    toggleSwitch.Visibility = Visibility.Collapsed;
                }
                else
                {
                    tbError.Visibility = Visibility.Collapsed;
                    toggleSwitch.Visibility = Visibility.Visible;
                    edited = true;
                    resultsReady = true;
                }
            }
            else
            {
                tbError.Text = "Invalid input!";
                tbError.Visibility = Visibility.Visible;
                toggleSwitch.Visibility = Visibility.Collapsed;
            }
            return edited;
        }

        public bool CheckInput3M4M()
        {
            double input;
            int intinput;
            bool edited = false;
            resultsReady = false;
            if (double.TryParse(tbLambda.Text, out input) && double.TryParse(tbMu.Text, out input)
                    && int.TryParse(tbV.Text, out intinput))
            {
                if (tbLambda.Text == "" || tbMu.Text == "" || tbV.Text == "")
                {
                    tbError.Text = "One or more required fields is empty!";
                    tbError.Visibility = Visibility.Visible;
                    toggleSwitch.Visibility = Visibility.Collapsed;
                }
                else if (double.Parse(tbLambda.Text) / (int.Parse(tbV.Text) * double.Parse(tbMu.Text)) >= 1)
                {
                    tbError.Text = String.Format("λ/(v*μ)={0} is more or equal 1!", double.Parse(tbLambda.Text) / (int.Parse(tbV.Text) * double.Parse(tbMu.Text)));
                    tbError.Visibility = Visibility.Visible;
                    toggleSwitch.Visibility = Visibility.Collapsed;
                }
                else if (double.Parse(tbLambda.Text) / (int.Parse(tbV.Text) * double.Parse(tbMu.Text)) <= 0)
                {
                    tbError.Text = String.Format("λ/(v*μ)={0} is less or equal 0!", double.Parse(tbLambda.Text) / (int.Parse(tbV.Text) * double.Parse(tbMu.Text)));
                    tbError.Visibility = Visibility.Visible;
                    toggleSwitch.Visibility = Visibility.Collapsed;
                }
                else if (int.Parse(tbV.Text) <= 0)
                {
                    tbError.Text = String.Format("v={0} is less or equal 0!", int.Parse(tbV.Text));
                    tbError.Visibility = Visibility.Visible;
                    toggleSwitch.Visibility = Visibility.Collapsed;
                }
                else
                {
                    tbError.Visibility = Visibility.Collapsed;
                    toggleSwitch.Visibility = Visibility.Visible;
                    edited = true;
                    resultsReady = true;
                }
            }
            else
            {
                tbError.Text = "Invalid input!";
                tbError.Visibility = Visibility.Visible;
                toggleSwitch.Visibility = Visibility.Collapsed;
            }
            return edited;
        }
        public bool CheckInput5M()
        {
            double input;
            int intinput;
            bool edited = false;
            resultsReady = false;
            if (double.TryParse(tbLambda.Text, out input) && !tbLambda.Text.Contains(",") && double.TryParse(tbMu.Text, out input)
                    && int.TryParse(tbN.Text, out intinput) && int.TryParse(tbV.Text, out intinput))
            {
                if (tbLambda.Text == "" || tbMu.Text == "" || tbV.Text == "" || tbN.Text == "")
                {
                    tbError.Text = "One or more required fields is empty!";
                    tbError.Visibility = Visibility.Visible;
                    toggleSwitch.Visibility = Visibility.Collapsed;
                }
                else if (double.Parse(tbLambda.Text) <= 0)
                {
                    tbError.Text = String.Format("a={0} is less or equal 0!", double.Parse(tbLambda.Text));
                    tbError.Visibility = Visibility.Visible;
                    toggleSwitch.Visibility = Visibility.Collapsed;
                }
                else if (double.Parse(tbLambda.Text) >= 1)
                {
                    tbError.Text = String.Format("a={0} is more or equal 1!", double.Parse(tbLambda.Text));
                    tbError.Visibility = Visibility.Visible;
                    toggleSwitch.Visibility = Visibility.Collapsed;
                }
                else if (double.Parse(tbMu.Text) <= 0)
                {
                    tbError.Text = String.Format("μ={0} is less or equal 0!", double.Parse(tbMu.Text));
                    tbError.Visibility = Visibility.Visible;
                    toggleSwitch.Visibility = Visibility.Collapsed;
                }
                else if (double.Parse(tbN.Text) < double.Parse(tbV.Text))
                {
                    tbError.Text = String.Format("N={0} is less than V={1}!", double.Parse(tbN.Text), double.Parse(tbV.Text));
                    tbError.Visibility = Visibility.Visible;
                    toggleSwitch.Visibility = Visibility.Collapsed;
                }
                else
                {
                    tbError.Visibility = Visibility.Collapsed;
                    toggleSwitch.Visibility = Visibility.Visible;
                    edited = true;
                    resultsReady = true;
                }
            }
            else
            {
                tbError.Text = "Invalid input!";
                tbError.Visibility = Visibility.Visible;
                toggleSwitch.Visibility = Visibility.Collapsed;
            }
            return edited;
        }

        public void SetRandomInput()
        {
            if (HubModels.Header.ToString() != "M|M|V|K|N")
            {
                if (HubModels.Header.ToString() == "M|M|1" || HubModels.Header.ToString() == "M|M|∞"
                    || HubModels.Header.ToString() == "M|M|V" || HubModels.Header.ToString() == "M|M|V|K")
                {
                    Random rand = new Random();
                    int l = 1, m = 0;
                    while (l >= m)
                    {
                        l = rand.Next(1, 100);
                        m = rand.Next(1, 100);
                    }
                    tbLambda.Text = l.ToString();
                    tbMu.Text = m.ToString();
                }

                if (HubModels.Header.ToString() == "M|M|V" || HubModels.Header.ToString() == "M|M|V|K")
                {
                    Random rand = new Random();
                    int l = 2, m = 1, v = 1;
                    while (l / (v * m) >= 1)
                    {
                        l = rand.Next(1, 100);
                        m = rand.Next(1, 100);
                        v = rand.Next(1, 100);
                    }
                    tbLambda.Text = l.ToString();
                    tbMu.Text = m.ToString();
                    tbV.Text = v.ToString();
                }
            }
            else
            {
                Random rand = new Random();
                double a = 0.1;
                int m = 1, v = 1, n = 0;
                while (n < v || m <= 0 || a >= 0.999 || a <= 0)
                {

                    a = rand.NextDouble();
                    m = rand.Next(1, 100);
                    v = rand.Next(1, 100);
                    n = rand.Next(v, v + 50);
                }
                tbLambda.Text = a.ToString();
                tbMu.Text = m.ToString();
                tbV.Text = v.ToString();
                tbN.Text = n.ToString();
            }
        }
        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (toggleSwitch.IsOn && tbError.Visibility == Visibility.Collapsed && LineChart.Visibility == Visibility.Visible)
            {
                inkCanvas.Visibility = Visibility.Visible;
                inkToolbar.Visibility = Visibility.Visible;
            }
            else if (toggleScheme.IsOn)
            {
                toggleSwitch.IsOn = false;
            }
            else
            {
                inkCanvas.Visibility = Visibility.Collapsed;
                inkToolbar.Visibility = Visibility.Collapsed;
                inkCanvas.InkPresenter.StrokeContainer.Clear();
            }
        }
        bool resultsReady = false;
        public async void bResult_Click(object sender, RoutedEventArgs e)
        {
            queue = 0;
            proceeded = 0;
            packetsCount = 0;
            sourcePackets = 0;
            inProcessServices = 0;
            inProcessMax = 0;
            lostPackets = 0;
            lastActive = 0;
            tbError.Visibility = Visibility.Collapsed;
            sayText.Visibility = Visibility.Collapsed;
            bListen.IsEnabled = false;
            ProgressRing.IsActive = true;
            await Task.Delay(100);
            await Task.Yield();
            listViewResults.Items.Clear();
            listView.Items.Clear();
            if ((HubModels.Header.ToString() == "M|M|1" || HubModels.Header.ToString() == "M|M|∞") && CheckInput1M2M())
            {
                if (!toggleScheme.IsOn)
                {
                    LineChart.Visibility = Visibility.Visible;
                }
                listViewResults.Header = "P(k):";
                if (HubModels.Header.ToString() == "M|M|1")
                {
                    Models.MM1.CalcPk(double.Parse(tbLambda.Text), double.Parse(tbMu.Text), listViewResults, LineChart);
                    listView.Items.Add("K(avg) = " + Models.MM1.CalcK_Avg(double.Parse(tbLambda.Text), double.Parse(tbMu.Text)).ToString());
                    listView.Items.Add("W[s](avg) = " + Models.MM1.CalcWs_Avg(double.Parse(tbLambda.Text), double.Parse(tbMu.Text)).ToString());
                    listView.Items.Add("L[q](avg) = " + Models.MM1.CalcLq_Avg(double.Parse(tbLambda.Text), double.Parse(tbMu.Text)).ToString());
                    listView.Items.Add("W[q](avg) = " + Models.MM1.CalcWq_Avg(double.Parse(tbLambda.Text), double.Parse(tbMu.Text)).ToString());
                }
                else if (HubModels.Header.ToString() == "M|M|∞")
                {
                    Models.MMinf.CalcPk(double.Parse(tbLambda.Text), double.Parse(tbMu.Text), listViewResults, LineChart);
                    listView.Items.Add("K(avg) = " + Models.MMinf.CalcK_Avg(double.Parse(tbLambda.Text), double.Parse(tbMu.Text)).ToString());
                    listView.Items.Add("W[s](avg) = " + Models.MMinf.CalcWs_Avg(double.Parse(tbLambda.Text), double.Parse(tbMu.Text)).ToString());
                }
            }
            if ((HubModels.Header.ToString() == "M|M|V" || HubModels.Header.ToString() == "M|M|V|K") && CheckInput3M4M())
            {
                if (!toggleScheme.IsOn)
                {
                    LineChart.Visibility = Visibility.Visible;
                }
                if (HubModels.Header.ToString() == "M|M|V")
                {
                    listViewResults.Header = "P(i) + W(j):";
                    LineChart.Title = "P(k): P(i) + W(j)";
                    Models.MMV.CalcPi(double.Parse(tbLambda.Text), double.Parse(tbMu.Text), int.Parse(tbV.Text), listViewResults, LineChart);
                    Models.MMV.CalcWj(double.Parse(tbLambda.Text), double.Parse(tbMu.Text), int.Parse(tbV.Text), listViewResults, LineChart);
                    listView.Items.Add("ɣ(avg) = " + Models.MMV.CalcGamma_Avg(double.Parse(tbLambda.Text), double.Parse(tbMu.Text), int.Parse(tbV.Text)));
                    listView.Items.Add("j(avg) = " + Models.MMV.CalcJ_Avg(double.Parse(tbLambda.Text), double.Parse(tbMu.Text), int.Parse(tbV.Text)));
                    listView.Items.Add("P[t] = " + Models.MMV.CalcPt(double.Parse(tbLambda.Text), double.Parse(tbMu.Text), int.Parse(tbV.Text)));
                }
                else if (HubModels.Header.ToString() == "M|M|V|K")
                {
                    listViewResults.Header = "P(k):";
                    Models.MMVK.CalcPk(double.Parse(tbLambda.Text), double.Parse(tbMu.Text), int.Parse(tbV.Text), listViewResults, LineChart);
                    listView.Items.Add("P[v] = " + Models.MMVK.CalcPv(double.Parse(tbLambda.Text), double.Parse(tbMu.Text), int.Parse(tbV.Text)));
                }
            }

            if (HubModels.Header.ToString() == "M|M|V|K|N" && CheckInput5M())
            {
                if (!toggleScheme.IsOn)
                {
                    LineChart.Visibility = Visibility.Visible;
                }
                listViewResults.Header = "P[k]";
                Models.MMVKN.CalcPk(double.Parse(tbLambda.Text), double.Parse(tbMu.Text), int.Parse(tbV.Text), uint.Parse(tbN.Text), listViewResults, LineChart);
                listView.Items.Add("K(avg) = " + Models.MMVKN.CalcK_Avg(double.Parse(tbLambda.Text), double.Parse(tbMu.Text), int.Parse(tbV.Text), uint.Parse(tbN.Text)));
                listView.Items.Add("T(avg) = " + Models.MMVKN.CalcT_Avg(double.Parse(tbLambda.Text), double.Parse(tbMu.Text), int.Parse(tbV.Text), uint.Parse(tbN.Text)));
                listView.Items.Add("P[t] = " + Models.MMVKN.CalcPt(double.Parse(tbLambda.Text), uint.Parse(tbV.Text), uint.Parse(tbN.Text)));
                listView.Items.Add("P[v] = " + Models.MMVKN.CalcPv(double.Parse(tbLambda.Text), uint.Parse(tbV.Text), uint.Parse(tbN.Text)));
            }
            ProgressRing.IsActive = false;
            bListen.IsEnabled = true;
            sayText.Visibility = Visibility.Visible;
            ToggleSwitch_Toggled(this, new RoutedEventArgs());
            inkCanvas.InkPresenter.StrokeContainer.Clear();
        }

        private void Listen_Click(object sender, RoutedEventArgs e)
        {
            SetListening(!listening, Models.Point.GetNumberByModel(HubModels.Header.ToString()));
        }

        private async Task SpeakAsync(string toSpeak)
        {
            SpeechSynthesizer speechSyntesizer = new SpeechSynthesizer();
            SpeechSynthesisStream syntStream = await speechSyntesizer.SynthesizeTextToStreamAsync(toSpeak);
            Media.SetSource(syntStream, syntStream.ContentType);

            Task t = Task.Run(() =>
            {
                manualResetEvent.Reset();
                manualResetEvent.WaitOne();
            });

            await t;
        }

        private async Task InitSpeech()
        {
            if (speechRecognizer == null)
            {
                try
                {
                    speechRecognizer = new SpeechRecognizer();

                    SpeechRecognitionCompilationResult compilationResult = await speechRecognizer.CompileConstraintsAsync();
                    speechRecognizer.HypothesisGenerated += SpeechRecognizer_HypothesisGenerated;

                    if (compilationResult.Status != SpeechRecognitionResultStatus.Success)
                        throw new Exception();

                    Debug.WriteLine("SpeechInit OK");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("SpeechInit Failed: " + ex.Message);
                    speechRecognizer = null;
                }
            }
        }
        private async Task InitContiniousRecognition()
        {
            try
            {
                if (speechRecognizerContinuous == null)
                {
                    speechRecognizerContinuous = new SpeechRecognizer();
                    speechRecognizerContinuous.Constraints.Add(
                        new SpeechRecognitionListConstraint(
                            new List<String>() { "Start listening" }, "start"));
                    SpeechRecognitionCompilationResult contCompilationResult =
                        await speechRecognizerContinuous.CompileConstraintsAsync();


                    if (contCompilationResult.Status != SpeechRecognitionResultStatus.Success)
                    {
                        throw new Exception();
                    }
                    speechRecognizerContinuous.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;
                }
                await speechRecognizerContinuous.ContinuousRecognitionSession.StartAsync();
            }
            catch (Exception ex)
            {
                int privacyPolicyHResult = unchecked((int)0x80045509);
                if (ex.HResult == privacyPolicyHResult)
                {
                    var i = new MessageDialog("You will need to accept the speech privacy policy in order to use speech recognition in this app. Here is the error code in case you need it: 0x" + ex.HResult.ToString("X8")).ShowAsync();
                }
                else
                {
                    var i = new MessageDialog("Speech recognizer failed. Here is the error code in case you need it: 0x" + ex.HResult.ToString("X8")).ShowAsync();
                }
            }
        }

        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            if (args.Result.Confidence == SpeechRecognitionConfidence.Medium ||
                args.Result.Confidence == SpeechRecognitionConfidence.High)
            {
                if (args.Result.Text == "Start listening")
                {
                    await Media.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        SetListening(true, Models.Point.GetNumberByModel(HubModels.Header.ToString()));
                    });
                }
            }
        }

        private async Task SetListening(bool toListen, int model)
        {
            if (toListen)
            {
                listening = true;
                tbLambda.IsEnabled = false;
                mic.Symbol = Symbol.Cancel;
                tbLambda.PlaceholderText = "Waiting...";

                if (speechRecognizerContinuous != null)
                    await speechRecognizerContinuous.ContinuousRecognitionSession.CancelAsync();

                StartListenMode();
            }
            else
            {
                listening = false;
                tbLambda.IsEnabled = true;
                mic.Symbol = Symbol.Microphone;
                ProgressRing.IsActive = false;

                if (speechRecognizerContinuous != null)
                    await speechRecognizerContinuous.ContinuousRecognitionSession.StartAsync();
            }
        }

        private async void StartListenMode()
        {
            while (listening)
            {
                string spokenText = await ListenForText();
                while (string.IsNullOrWhiteSpace(spokenText) && listening)
                    spokenText = await ListenForText();

                if (spokenText.ToLower().Contains("stop listening"))
                {
                    speechRecognizer.UIOptions.AudiblePrompt = "Are you sure you want me to stop listening?";
                    speechRecognizer.UIOptions.ExampleText = "Yes/No";
                    speechRecognizer.UIOptions.ShowConfirmation = false;
                    SpeakAsync(speechRecognizer.UIOptions.AudiblePrompt);
                    var result = await speechRecognizer.RecognizeWithUIAsync();

                    if (!string.IsNullOrWhiteSpace(result.Text) && (result.Text.ToLower() == "yes"
                                                                    || result.Text.ToLower() == "i'm sure"
                                                                    || result.Text.ToLower() == "i am sure"))
                    {
                        await SetListening(false, 0);
                    }
                }

                if (listening)
                {
                    tbLambda.Text = spokenText;
                }
            }
        }

        private async Task<string> ListenForText()
        {
            string result = "";
            await InitSpeech();
            try
            {
                tbError.Visibility = Visibility.Collapsed;
                toggleSwitch.Visibility = Visibility.Visible;
                ProgressRing.IsActive = true;
                sayText.Visibility = Visibility.Collapsed;
                tbLambda.PlaceholderText = "Listening...";
                SpeechRecognitionResult speechRecognitionResult = await speechRecognizer.RecognizeAsync();
                if (speechRecognitionResult.Status == SpeechRecognitionResultStatus.Success)
                {
                    result = speechRecognitionResult.Text;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                ProgressRing.IsActive = false;
                sayText.Visibility = Visibility.Visible;
                tbLambda.PlaceholderText = "placeholder_temp";
            }
            return result;
        }

        private async void SpeechRecognizer_HypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            await tbLambda.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                tbLambda.Text = args.Hypothesis.Text;
            });
        }

        private void Media_MediaEnded(object sender, RoutedEventArgs e)
        {
            manualResetEvent.Set();
        }

        private void HideScheme()
        {
            customer1.Visibility = Visibility.Collapsed;
            customer2.Visibility = Visibility.Collapsed;
            customer3.Visibility = Visibility.Collapsed;
            queuepart1.Visibility = Visibility.Collapsed;
            queuepart2.Visibility = Visibility.Collapsed;
            queuepart3.Visibility = Visibility.Collapsed;
            queuepart4.Visibility = Visibility.Collapsed;
            queuepart5.Visibility = Visibility.Collapsed;
            queuepart6.Visibility = Visibility.Collapsed;
            queuepart7.Visibility = Visibility.Collapsed;
            server1.Visibility = Visibility.Collapsed;
            server2.Visibility = Visibility.Collapsed;
            server3.Visibility = Visibility.Collapsed;
            sink.Visibility = Visibility.Collapsed;
            service.Visibility = Visibility.Collapsed;
            lossScheme.Visibility = Visibility.Collapsed;
            sliderInterval.Visibility = Visibility.Collapsed;
            customersCounter.Visibility = Visibility.Collapsed;
            queueCounter.Visibility = Visibility.Collapsed;
            sinkCounter.Visibility = Visibility.Collapsed;
        }

        private void toggleScheme_Toggled(object sender, RoutedEventArgs e)
        {
            if (toggleScheme.IsOn)
            {
                LineChart.Visibility = Visibility.Collapsed;
                inkCanvas.Visibility = Visibility.Collapsed;
                inkToolbar.Visibility = Visibility.Collapsed;
                inkCanvas.InkPresenter.StrokeContainer.Clear();
                sliderInterval.Visibility = Visibility.Visible;
                switch (Models.Point.GetNumberByModel(HubModels.Header.ToString()))
                {
                    case 1:
                        customer1.Visibility = Visibility.Visible;
                        customer2.Visibility = Visibility.Visible;
                        customer3.Visibility = Visibility.Visible;
                        queuepart1.Visibility = Visibility.Visible;
                        queuepart2.Visibility = Visibility.Visible;
                        queuepart3.Visibility = Visibility.Visible;
                        queuepart4.Visibility = Visibility.Visible;
                        queuepart5.Visibility = Visibility.Visible;
                        queuepart6.Visibility = Visibility.Visible;
                        queuepart7.Visibility = Visibility.Visible;
                        server1.Visibility = Visibility.Collapsed;
                        server2.Visibility = Visibility.Visible;
                        server3.Visibility = Visibility.Collapsed;
                        sink.Visibility = Visibility.Visible;
                        service.Visibility = Visibility.Visible;
                        lossScheme.Visibility = Visibility.Collapsed;
                        customersCounter.Visibility = Visibility.Visible;
                        queueCounter.Visibility = Visibility.Visible;
                        sinkCounter.Visibility = Visibility.Visible;
                        break;
                    case 2:
                    case 3:
                        customer1.Visibility = Visibility.Visible;
                        customer2.Visibility = Visibility.Visible;
                        customer3.Visibility = Visibility.Visible;
                        queuepart1.Visibility = Visibility.Visible;
                        queuepart2.Visibility = Visibility.Visible;
                        queuepart3.Visibility = Visibility.Visible;
                        queuepart4.Visibility = Visibility.Visible;
                        queuepart5.Visibility = Visibility.Visible;
                        queuepart6.Visibility = Visibility.Visible;
                        queuepart7.Visibility = Visibility.Visible;
                        server1.Visibility = Visibility.Visible;
                        server2.Visibility = Visibility.Visible;
                        server3.Visibility = Visibility.Visible;
                        sink.Visibility = Visibility.Visible;
                        service.Visibility = Visibility.Visible;
                        lossScheme.Visibility = Visibility.Collapsed;
                        customersCounter.Visibility = Visibility.Visible;
                        queueCounter.Visibility = Visibility.Visible;
                        sinkCounter.Visibility = Visibility.Visible;
                        break;
                    case 4:
                    case 5:
                        customer1.Visibility = Visibility.Visible;
                        customer2.Visibility = Visibility.Visible;
                        customer3.Visibility = Visibility.Visible;
                        queuepart1.Visibility = Visibility.Collapsed;
                        queuepart2.Visibility = Visibility.Collapsed;
                        queuepart3.Visibility = Visibility.Collapsed;
                        queuepart4.Visibility = Visibility.Collapsed;
                        queuepart5.Visibility = Visibility.Collapsed;
                        queuepart6.Visibility = Visibility.Collapsed;
                        queuepart7.Visibility = Visibility.Collapsed;
                        sink.Margin = new Thickness(queuepart5.Margin.Left + 120, sink.Margin.Top, 0, 0);
                        server1.Margin = new Thickness(queuepart5.Margin.Left, server1.Margin.Top, 0, 0);
                        server2.Margin = new Thickness(queuepart5.Margin.Left, server2.Margin.Top, 0, 0);
                        server3.Margin = new Thickness(queuepart5.Margin.Left, server3.Margin.Top, 0, 0);
                        sinkCounter.Margin = new Thickness(sink.Margin.Left, sinkCounter.Margin.Top, 0, 0);
                        service.Margin = new Thickness(server1.Margin.Left, service.Margin.Top, 0, 0);
                        lossScheme.Margin = new Thickness(server1.Margin.Left, lossScheme.Margin.Top, 0, 0);
                        server1.Visibility = Visibility.Visible;
                        server2.Visibility = Visibility.Visible;
                        server3.Visibility = Visibility.Visible;
                        sink.Visibility = Visibility.Visible;
                        service.Visibility = Visibility.Visible;
                        lossScheme.Visibility = Visibility.Visible;
                        customersCounter.Visibility = Visibility.Visible;
                        queueCounter.Visibility = Visibility.Collapsed;
                        sinkCounter.Visibility = Visibility.Visible;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                if (toggleSwitch.IsOn)
                {
                    inkCanvas.Visibility = Visibility.Visible;
                    inkToolbar.Visibility = Visibility.Visible;
                }
                if (resultsReady)
                {
                    LineChart.Visibility = Visibility.Visible;
                }
                HideScheme();
            }
        }
        int sourcePackets = 0;
        int sourceScheme = 0;
        int onServiceMu = 0;
        int inProcessServices = 0;
        int inProcessMax = 0;
        int lostPackets = 0;
        int packetsCount = 0;
        int proceeded = 0;
        int queue = 0;
        int lastActive = 0;
        int[] everyServicesMu = null;
        int sumServicesMu = 0;
        int currentQueue = 0;
        bool first = true;
        bool isQueueEmpty = false;
        //TODO: New packet
        private async Task ProcessNewPacket(double Lambda, double Mu, int V, int N)
        {
            switch(Models.Point.GetNumberByModel(HubModels.Header.ToString()))
            {
                case 1:
                    sourcePackets = Models.PoissonDistribution.GetPoisson(Lambda);
                    sourceScheme = sourcePackets;
                    await RefreshSourceState();
                    inProcessMax = 1;
                    onServiceMu = Models.PoissonDistribution.GetPoisson(Mu);

                    if (currentQueue > 0)
                    {
                        sourcePackets += currentQueue;
                        queue -= currentQueue;
                        await RefreshQueue();
                        currentQueue = 0;
                        first = true;
                    }
                    else
                        first = false;
                    while (sourceScheme > 0)
                    {
                        sourceScheme--;
                        await RefreshSourceState();
                    }
                    break;
                case 2:
                    sourcePackets = Models.PoissonDistribution.GetPoisson(Lambda);
                    sourceScheme = sourcePackets;
                    inProcessServices = 0;
                    break;
                case 3:
                    sourcePackets = Models.PoissonDistribution.GetPoisson(Lambda);
                    sourceScheme = sourcePackets;
                    await RefreshSourceState();
                    
                    if (first)
                    {
                        inProcessMax = V;
                        everyServicesMu = new int[V];
                        first = false;
                    }
                    if (currentQueue > 0)
                    {
                        sourcePackets += currentQueue;
                        queue -= currentQueue;
                        await RefreshQueue();
                        currentQueue = 0;
                    }
                    int sum3 = 0;
                    sumServicesMu = 0;
                    for (int i = 1; i <= V && sumServicesMu == 0; i++)
                    {
                        everyServicesMu[i - 1] = Models.PoissonDistribution.GetPoisson(Mu);
                        sum3 += everyServicesMu[i - 1];
                        if (sum3 >= sourcePackets)
                        {
                            inProcessServices = i;
                            await RefreshServiceState();
                            sumServicesMu = sum3;
                        }
                    }
                    if (sumServicesMu == 0)
                    {
                        inProcessServices = V;
                        await RefreshServiceState();
                        sumServicesMu = sum3;
                    }
                    while (sourceScheme > 0)
                    {
                        sourceScheme--;
                        await RefreshSourceState();
                    }
                    break;
                case 4:
                    sourcePackets = Models.PoissonDistribution.GetPoisson(Lambda);
                    sourceScheme = sourcePackets;
                    inProcessServices = 0;
                    inProcessMax = V;
                    break;
                case 5:
                    sourcePackets = 0;
                    if (lastActive != 0 && N - lastActive + 1 != 0)
                        sourcePackets = Models.PoissonDistribution.GetPoisson(Lambda) * (N - lastActive + 1);
                    else
                        sourcePackets = N * Models.PoissonDistribution.GetPoisson(Lambda);
                    sourceScheme = sourcePackets;
                    inProcessMax = V;
                    break;
            }
            await RefreshSourceState();
        }
        
        private async Task AddInQueue1()
        {
            if (sourcePackets > onServiceMu)
            {
                isQueueEmpty = false;
                currentQueue = sourcePackets - onServiceMu;
                inProcessServices = 1;
                await RefreshServiceState();
            }
            else if (sourcePackets == 0)
            {
                isQueueEmpty = true;
                inProcessServices = 0;
                await RefreshServiceState();
            }
            else
            {
                isQueueEmpty = true;
                currentQueue = 0;
                queue = 0;
                await RefreshQueue();
                inProcessServices = 1;
                await RefreshServiceState();
            }
            if (!isQueueEmpty)
            {
                queue += currentQueue;
                await RefreshQueue();
            }
        }
        
        private async Task ProceededSink1()
        {
            sinkCounter.Text = "Sink (" + (proceeded += onServiceMu).ToString() + ")";
            await Task.Delay(Convert.ToInt32(sliderInterval.Value));
        }

        //TODO: Add in Queue model MMinf
        private async void AddInQueue2()
        {
            while (sourceScheme > 0)
            {
                sourceScheme--;
                await RefreshSourceState();
            }
        }

        //TODO: Sink in model MMinf
        private async void ProceededSink2(double mu)
        {
            int num = 0;
            int i = 1, j = 0;
            int rndMu = 0;
            rndMu = Models.PoissonDistribution.GetPoisson(mu);
            Debug.WriteLine(rndMu);
            for (int packet = 1; packet <= sourcePackets; packet++)
            {
                if (j == 0)
                {
                    if (sourcePackets != 0)
                        inProcessServices++;
                    j++;
                }
                else
                {
                    if (i == rndMu)
                    {
                        inProcessServices++;
                        i = 0;
                        num += rndMu;
                        rndMu = Models.PoissonDistribution.GetPoisson(mu);
                        Debug.WriteLine(rndMu);
                        j++;
                    }
                    i++;
                }
            }
            await RefreshServiceState();
            await RefreshQueue();
            sinkCounter.Text = "Sink (" + (proceeded += num).ToString() + ")";
        }
        
        private async Task AddInQueue3()
        {
            if (sourcePackets > sumServicesMu)
            {
                isQueueEmpty = false;
                currentQueue = sourcePackets - sumServicesMu;
            }
            else if (sourcePackets == 0)
            {
                isQueueEmpty = true;
                inProcessServices = 0;
                await RefreshServiceState();
            }
            else
            {
                isQueueEmpty = true;
                currentQueue = 0;
                queue = 0;
                await RefreshQueue();
            }
            queue += currentQueue;
            await RefreshQueue();
        }
        
        private async Task ProceededSink3(double mu)
        {
            sinkCounter.Text = "Sink (" + (proceeded += sumServicesMu).ToString() + ")";
            inProcessServices = 0;
            await RefreshServiceState();
            await Task.Delay(Convert.ToInt32(sliderInterval.Value));
        }

        //TODO: Sink in model MMVK
        private async void ProceededSink4(int packets, double mu)
        {
            List<int> arr = new List<int>();
            int num = 0;
            for (var k = 0; k < inProcessMax; k++)
            {
                var num1 = Models.PoissonDistribution.GetPoisson(mu);
                arr.Add(num1);
                num += num1;
            }
            int i = 1, j = 0;
            for (var packet = 1; packet <= packets; packet++)
            {
                var busyServices = inProcessServices;
                if (j == 0)
                {
                    if (packets != 0)
                        inProcessServices++;
                    j++;
                }
                else
                {
                    if (j < arr.Count && i == arr[j])
                    {
                        if (busyServices < inProcessMax)
                            inProcessServices++;
                        i = 0;
                        j++;
                    }
                    i++;
                }
            }
            await RefreshServiceState();

            packetsCount += packets;
            if (packets < num)
                proceeded += packets;
            else
                proceeded += num;

            if (inProcessServices == inProcessMax)
            {
                lostPackets = packetsCount - proceeded;
            }
            lossScheme.Text = "Loss (" + lostPackets.ToString() + ")";
            sinkCounter.Text = "Sink (" + packetsCount.ToString() + ")";
        }

        //TODO: Sink in model MMVKN
        private async void ProceededSink5(int packets, double mu)
        {
            List<int> arr = new List<int>();
            int num = 0;
            int i, j;
            for (i = 0; i < inProcessMax; i++)
            {
                var num1 = Models.PoissonDistribution.GetPoisson(mu);
                arr.Add(num1);
                num += num1;
            }
            i = 1;
            j = 0;
            for (var packet = 1; packet <= packets; packet++)
            {
                var busyServices = inProcessServices;
                if (j == 0)
                {
                    if (packets != 0)
                        inProcessServices++;
                    j++;
                }
                else
                {
                    if (inProcessServices < inProcessMax && i == arr[j])
                    {
                        if (busyServices < inProcessMax)
                            inProcessServices++;
                        i = 0;
                        j++;
                    }
                    i++;
                }
            }
            await RefreshServiceState();

            packetsCount += packets;
            if (packets < num)
                proceeded += packets;
            else
                proceeded += num;


            lostPackets = packetsCount - proceeded;
            lastActive = inProcessServices;
            lossScheme.Text = "Loss (" + lostPackets.ToString() + ")";
            sinkCounter.Text = "Sink (" + packetsCount.ToString() + ")";
            inProcessServices = 0;
        }
        bool stopScheme = false;
        private async void HubModels_Loaded(object sender, RoutedEventArgs e)
        {
            double inLambda = 0, inMu = 0;
            int serviceMax = 0;
            int N = 0;
            while (true)
            {
                if (resultsReady && !stopScheme)
                {
                    switch (Models.Point.GetNumberByModel(HubModels.Header.ToString()))
                    {
                        case 1:
                            if (double.TryParse(tbLambda.Text, out inLambda) && double.TryParse(tbMu.Text, out inMu))
                                await ProcessNewPacket(inLambda, inMu, serviceMax, N);
                            await AddInQueue1();
                            await ProceededSink1();
                            break;
                        case 2:
                            if (double.TryParse(tbLambda.Text, out inLambda) && double.TryParse(tbMu.Text, out inMu))
                                await ProcessNewPacket(inLambda, inMu, serviceMax, N);
                            await Task.Delay(Convert.ToInt32(sliderInterval.Value));
                            AddInQueue2();
                            await Task.Delay(Convert.ToInt32(sliderInterval.Value));
                            if (double.TryParse(tbMu.Text, out inMu))
                                ProceededSink2(inMu);
                            break;
                        case 3:
                            if (double.TryParse(tbLambda.Text, out inLambda) && double.TryParse(tbMu.Text, out inMu) && int.TryParse(tbV.Text, out serviceMax))
                                await ProcessNewPacket(inLambda, inMu, serviceMax, N);
                            await AddInQueue3();
                            if (double.TryParse(tbMu.Text, out inMu))
                                await ProceededSink3(inMu);
                            break;
                        case 4:
                            if (double.TryParse(tbLambda.Text, out inLambda) && double.TryParse(tbMu.Text, out inMu) && int.TryParse(tbV.Text, out serviceMax))
                                await ProcessNewPacket(inLambda, inMu, serviceMax, N);
                            await Task.Delay(Convert.ToInt32(sliderInterval.Value));
                            if (double.TryParse(tbMu.Text, out inMu))
                                ProceededSink4(sourcePackets, inMu);
                            break;
                        case 5:
                            if (double.TryParse(tbLambda.Text, out inLambda) && double.TryParse(tbMu.Text, out inMu) && int.TryParse(tbV.Text, out serviceMax) && int.TryParse(tbN.Text, out N))
                                await ProcessNewPacket(inLambda, inMu, serviceMax, N);
                            await Task.Delay(Convert.ToInt32(sliderInterval.Value));
                            if (double.TryParse(tbMu.Text, out inMu))
                                ProceededSink5(sourcePackets, inMu);
                            break;
                    }
                }
                if (sliderInterval.Value == 0)
                {
                    stopScheme = true;
                }
                else
                {
                    stopScheme = false;
                }
                await Task.Delay(Convert.ToInt32(1));
            }
        }
    }
}
