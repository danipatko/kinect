using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
// using Nefarius.ViGEm.Client;
// using Nefarius.ViGEm.Client.Targets;
// using Nefarius.ViGEm.Client.Targets.Xbox360;

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

        // private ViGEmClient client;
        // private IXbox360Controller controller;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sensor != null) sensor.Stop();
            // controller.Disconnect();
        }

        // private void SetCursorPosition(Point point)
        // {
        //     Win32.POINT p = new Win32.POINT((int)(point.X * widthRatio), (int)(point.Y * heightRatio));
        //     if(p.x == 0 && p.y == 0)
        //     {
        //         return;
        //     }
        //     Console.WriteLine($"x = {p.x}, y = {p.y}");
        //     Win32.SetCursorPos(p.x, p.y);
        // }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // create virtual device
            // client = new ViGEmClient();
            // controller = client.CreateXbox360Controller();
            // controller.Connect();

            // controller.SetButtonState(Xbox360Button.A, true);
            // controller.SetSliderValue(Xbox360Slider.RightTrigger, 255);

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

            // start image stream
            display = new Display(sensor);
            DisplayImage.Source = display.ImageSource;
            PositionText.Text = display.skeletonPosition;

            // Dispatcher.Invoke(() => {
            //     PositionText.Text = display.skeletonPosition;
            // });

            sensor.SkeletonStream.Enable();
            sensor.SkeletonFrameReady += display.OnFrameReady;
            display.DebugData += Augh;

            try
            {
                sensor.Start();
            }
            catch (IOException)
            {
                sensor = null;
            }
        }

        private void Augh(object sender, Display.DebugDataArgs e)
        {
            PositionText.Text = e.SkeletonPosition;
        }
        
    }
}
