﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using AForge.Video.DirectShow;

using Drawing = System.Drawing;
using Point = System.Windows.Point;
using System.Windows.Data;
using MoreLinq;
using System.IO;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Vision.Motion;
using EasyScan.ImageProcessing;

namespace EasyScan.Controls
{
    /// <summary>
    /// Interaction logic for web-cam device XAML.
    /// </summary>
    public partial class WebcamDevice
    {
        #region Variable declaration

        /// <summary>
        /// Dependency property for video display width.
        /// </summary>
        public static readonly DependencyProperty VideoPreviewWidthProperty = DependencyProperty.Register("VideoPreviewWidth", typeof(double), typeof(WebcamDevice), 
            new PropertyMetadata(VideoPreviewWidthPropertyChangedCallback));

        /// <summary>
        /// Dependency property for video display height.
        /// </summary>
        public static readonly DependencyProperty VideoPreviewHeightProperty = DependencyProperty.Register("VideoPreviewHeight", typeof(double), typeof(WebcamDevice), 
            new PropertyMetadata(VideoPreviewHeightPropertyChangedCallback));

        /// <summary>
        /// Dependency property for video device source Id.
        /// </summary>
        public static readonly DependencyProperty VideoSourceIdProperty = DependencyProperty.Register("VideoSourceId", typeof(string), typeof(WebcamDevice), 
            new PropertyMetadata(string.Empty, VideoSourceIdPropertyChangedCallback, VideoSourceIdPropertyCoherceValueChanged));

        /// <summary>
        /// Instance of video capture device.
        /// </summary>
        private VideoCaptureDevice videoCaptureDevice;

        /// <summary>
        /// The is video source initialized.
        /// </summary>
        private bool isVideoSourceInitialized;

        private FrameProcessor frameProcessor;

        #endregion

        #region Constructor

        public WebcamDevice()
        {
            this.InitializeComponent();

            //// Subcribe to dispatcher shutdown event and dispose all used resources gracefully.
            this.Dispatcher.ShutdownStarted += this.DispatcherShutdownStarted;


            this.frameProcessor = new FrameProcessor();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets video device source collection current available.
        /// </summary>
        public static IEnumerable<MediaInformation> GetVideoDevices
        {
            get
            {
                var filterVideoDeviceCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                return (from FilterInfo filterInfo in filterVideoDeviceCollection select new MediaInformation
                {
                    DisplayName = filterInfo.Name,
                    UsbId = filterInfo.MonikerString
                }).ToList();
            }
        }

        /// <summary>
        /// Gets or sets video source device display width.
        /// </summary>
        /// <remarks>
        /// Important: Do not write any logic in dependency property stub.
        /// </remarks>
        [TypeConverter(typeof(LengthConverter))]
        public double VideoPreviewWidth
        {
            get
            {
                return (double)GetValue(VideoPreviewWidthProperty);
            }

            set
            {
                this.SetValue(VideoPreviewWidthProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets video source device display height.
        /// </summary>
        /// <remarks>
        /// Important: Do not write any logic in dependency property stub.
        /// </remarks>
        [TypeConverter(typeof(LengthConverter))]
        public double VideoPreviewHeight
        {
            get
            {
                return (double)GetValue(VideoPreviewHeightProperty);
            }

            set
            {
                this.SetValue(VideoPreviewHeightProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets video device source Id / USB ID / Moniker string.
        /// </summary>
        /// <remarks>
        /// Note: If the Id start with "Message:" followed by a message,
        /// the following message will be display instead of the video source device.
        /// Important: Do not write any logic in dependency property stub.
        /// </remarks>
        public string VideoSourceId
        {
            get
            {
                return (string)GetValue(VideoSourceIdProperty);
            }

            set
            {
                this.SetValue(VideoSourceIdProperty, value);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Call back function for video device source Id property changed event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="eventArgs">Event argument.</param>
        private static void VideoSourceIdPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs)
        {
            var oldValue = eventArgs.OldValue as string;
            var newValue = eventArgs.NewValue as string;
            var webCamDevice = sender as WebcamDevice;
            if (null == webCamDevice)
            {
                return;
            }

            if (null == eventArgs.NewValue)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(newValue))
            {
                if (!string.IsNullOrWhiteSpace(oldValue))
                {
                    webCamDevice.InitializeVideoDevice(oldValue);
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(oldValue))
                {
                    webCamDevice.InitializeVideoDevice(newValue);
                }
                else
                {
                    if (oldValue != newValue)
                    {
                        webCamDevice.isVideoSourceInitialized = false;
                    }

                    webCamDevice.InitializeVideoDevice(oldValue.Equals(newValue) ? oldValue : newValue);
                }
            }
        }

        /// <summary>
        /// Event handler for video device Id value changed event.
        /// </summary>
        /// <param name="dependencyObject">Instance of dependency object.</param>
        /// <param name="basevalue">Base value.</param>
        /// <returns>Return base value / NULL or the new Id value of the video device source.</returns>
        private static object VideoSourceIdPropertyCoherceValueChanged(DependencyObject dependencyObject, object basevalue)
        {
            var baseValueStringFormat = Convert.ToString(basevalue, CultureInfo.InvariantCulture);
            var availableMediaList = GetVideoDevices;
            var mediaInfos = availableMediaList as IList<MediaInformation> ?? availableMediaList.ToList();
            if (string.IsNullOrEmpty(baseValueStringFormat) || !mediaInfos.Any())
            {
                return null;
            }

            var filteredVideoDevice = mediaInfos.FirstOrDefault(item => item.UsbId.Equals(baseValueStringFormat));
            return null != filteredVideoDevice ? filteredVideoDevice.UsbId : baseValueStringFormat;
        }

        /// <summary>
        /// Call back function for source video device preview width property changed event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="eventArgs">Event argument.</param>
        private static void VideoPreviewWidthPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs)
        {
            var webCamDevice = sender as WebcamDevice;
            if (null == webCamDevice)
            {
                return;
            }

            if (null == eventArgs.NewValue)
            {
                return;
            }

            var newValue = (double)eventArgs.NewValue;
            if (double.IsNaN(newValue))
            {
                var parentControl = (webCamDevice.VisualParent as Grid);
                webCamDevice.SetVideoPlayerWidth(null != parentControl ? parentControl.Width : newValue);
            }
            else
            {
                webCamDevice.SetVideoPlayerWidth(newValue);
            }
        }

        /// <summary>
        /// Call back function for source video device preview height property changed event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="eventArgs">Event argument.</param>
        private static void VideoPreviewHeightPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs)
        {
            var webCamDevice = sender as WebcamDevice;
            if (null == webCamDevice)
            {
                return;
            }

            if (null == eventArgs.NewValue)
            {
                return;
            }

            var newValue = (double)eventArgs.NewValue;
            if (double.IsNaN(newValue))
            {
                var parentControl = (webCamDevice.VisualParent as Grid);
                webCamDevice.SetVideoPlayerHeight(null != parentControl ? parentControl.Height : newValue);
            }
            else
            {
                webCamDevice.SetVideoPlayerHeight(newValue);
            }
        }

        /// <summary>
        /// Initialize video device.
        /// </summary>
        /// <param name="videoDeviceSourceId">Video device source Id.</param>
        /// <exception cref="InvalidOperationException">Throws invalid operation exception if video device source setup fails.</exception>
        private void InitializeVideoDevice(string videoDeviceSourceId)
        {
            if (this.isVideoSourceInitialized)
            {
                return;
            }

            var errorAction = new Action(() => this.SetVideoPlayer(false, "Unable to set video device source"));
            this.ReleaseVideoDevice();
            if (string.IsNullOrEmpty(videoDeviceSourceId))
            {
                return;
            }

            if (videoDeviceSourceId.StartsWith("Message:", StringComparison.OrdinalIgnoreCase))
            {
                var splitString = videoDeviceSourceId.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (splitString.Length == 2)
                {
                    this.SetVideoPlayer(false, splitString[1]);
                }
                else
                {
                    this.SetVideoPlayer(false);
                }
            }
            else
            {
                try
                {
                    if (!GetVideoDevices.Any(item => item.UsbId.Equals(videoDeviceSourceId)))
                    {
                        return;
                    }

                    this.videoCaptureDevice = new VideoCaptureDevice(videoDeviceSourceId);
                    videoCaptureDevice.NewFrame += NewFrameHandler;

                    var setting = this.videoCaptureDevice.VideoCapabilities.MaxBy(f => f.FrameSize.Height);
                    videoCaptureDevice.VideoResolution = setting;
                    videoCaptureDevice.SnapshotResolution = setting;

                    var ratio = (double)setting.FrameSize.Width / (double)setting.FrameSize.Height;
                    VideoPreviewHeight = 550;
                    VideoPreviewWidth = (double)VideoPreviewHeight * ratio;

                    videoCaptureDevice.Start();

                    //this.VideoSourcePlayer.VideoSource = this.videoCaptureDevice;
                    //this.VideoSourcePlayer.Start();
                    this.isVideoSourceInitialized = true;
                    this.SetVideoPlayer(true);
                }
                catch (ArgumentNullException)
                {
                    errorAction();
                }
                catch (ArgumentException)
                {
                    errorAction();
                }
            }
        }

        private void NewFrameHandler(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            if(frameProcessor.IsProcessing)
            {
                return;
            }

            var newFrame = frameProcessor.ProcessFrame(eventArgs.Frame);
            ImageBox.Image = newFrame;
            ImageBox.Width = newFrame.Width;
            ImageBox.Height = newFrame.Height;
        }

        /// <summary>
        /// Set video player width.
        /// </summary>
        /// <param name="newWidth">New width value.</param>
        private void SetVideoPlayerWidth(double newWidth)
        {
            this.NoVideoSourceGrid.Width = newWidth;
            this.VideoSourceWindowsFormsHost.Width = newWidth;
        }

        /// <summary>
        /// Set video player height.
        /// </summary>
        /// <param name="newHeight">New height value.</param>
        private void SetVideoPlayerHeight(double newHeight)
        {
            this.NoVideoSourceGrid.Height = newHeight;
            this.VideoSourceWindowsFormsHost.Height = newHeight;
        }

        /// <summary>
        /// Event handler for camera video device on loaded event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="eventArgs">Event argument.</param>
        private void WebcamDeviceOnLoaded(object sender, RoutedEventArgs eventArgs)
        {
            //// Set controls width / height based on VideoPreviewWidth / VideoPreviewHeight binding properties.
            this.NoVideoSourceGrid.Width = this.VideoPreviewWidth;
            this.VideoSourceWindowsFormsHost.Width = this.VideoPreviewWidth;
            this.NoVideoSourceGrid.Height = this.VideoPreviewHeight;
            this.VideoSourceWindowsFormsHost.Height = this.VideoPreviewHeight;
            this.InitializeVideoDevice(this.VideoSourceId);
        }

        /// <summary>
        ///  Disconnect video source device.
        /// </summary>
        private void ReleaseVideoDevice()
        {
            this.isVideoSourceInitialized = false;
            this.SetVideoPlayer(false);
            if (null == this.videoCaptureDevice)
            {
                return;
            }

            this.videoCaptureDevice.SignalToStop();
            this.videoCaptureDevice.WaitForStop();
            this.videoCaptureDevice.Stop();
            this.videoCaptureDevice = null;
        }

        /// <summary>
        /// Set video source player visibility.
        /// </summary>
        /// <param name="isVideoSourceFound">Indicates a value weather video source device found or not.</param>
        /// <param name="noVideoSourceMessage">Message to display when no video source found, optional will use empty string.</param>
        private void SetVideoPlayer(bool isVideoSourceFound, string noVideoSourceMessage = "")
        {
            //// If video source found is true show the video source player or else show no video source message.
            if (isVideoSourceFound)
            {
                this.VideoSourceWindowsFormsHost.Visibility = Visibility.Visible;
                this.NoVideoSourceGrid.Visibility = Visibility.Hidden;
                this.NoVideoSourceMessage.Text = string.Empty;
            }
            else
            {
                this.VideoSourceWindowsFormsHost.Visibility = Visibility.Hidden;
                this.NoVideoSourceGrid.Visibility = Visibility.Visible;
                this.NoVideoSourceMessage.Text = string.IsNullOrWhiteSpace(noVideoSourceMessage) ? "No video source device found" : noVideoSourceMessage;
            }
        }

        /// <summary>
        /// Event handler for dispatcher shutdown started event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private void DispatcherShutdownStarted(object sender, EventArgs eventArgs)
        {
            this.ReleaseVideoDevice();
        }

        /// <summary>
        /// Event handler for camera video device on unloaded event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private void WebcamDeviceOnUnloaded(object sender, RoutedEventArgs eventArgs)
        {
            this.ReleaseVideoDevice();
        }

        #endregion
    }

    /// <summary>
    /// Represents class that contains media information for video device source.
    /// </summary>
    public sealed class MediaInformation
    {
        /// <summary>
        /// Gets or sets the display name of the video device source.
        /// </summary>
        public string DisplayName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the USB Id / Moniker string of the video device source.
        /// </summary>
        public string UsbId
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Media information converter.
    /// </summary>
    public class MediaInformationConverter : IValueConverter
    {
        /// <summary>
        /// Convert media information instance to USB ID or USB moniker string.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="parameter">Parameter for conversion.</param>
        /// <param name="culture">Culture information.</param>
        /// <returns>Return USB ID or moniker string for the input media information instance.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = string.Empty;
            if (null == value)
            {
                return result;
            }

            var filterInfo = value as MediaInformation;
            if (null != filterInfo)
            {
                result = filterInfo.UsbId;
            }

            return result;
        }

        /// <summary>
        /// Convert back the input value for media information.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>The <see cref="object"/>.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
