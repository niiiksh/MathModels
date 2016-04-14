using MathModels.Common;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента "Пустая страница" см. по адресу http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MathModels.View
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private NavigationHelper navigationHelper;
        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return navigationHelper; }
        }
        public  MainPage()
        {
            InitializeComponent();
            ApplicationView.PreferredLaunchViewSize = new Size(1024, 600);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
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
                if (ee.Size.Height >= 568)
                {
                    imageMain.Height = ee.Size.Height;
                }
                if (ee.Size.Width >= 1024)
                {
                    imageMain.Width = ee.Size.Width;
                }
            };
        }

        /// <summary>
        /// Sets up the view model.
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

        #region NavigationHelper registration

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
            if (Window.Current.Bounds.Height >= 568)
            {
                imageMain.Height = Window.Current.Bounds.Height;
            }
            if (Window.Current.Bounds.Width >= 1024)
            {
                imageMain.Width = Window.Current.Bounds.Width;
            }
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void bModel1_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SelectedModel), bModel1.Content);
        }

        private void bModel2_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SelectedModel), bModel2.Content);
        }

        private void bModel3_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SelectedModel), bModel3.Content);
        }

        private void bModel4_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SelectedModel), bModel4.Content);
        }

        private void bModel5_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SelectedModel), bModel5.Content);
        }
    }
}
