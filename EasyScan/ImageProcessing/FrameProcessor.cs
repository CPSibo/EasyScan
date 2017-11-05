using AForge.Imaging;
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

        protected double LowMotionThreshold { get; set; } = 0.02;

        protected double HighMotionThreshold { get; set; } = 0.1;

        protected int FramesThreshold { get; set; } = 15;
        
        protected MotionDetector Detector { get; set; } = new MotionDetector(
            new SimpleBackgroundModelingDetector(true, true),
            new MotionAreaHighlighting(Color.Green));

        public double MotionThisFrame { get; set; }

        public void ProcessFrame(Bitmap frame)
        {
            LatestFrame = new Bitmap(frame);
            MotionThisFrame = Detector.ProcessFrame(frame);

            // process new video frame and check motion level
            if (PageTurned)
            {
                if (MotionThisFrame < LowMotionThreshold && Page < 50)
                {
                    Directory.CreateDirectory(Settings.ProjectPath);

                    LatestFrame.Save(Path.Combine(Settings.ProjectPath, $"Pages {Page++}-{Page++}.png"), System.Drawing.Imaging.ImageFormat.Png);

                    PageTurned = false;
                }
            }
            else
            {
                if (MotionThisFrame >= HighMotionThreshold)
                {
                    FramesWithMotion++;
                }
                else
                {
                    FramesWithMotion = 0;
                }

                if (FramesWithMotion >= FramesThreshold)
                {
                    PageTurned = true;
                }
            }
        }

        public void SetBackground()
        {
            ((CustomFrameDifferenceDetector)Detector.MotionDetectionAlgorithm).SetBackgroundFrame(LatestFrame);
        }
    }
}
