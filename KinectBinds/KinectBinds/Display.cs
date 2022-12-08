﻿using System;
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

namespace KinectBinds
{
    internal class Display
    {
        private readonly KinectSensor sensor;

        private readonly DrawingGroup drawingGroup;
        public DrawingImage ImageSource { get; }

        public readonly double width = 640;
        public readonly double height = 480;

        // debug stuff to show on screen
        public string skeletonPosition = "bhkjeasf";

        private float screenWidth;
        public float ScreenWidth { 
            get => screenWidth; 
            set {
                screenWidth = value;
                widthRatio = screenWidth / (float)width;
            }
        }

        private float screenHeight;

        public float ScreenHeight
        {
            get => screenHeight;
            set
            {
                screenHeight = value;
                heightRatio = screenHeight / (float)height;
            }
        }

        public float widthRatio;
        public float heightRatio;

        private readonly Pen drawPen = new Pen(Brushes.Green, 6);

        public Display(KinectSensor sensor, int width = 2560, int height = 1440)
        {
            this.sensor = sensor;
            
            ScreenHeight = height;
            ScreenWidth = width;

            drawingGroup = new DrawingGroup();
            ImageSource = new DrawingImage(drawingGroup);
        }

        public void OnFrameReady(object sender, SkeletonFrameReadyEventArgs e)
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
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, width, height));
                if (skeletons.Length > 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            FireDebugData(new DebugDataArgs($"Position X:{skel.Position.X} Y:{skel.Position.Y} Z:{skel.Position.Z}"));

                            DrawBones(skel, dc);
                        }
                    }
                }
                drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, width, height));
            }
        }

        private void DrawBones(Skeleton skeleton, DrawingContext drawingContext)
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
            
            // ignore inferred/untracked joints
            if (joint0.TrackingState == JointTrackingState.NotTracked || joint1.TrackingState == JointTrackingState.NotTracked) return;
            if (joint0.TrackingState == JointTrackingState.Inferred && joint1.TrackingState == JointTrackingState.Inferred) return;

            drawingContext.DrawLine(drawPen, SkeletonPointToWindow(joint0.Position), SkeletonPointToWindow(joint1.Position));
        }

        public Point SkeletonPointToWindow(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        public Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            DepthImagePoint depthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X * widthRatio, depthPoint.Y * heightRatio);
        }

        public event EventHandler<DebugDataArgs> DebugData;

        public class DebugDataArgs : EventArgs
        {
            public string SkeletonPosition { get; set; }
            public DebugDataArgs(string skeletonPosition )
            {
                SkeletonPosition = skeletonPosition;
            }
        }


        protected virtual void FireDebugData(DebugDataArgs e)
        {
            DebugData?.Invoke(this, e);
        }
    }
}