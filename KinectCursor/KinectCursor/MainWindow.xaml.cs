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

namespace KinectCursor
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

        private DrawingGroup drawingGroup;
        private DrawingImage imageSource;

        private readonly double width = 640;
        private readonly double height = 480;

        private readonly float screenWidth = 2560;
        private readonly float screenHeight = 1440;

        private float widthRatio;
        private float heightRatio;

        private readonly Pen drawPen = new Pen(Brushes.Green, 6);

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sensor != null) sensor.Stop();
        }

        private void SetCursorPosition(Point point)
        {
             Win32.POINT p = new Win32.POINT((int)(point.X * widthRatio), (int)(point.Y * heightRatio));
            // Win32.POINT p = new Win32.POINT((int)point.X, (int)point.Y);
            if(p.x == 0 && p.y == 0)
            {
                return;
            }
            Console.WriteLine($"x = {p.x}, y = {p.y}");
            Win32.SetCursorPos(p.x, p.y);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayImage.Width = width;
            DisplayImage.Height = height;

            widthRatio = screenWidth / (float)width;
            heightRatio = screenHeight / (float)height;

            MessageBox.Show($"Width: {widthRatio} | height: {heightRatio}");

            drawingGroup = new DrawingGroup();
            imageSource = new DrawingImage(drawingGroup);
            DisplayImage.Source = imageSource;
 
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

            sensor.SkeletonStream.Enable();
            sensor.SkeletonFrameReady += OnFrameReady;

            try
            {
                sensor.Start();
            }
            catch (IOException)
            {
                sensor = null;
            }
        }

        private void OnFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = drawingGroup.Open())
            {
                // base background
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, width, height));

                if (skeletons.Length > 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        if (skel.TrackingState == SkeletonTrackingState.Tracked) DrawBonesAndJoints(skel, dc);
                        SetCursorPosition(SkeletonPointToScreen(skel.Joints[JointType.HandRight].Position));
                    }
                }

                // prevent drawing outside of our render area
                drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, width, height));
            }
        }

        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            Console.WriteLine(skeleton.Joints[JointType.HandRight].Position);

            // Left Leg
            DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);
        }
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked || joint1.TrackingState == JointTrackingState.NotTracked) return;

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred && joint1.TrackingState == JointTrackingState.Inferred) return;

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            drawingContext.DrawLine(drawPen, SkeletonPointToScreen(joint0.Position), SkeletonPointToScreen(joint1.Position));
        }

        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }
    }
}
