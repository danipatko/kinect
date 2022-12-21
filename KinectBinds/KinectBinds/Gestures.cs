using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectBinds
{
    internal class Gestures
    {
        KinectSensor sensor;
        Skeleton skeleton;

        public enum HandPosition
        {
            Unknown = -1,
            Up = 0,
            Down = 1,
            Middle = 2
        }

        public Gestures(KinectSensor sensor)
        {
            // skeleton = new Skeleton();
            this.sensor = sensor;
        }

        public void OnFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    var skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                    // tracks the closest one (override using sensor.SkeletonStream.AppChoosesSkeletons = true)
                    skeleton = skeletons[0];
                }
            }

            Console.WriteLine(skeleton.TrackingState);

            CheckRightHandIdle();
            CheckRightHand();
            CheckLeftHand();
            CheckPosition();
        }

        public (float x, float y) RightHandPosition { get; set; } = (0, 0);
        private void CheckRightHand()
        {
            if (RightHandIdle)
            {
                RightHandPosition = (0, 0);
                return;
            }
            Joint hand = skeleton.Joints[JointType.HandRight], thorax = skeleton.Joints[JointType.ShoulderRight];
            RightHandPosition = (hand.Position.X - thorax.Position.X, hand.Position.Y - thorax.Position.Y);
        }

        public bool RightHandIdle { get; set; } = true;
        private const float RightHandIdleOffset = 0.25f;
        private void CheckRightHandIdle()
        {
            Joint hand = skeleton.Joints[JointType.HandRight], thorax = skeleton.Joints[JointType.ShoulderRight];
            RightHandIdle = hand.TrackingState == JointTrackingState.Tracked && Dist((hand.Position.X, hand.Position.Y), (thorax.Position.X, thorax.Position.Y)) < RightHandIdleOffset;
        }

        public double LeftHandAngle { get; set; } = double.NaN;
        private const double LeftHandOffsetDeg = 30f;
        private HandPosition leftHandPosition = HandPosition.Unknown;
        private void CheckLeftHand()
        {
            Joint hand = skeleton.Joints[JointType.HandLeft], thorax = skeleton.Joints[JointType.ShoulderLeft];
            if (hand.Position.X > thorax.Position.X)
            {
                leftHandPosition = HandPosition.Unknown;
                LeftHandAngle = double.NaN;
                return;
            }

            LeftHandAngle = Math.Atan((hand.Position.Y - thorax.Position.Y) / (thorax.Position.X - hand.Position.X)) * 180 / Math.PI;

            // down
            if(LeftHandAngle < -LeftHandOffsetDeg && leftHandPosition != HandPosition.Down)
                LeftHandMoved(new LeftHandEventArgs(leftHandPosition, leftHandPosition = HandPosition.Down));
            // up
            else if (LeftHandAngle > LeftHandOffsetDeg && leftHandPosition != HandPosition.Up)
                LeftHandMoved(new LeftHandEventArgs(leftHandPosition, leftHandPosition = HandPosition.Up));
            // middle
            else if(LeftHandAngle < LeftHandOffsetDeg && -LeftHandOffsetDeg < LeftHandAngle && leftHandPosition != HandPosition.Middle)
                LeftHandMoved(new LeftHandEventArgs(leftHandPosition, leftHandPosition = HandPosition.Middle));
        }

        public (float x, float y) BodyPosition { get; set; } = (0, 0);
        public bool BodyCentered { get; set; } = false;
        private const float BodyCenter = .6f; // the depth where the body is centered
        private const float BodyOffsetY = .1f;
        private const float BodyOffsetX = .2f;
        private void CheckPosition()
        {
            float depth = -(skeleton.Position.Z / 3f - BodyCenter);
            BodyPosition = (skeleton.Position.X, depth);
            if (-BodyOffsetX < skeleton.Position.X && skeleton.Position.X < BodyOffsetX) BodyPosition = (0, BodyPosition.y); // set x to 0
            if (-BodyOffsetY < depth && depth < BodyOffsetY) BodyPosition = (BodyPosition.x, 0); // set y to 0
        }

        private float Dist((float x, float y) a, (float x, float y) b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }

        // event stuff

        public class LeftHandEventArgs : EventArgs
        {
            public HandPosition Current { get; }
            public HandPosition Previous { get; }
            public LeftHandEventArgs(HandPosition prev, HandPosition curr)
            {
                Previous = prev;
                Current = curr;
            }
        }

        public event EventHandler<LeftHandEventArgs> OnLeftMoved;
        protected virtual void LeftHandMoved(LeftHandEventArgs e)
        {
            OnLeftMoved?.Invoke(this, e);
        }
    }
}
