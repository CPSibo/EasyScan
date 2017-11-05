using EasyScan.Controls;
using EasyScan.ImageProcessing;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;

namespace EasyScan.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Video preview width.
        /// </summary>
        private int videoPreviewWidth;

        /// <summary>
        /// Video preview height.
        /// </summary>
        private int videoPreviewHeight;

        private double motion;

        /// <summary>
        /// Selected video device.
        /// </summary>
        private MediaInformation selectedVideoDevice;

        /// <summary>
        /// List of media information device available.
        /// </summary>
        private IEnumerable<MediaInformation> mediaDeviceList;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Register<double>(this, UpdateMotion);

            this.MediaDeviceList = WebcamDevice.GetVideoDevices;
            this.VideoPreviewWidth = 1920;
            this.VideoPreviewHeight = 1080;
            this.SelectedVideoDevice = null;
            this.Motion = 0.0.ToString();
        }

        public void UpdateMotion(double motion)
        {
            Motion = motion.ToString();
        }

        public string Motion
        {
            get
            {
                return $"Motion: {Math.Round(this.motion, 3)}";
            }

            set
            {
                this.motion = double.Parse(value);
                this.RaisePropertyChanged(() => this.Motion);
            }
        }

        /// <summary>
        /// Gets or sets video preview width.
        /// </summary>
        public int VideoPreviewWidth
        {
            get
            {
                return this.videoPreviewWidth;
            }

            set
            {
                this.videoPreviewWidth = value;
                this.RaisePropertyChanged(() => this.VideoPreviewWidth);
            }
        }

        /// <summary>
        /// Gets or sets video preview height.
        /// </summary>
        public int VideoPreviewHeight
        {
            get
            {
                return this.videoPreviewHeight;
            }

            set
            {
                this.videoPreviewHeight = value;
                this.RaisePropertyChanged(() => this.VideoPreviewHeight);
            }
        }

        /// <summary>
        /// Gets or sets selected media video device.
        /// </summary>
        public MediaInformation SelectedVideoDevice
        {
            get
            {
                return this.selectedVideoDevice;
            }

            set
            {
                this.selectedVideoDevice = value;
                this.RaisePropertyChanged(() => this.SelectedVideoDevice);
            }
        }

        /// <summary>
        /// Gets or sets media device list.
        /// </summary>
        public IEnumerable<MediaInformation> MediaDeviceList
        {
            get
            {
                return this.mediaDeviceList;
            }

            set
            {
                this.mediaDeviceList = value;
                this.RaisePropertyChanged(() => this.MediaDeviceList);
            }
        }
    }
}