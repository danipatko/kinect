using System;
using System.IO;
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

            gestures = new Gestures();
            gestures.OnLeftMoved += HandleLeftMoved;

            // start image stream
            display = new Display(sensor);
            DisplayImage.Source = display.ImageSource;
    
            sensor.SkeletonStream.Enable();
            sensor.SkeletonStream.EnableTrackingInNearRange = true;

            sensor.SkeletonFrameReady += gestures.OnFrameReady;
            sensor.SkeletonFrameReady += UpdatePositionAndRotation;
            sensor.SkeletonFrameReady += display.OnFrameReady;

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
            if(e.Current == Gestures.HandPosition.Up) controller.SetSliderValue(Xbox360Slider.RightTrigger, byte.MaxValue);
            else controller.SetSliderValue(Xbox360Slider.RightTrigger, 0);
            
            // crouch
            if (e.Current == Gestures.HandPosition.Middle || e.Current == Gestures.HandPosition.Up) controller.SetButtonState(Xbox360Button.B, true);
            else controller.SetButtonState(Xbox360Button.B, false);
        }

        private const float MoveSensX = 2, MoveSensY = 5;
        private const float LookSensX = 1.2f, LookSensY = 1.25f;

        private void UpdatePositionAndRotation(object sender, SkeletonFrameReadyEventArgs _)
        {
            PositionText.Text = $"body: {gestures.BodyPosition}\nright hand: {(gestures.RightHandIdle ? "idle" : "tracked")} {gestures.RightHandPosition}\nleft hand: {gestures.LeftHandAngle}";
            
            controller.SetAxisValue(Xbox360Axis.RightThumbX, Convert(gestures.RightHandPosition.x * LookSensX));
            controller.SetAxisValue(Xbox360Axis.RightThumbY, Convert(gestures.RightHandPosition.y * LookSensY));
            controller.SetAxisValue(Xbox360Axis.LeftThumbX, Convert(gestures.BodyPosition.x * MoveSensX));
            controller.SetAxisValue(Xbox360Axis.LeftThumbY, Convert(gestures.BodyPosition.y * MoveSensY));
        }

        private short Convert(float val)
        {
            if (val > 1) return short.MaxValue;
            else if (val < -1) return short.MinValue;
            else return (short)(val * short.MaxValue);
        }
    }
}
