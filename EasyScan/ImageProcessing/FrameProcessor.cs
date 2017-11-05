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
            new SimpleBackgroundModelingDetector(true, true));//,
            //new MotionAreaHighlighting(Color.Green));

        public double MotionThisFrame { get; set; }

        public bool IsProcessing { get; protected set; } = false;

        protected Bitmap ApplyFilters(Bitmap frame)
        {
            frame = Grayscale.CommonAlgorithms.Y.Apply(frame);
            //new Sharpen() { Threshold = 20, Divisor = 1 }.ApplyInPlace(frame)
            //new ConservativeSmoothing(7).ApplyInPlace(frame)
            new BradleyLocalThresholding() {  }.ApplyInPlace(frame);
            //new GammaCorrection(0.9).ApplyInPlace(frame);
            //new BrightnessCorrection().ApplyInPlace(frame);
            //new HistogramEqualization().ApplyInPlace(frame);
            //new Threshold(120).ApplyInPlace(frame);
            //new IterativeThreshold(0, 0).ApplyInPlace(frame);
            //new FillHoles()
            //{
            //    MaxHoleHeight = 5,
            //    MaxHoleWidth = 5,
            //    CoupledSizeFiltering = true,
            //}.ApplyInPlace(frame);

            return frame;
        }

        public Bitmap ProcessFrame(Bitmap frame)
        {
            if(!Settings.AutoSave)
            {
                Detector.MotionDetectionAlgorithm.Reset();
                return frame;
            }

            IsProcessing = true;

            LatestFrame = new Bitmap(frame);
            
            MotionThisFrame = Detector.ProcessFrame(frame);
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<double>(MotionThisFrame, "motion");

            //frame.RotateFlip(RotateFlipType.Rotate180FlipNone);

            frame = ApplyFilters(frame);

            // process new video frame and check motion level
            if (PageTurned)
            {
                if (MotionThisFrame < Settings.MotionFloor && Page < 50)
                {
                    //Directory.CreateDirectory(Settings.ProjectPath);

                    //LatestFrame.Save(Path.Combine(Settings.ProjectPath, $"Pages {Page++}-{Page++}.png"), System.Drawing.Imaging.ImageFormat.Png);

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

            IsProcessing = false;

            return frame;
        }

        public void SetBackground()
        {
            ((CustomFrameDifferenceDetector)Detector.MotionDetectionAlgorithm).SetBackgroundFrame(LatestFrame);
        }
    }
}
