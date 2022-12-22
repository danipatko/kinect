using System;
using Microsoft.Kinect;

namespace KinectBinds
{
    internal class Gestures
    {
        // OFFSETS
        // max distance between right hand and right shoulder
        private const float RightHandIdleOffset = 0.25f;
        // angle of the body and left hand (up -> offset < angle | down -> -offset > angle | else middle)
        private const double LeftHandOffsetDeg = 30;
        // the depth where the body is centered
        private const float BodyCenter = .6f;
        // one step distance forward
        private const float BodyOffsetY = .1f;
        // one step distance sideways
        private const float BodyOffsetX = .2f;

        Skeleton skeleton;

        public enum HandPosition
        {
            Unknown = -1,
            Up = 0,
            Down = 1,
            Middle = 2
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

            skeleton = new Skeleton();
            foreach (Skeleton s in skeletons)
            {
                if (s.TrackingState == SkeletonTrackingState.Tracked)
                {
                    skeleton = s;
                    break;
                }
            }

            if(skeleton.TrackingState != SkeletonTrackingState.Tracked)
            {
                // default values
                RightHandIdle = true;
                RightHandPosition = (0, 0);
                LeftHandAngle = double.NaN;
                leftHandPosition = HandPosition.Unknown;
                IsBodyCentered = true;
                BodyPosition = (0, 0);
                return;
            }

            CheckRightHand();
            CheckLeftHand();
            CheckPosition();
        }

        public (float x, float y) RightHandPosition { get; set; } = (0, 0);
        public bool RightHandIdle { get; set; } = true;
        private void CheckRightHand()
        {
            Joint hand = skeleton.Joints[JointType.HandRight], thorax = skeleton.Joints[JointType.ShoulderRight];
            if (Dist((hand.Position.X, hand.Position.Y), (thorax.Position.X, thorax.Position.Y)) < RightHandIdleOffset)
            {
                RightHandIdle = true;
                RightHandPosition = (0, 0);
                return;
            }
            RightHandIdle = false;
            RightHandPosition = (hand.Position.X - thorax.Position.X, hand.Position.Y - thorax.Position.Y);
        }

        public double LeftHandAngle { get; set; } = double.NaN;
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
        public bool IsBodyCentered { get; set; } = false;
        private void CheckPosition()
        {
            if (skeleton.TrackingState == SkeletonTrackingState.NotTracked)
            {
                BodyPosition = (0, 0);
                return;
            }

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
