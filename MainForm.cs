using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace CameraCapture
{
    public partial class MainForm : Form
    {
        private VideoCapture capture;
        private Mat pre_frame;
        private VideoWriter videoWriter;
        private DateTime start_time;
        private int fps;
        public MainForm()
        {
            InitializeComponent();
            pre_frame = null;
            videoWriter = null;
            fps = 30;
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            capture = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
            if (!capture.IsOpened())
            {
                MessageBox.Show("打开摄像头失败");
                capture.Dispose();
                return;
            }
            backgroundWorker1.RunWorkerAsync();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
            capture?.Dispose();
            if (videoWriter is not null)
            {
                videoWriter.Release();
                videoWriter = null;
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var bgWorker = (BackgroundWorker)sender;
            while (!bgWorker.CancellationPending)
            {
                using (var frame = capture.RetrieveMat())
                {
                    var img = BitmapConverter.ToBitmap(frame);
                    bgWorker.ReportProgress(0, img);
                    DetectAction(frame);
                    GC.Collect();
                }
                Thread.Sleep(1000 / fps);
            }
        }

        private void DetectAction(Mat frame)
        {
            bool isdiff = false;
            using Mat gray_frame = new(), img_delta = new(), thresh = new();
            Cv2.CvtColor(frame, gray_frame, ColorConversionCodes.BGR2GRAY);
            Cv2.Resize(gray_frame, gray_frame, new OpenCvSharp.Size(500, 500));
            Cv2.GaussianBlur(gray_frame, gray_frame, new OpenCvSharp.Size(21, 21), 0);
            if (pre_frame is null)
            {
                pre_frame = new();
                gray_frame.CopyTo(pre_frame);
                return;
            }
            else
            {
                Cv2.Absdiff(pre_frame, gray_frame, img_delta);
                Cv2.Threshold(img_delta, thresh, 25, 255, ThresholdTypes.Binary);
                Cv2.Dilate(thresh, thresh, null, null, 2);

                Cv2.FindContours(thresh, out OpenCvSharp.Point[][] contours, out HierarchyIndex[] h,
                    RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                foreach (var c in contours)
                {
                    if (Cv2.ContourArea(c) < 1000)
                    {
                        continue;
                    }
                    else
                    {
                        if (videoWriter is null)
                        {
                            string file = string.Format("./video/{0:yyyyMMddmmssffff}", DateTime.Now) + ".avi";
                            videoWriter = new VideoWriter(file, FourCC.DIVX, fps,
                                new OpenCvSharp.Size(capture.FrameWidth, capture.FrameHeight));
                        }
                        start_time = DateTime.Now;
                        isdiff = true;
                        break;
                    }
                }
                if (videoWriter is not null)
                {
                    videoWriter.Write(frame);
                    if (!isdiff)
                    {
                        DateTime end_time = DateTime.Now;
                        var dt = end_time - start_time;
                        if (dt.TotalSeconds >= 10)
                        {
                            videoWriter.Release();
                            videoWriter = null;
                        }
                    }
                }
                gray_frame.CopyTo(pre_frame);

            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var img = (Bitmap)e.UserState;
            pbImg.Image?.Dispose();
            pbImg.Image = img;
        }
    }
}
