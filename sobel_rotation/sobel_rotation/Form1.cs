using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using ZBar;

namespace sobel_rotation
{
    public partial class Form1 : Form
    {
        OpenCvSharp.Point point;
        OpenCvSharp.Size size;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            String selected = "" + listBox1.SelectedItem;
            if(dic.ContainsKey(selected))
            {
                info obj = dic[selected];
                String filepath = obj.fname;
                Bitmap bmp = new Bitmap(filepath);
                ZBar.ImageScanner Is = new ZBar.ImageScanner();
                pictureBox1.Image = bmp; 
                List<ZBar.Symbol> Original_Result = Is.Scan(bmp);
                label23.ResetText();
                foreach (var item in Original_Result)
                {
                    
                    label23.Text = item.Data;
                }
                Mat ipl = BitmapConverter.ToMat(bmp);
                
                Mat detect_res = detect(ipl);

                List<ZBar.Symbol> rotate_result = Is.Scan(detect_res.ToBitmap());
                label25.ResetText();
                foreach (var item in rotate_result)
                {
                    
                    label25.Text = item.Data;
                }
            }
        }
        public Mat detect(Mat src)
        {
            Mat gray = new Mat();
            MatType ddepth;
            Mat gradX = new Mat();
            Mat gradY = new Mat();
            Mat Subtract_Mat = new Mat();
            Mat ConvertScaleAbs_Mat = new Mat();
            Mat Blur_Mat = new Mat();
            Mat threshold_Mat = new Mat();
            OpenCvSharp.Size sz = new OpenCvSharp.Size(21, 7);
            Mat MorphologyEx_Mat = new Mat();
            Mat Erode_Mat = new Mat();
            Mat Dilate_Mat = new Mat();
            Mat kernel = new Mat();
            Mat element = new Mat();
            Mat copy = new Mat();
           
            OpenCvSharp.Point[][] contours;

            HierarchyIndex[] hierachy_index;

            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

            ddepth = MatType.CV_8U;
            Cv2.Sobel(gray, gradX, ddepth, 1, 0, -1);
            pictureBox2.Image = gradX.ToBitmap();

            Cv2.Sobel(gray, gradY, ddepth, 0, 1, -1);
            pictureBox3.Image = gradY.ToBitmap();
            
            Cv2.Subtract(gradX, gradY, Subtract_Mat);
            pictureBox4.Image = Subtract_Mat.ToBitmap();

            Cv2.ConvertScaleAbs(Subtract_Mat, ConvertScaleAbs_Mat);
            pictureBox5.Image = ConvertScaleAbs_Mat.ToBitmap();
            point = new OpenCvSharp.Point(9, 9);
            size = ConvertScaleAbs_Mat.Size();
            OpenCvSharp.Size ksize = new OpenCvSharp.Size(9, 9);

            Cv2.Blur(ConvertScaleAbs_Mat, Blur_Mat, ksize);
            pictureBox5.Image = Blur_Mat.ToBitmap();

            Cv2.Threshold(Blur_Mat, threshold_Mat, 100, 255, ThresholdTypes.Binary);
            pictureBox6.Image = threshold_Mat.ToBitmap();
            
            kernel = Cv2.GetStructuringElement(MorphShapes.Rect, sz);
            Cv2.MorphologyEx(threshold_Mat, MorphologyEx_Mat, MorphTypes.Close, kernel);
            pictureBox7.Image = MorphologyEx_Mat.ToBitmap();

            Cv2.Erode(MorphologyEx_Mat, Erode_Mat, element, null, 4);
            pictureBox8.Image = Erode_Mat.ToBitmap();

            Cv2.Dilate(Erode_Mat, Dilate_Mat, element, null, 4);
            pictureBox9.Image = Dilate_Mat.ToBitmap();

            Dilate_Mat.CopyTo(copy);

            Cv2.FindContours(Dilate_Mat, out contours, out hierachy_index, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            for(int i = 1; i < contours.Length; i++)
            {
                if(contours[0].Length < contours[i].Length)
                {
                    contours[0] = contours[i];
                }
            }
            RotatedRect rc = Cv2.MinAreaRect(contours[0]);

            double dis1 = 0;
            OpenCvSharp.Point ptfmax1 = new OpenCvSharp.Point(0,0);
            OpenCvSharp.Point ptfmax2 = new OpenCvSharp.Point(0,0);


            Mat src_copy = new Mat();
            src.CopyTo(src_copy);

            for (int i = 0; i < rc.Points().Length; i++)
            {
                Point2f ptf1 = rc.Points()[i];
                Point2f ptf2 = rc.Points()[(i + 1) % 4];
                OpenCvSharp.Point pt1 = new OpenCvSharp.Point(ptf1.X,ptf1.Y);
                OpenCvSharp.Point pt2 = new OpenCvSharp.Point(ptf2.X, ptf2.Y);
                double dis2 = dis(ptf1, ptf2);
                if (dis1 == 0)
                {
                    dis1 = dis2;
                    ptfmax1 = pt1;
                    ptfmax2 = pt2;
                }

                if( dis2 > dis1)
                {
                    ptfmax1 = pt1;
                    ptfmax2 = pt2;
                }
                
                Cv2.Line(src, pt1, pt2, Scalar.Blue, 2, LineTypes.AntiAlias);
            }
            
            Cv2.Line(src, ptfmax1, ptfmax2, Scalar.Red, 5, LineTypes.AntiAlias);

            pictureBox10.Image = src.ToBitmap();
            Mat mrot_2 = new Mat();
            Rotate_image(src_copy, ref mrot_2, printAngle(rc));
            pictureBox12.Image = mrot_2.ToBitmap();
            return mrot_2;
        }
       
        float printAngle(RotatedRect calculatedRect)
        {
            if (calculatedRect.Size.Width < calculatedRect.Size.Height)
            {
                return calculatedRect.Angle + 90;
            }
            else
            {
                return calculatedRect.Angle;
            }
        }
        double dis(Point2f p, Point2f q)
        {
            Point2f diff = p - q;
            return Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
        }
        
        public Mat Rotate_image(Mat src, ref Mat dst, float angle)
        {
            OpenCvSharp.Size size = new OpenCvSharp.Size();
            size.Width = src.Width;
            size.Height = src.Height;

            Mat matrix = Cv2.GetRotationMatrix2D(new Point2f(src.Width / 2, src.Height / 2), angle, 1);
            Cv2.WarpAffine(src, dst, matrix, size);
            return src;
        }
        
        String basedir = @"C:\Users\ft20180723\Desktop\detect_test_picture\";
        class info
        {
            public string fname;
            public string fsize;
            public int fangle;
        }
        Dictionary<String, info> dic = new Dictionary<string, info>();

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] dd = System.IO.Directory.GetDirectories(basedir);

            foreach (var d in dd)
            {
                String[] ff = System.IO.Directory.GetFiles(d);
                foreach (var f in ff)
                {
                    string fshort = System.IO.Path.GetFileName(f);
                    info obj = new info();
                    try
                    {
                        obj.fangle = int.Parse(fshort);
                    }
                    catch
                    {
                        obj.fangle = 0;
                    }
                    obj.fname = f;
                    switch(fshort[0])
                    {
                        case 'b':
                            obj.fsize = "big";
                            break;
                        case 'g':
                            obj.fsize = "good";
                            break;
                        case 's':
                            obj.fsize = "small";
                            break;
                    }
                    dic.Add(fshort, obj);
                    listBox1.Items.Add(fshort);
                }
            }      
        }
    }
}
