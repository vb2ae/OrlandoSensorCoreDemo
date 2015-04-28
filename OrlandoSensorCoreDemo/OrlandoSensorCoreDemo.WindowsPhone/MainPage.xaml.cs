using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using Lumia.Sense;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace OrlandoSensorCoreDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
            
        }
        private StepCounter _stepCounter;


        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (_stepCounter != null)
                await CallSensorcoreApiAsync(async () =>
                    await _stepCounter.DeactivateAsync());

        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.

            if (!(await StepCounter.IsSupportedAsync()))
            {
                MessageDialog dlg = new MessageDialog("Unfortunately this device does not support SensorCore features");
                await dlg.ShowAsync();
                Application.Current.Exit();
            }
            else
            {
                MotionDataSettings settings = await SenseHelper.GetSettingsAsync();
                uint settingsVersion = settings.Version;
                tbVersion.Text="Current version number " + settingsVersion.ToString();

                        if (await CallSensorcoreApiAsync(async () =>
                        {
                            if (_stepCounter == null)
                            {
                                _stepCounter = await StepCounter.GetDefaultAsync();
                            }
                            else
                            {
                                await _stepCounter.ActivateAsync();
                            }
                        }))
                        {
                            await ShowCurrentReading();
                        }
                    }
          
        }

        private async Task<bool> CallSensorcoreApiAsync(Func<Task> action)
        {
            Exception failure = null;

            try
            {
                await action();
            }
            catch (Exception e)
            {
                failure = e;
            }

            if (failure != null)
            {
                MessageDialog dialog;

                switch (SenseHelper.GetSenseError(failure.HResult))
                {
                    case SenseError.LocationDisabled:
                        dialog = new MessageDialog("Location has been disabled. Do you want to open Location settings now?", "Information");
                        dialog.Commands.Add(new UICommand("Yes", async cmd => await
                            SenseHelper.LaunchLocationSettingsAsync()));
                        dialog.Commands.Add(new UICommand("No"));
                        await dialog.ShowAsync();
                        new System.Threading.ManualResetEvent(false).WaitOne(500);
                        return false;

                    case SenseError.SenseDisabled:
                        {
                            var settings = await SenseHelper.GetSettingsAsync();

                            if (settings.Version < 2)
                            {
                                dialog = new MessageDialog("Motion data has been disabled. Do you want to open motion data settings now?", "Information");
                            }
                            else
                            {
                                dialog = new MessageDialog("Places visited has been disabled. Do you want to open motion data settings now?", "Information");
                            }

                            dialog.Commands.Add(new UICommand("Yes", new
                                       UICommandInvokedHandler(async (cmd) => await
                                       SenseHelper.LaunchSenseSettingsAsync())));

                            dialog.Commands.Add(new UICommand("No"));
                            await dialog.ShowAsync();
                            new System.Threading.ManualResetEvent(false).WaitOne(500);
                            return false;
                        }

                    default:
                        dialog = new MessageDialog("Failure: " + SenseHelper.GetSenseError(
                                          failure.HResult), "");
                        await dialog.ShowAsync();
                        return false;
                }
            }

            return true;
        }
        private async Task ShowCurrentReading()
        {
            await CallSensorcoreApiAsync(async () =>
            {
                var reading = await _stepCounter.GetCurrentReadingAsync();
                tbMessage.Text="Current step counter reading";

                if (reading != null)
                {
                    tbWalk.Text="Walk steps = " + reading.WalkingStepCount;
                    tbWalkTime.Text="Walk time = " + reading.WalkTime.ToString();
                    tbRun.Text="Run steps = " + reading.RunningStepCount;
                    tbRunTime.Text="Run time = " + reading.RunTime.ToString();
                }
                else
                {
                    tbMessage.Text="Data not available";
                }   
            
            });
        }
    }
}
