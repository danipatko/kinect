using System;
using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.Kinect;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace KinectBinds
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private KinectSensor sensor;
        private Display display;
        private Gestures gestures;

        private ViGEmClient client;
        private IXbox360Controller controller;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sensor != null) sensor.Stop();
            controller.Disconnect();
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Thread.Sleep(5000);
            // create virtual device
            client = new ViGEmClient();
            controller = client.CreateXbox360Controller();
            controller.Connect();

            // find a sensor
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    sensor = potentialSensor;
                    break;
                }
            }
            if (sensor == null) throw new Exception("Unable to find kinect sensor!");

            gestures = new Gestures(sensor);
            gestures.OnLeftMoved += HandleLeftMoved;

            // start image stream
            display = new Display(sensor);
            DisplayImage.Source = display.ImageSource;
            PositionText.Text = display.skeletonPosition;
    
            sensor.SkeletonStream.Enable();
            sensor.SkeletonStream.EnableTrackingInNearRange = true;

            sensor.SkeletonFrameReady += gestures.OnFrameReady;
            sensor.SkeletonFrameReady += display.OnFrameReady;
            sensor.SkeletonFrameReady += Log;

            try
            {
                sensor.Start();
            }
            catch (IOException)
            {
                sensor = null;
            }
        }

        private void HandleLeftMoved(object sender, Gestures.LeftHandEventArgs e)
        {
            // shoot
            if(e.Current == Gestures.HandPosition.Up)
                controller.SetSliderValue(Xbox360Slider.RightTrigger, byte.MaxValue);
            // stop crouching
            else if (e.Current == Gestures.HandPosition.Down)
                controller.SetButtonState(Xbox360Button.B, false);
            else if (e.Current == Gestures.HandPosition.Middle)
            {
                // stop shooting
                if (e.Previous == Gestures.HandPosition.Up)
                    controller.SetSliderValue(Xbox360Slider.RightTrigger, 0);
                // crouch
                else
                    controller.SetButtonState(Xbox360Button.B, true);
            }
        }

        private void Log(object sender, SkeletonFrameReadyEventArgs _)
        {
            PositionText.Text = gestures.BodyPosition.ToString();
            controller.SetAxisValue(Xbox360Axis.RightThumbX, (short)(gestures.RightHandPosition.x * Int16.MaxValue));
            controller.SetAxisValue(Xbox360Axis.RightThumbY, (short)(gestures.RightHandPosition.y * Int16.MaxValue));
            controller.SetAxisValue(Xbox360Axis.LeftThumbX, (short)(gestures.BodyPosition.x * 2 * Int16.MaxValue));
            controller.SetAxisValue(Xbox360Axis.LeftThumbY, (short)(gestures.BodyPosition.y * 2 * Int16.MaxValue));
        }
    }
}
