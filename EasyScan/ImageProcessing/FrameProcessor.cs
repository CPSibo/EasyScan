using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Vision.Motion;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyScan.ImageProcessing
{
    public class FrameProcessor
    {
        protected Bitmap LatestFrame { get; set; }

        protected int Page { get; set; } = 1;

        protected bool PageTurned { get; set; } = false;

        protected int FramesWithMotion { get; set; } = 0;
        
        protected MotionDetector Detector { get; set; } = new MotionDetector(
            new SimpleBackgroundModelingDetector(true, true),
            new MotionAreaHighlighting(Color.Green));

        public double MotionThisFrame { get; set; }

        public void ProcessFrame(Bitmap frame)
        {
            if(!Settings.AutoSave)
            {
                return;
            }

            LatestFrame = new Bitmap(frame);
            MotionThisFrame = Detector.ProcessFrame(frame);
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<double>(MotionThisFrame, "motion");

            // process new video frame and check motion level
            if (PageTurned)
            {
                if (MotionThisFrame < Settings.MotionFloor && Page < 50)
                {
                    Directory.CreateDirectory(Settings.ProjectPath);

                    LatestFrame.Save(Path.Combine(Settings.ProjectPath, $"Pages {Page++}-{Page++}.png"), System.Drawing.Imaging.ImageFormat.Png);

                    PageTurned = false;

                    FramesWithMotion = 0;
                }
            }
            else
            {
                if (MotionThisFrame >= Settings.MotionCeil)
                {
                    FramesWithMotion++;
                }
                else
                {
                    FramesWithMotion = 0;
                }

                if (FramesWithMotion >= Settings.MinimumFramesForPageTurn)
                {
                    PageTurned = true;
                }
            }

            GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<int>(FramesWithMotion, "frames with motion");
        }

        public void SetBackground()
        {
            ((CustomFrameDifferenceDetector)Detector.MotionDetectionAlgorithm).SetBackgroundFrame(LatestFrame);
        }
    }
}
