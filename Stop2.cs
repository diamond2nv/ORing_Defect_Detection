﻿using System;
using System.IO;
using System.Collections.Generic;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Threading;
using System.Linq;

namespace Stop2_New
{
    class Program
    {
        public static int stop2_out_defect_inner_cicle_size_min = 100;
        public static int OK_Count = 0;
        public static int NG_Count = 0;
        public static int stop2_inner_circle_radius = 0;
        public static int stop2_out_defect_size_min = 200;
        public static int stop2_out_defect_size_max = 20000;
        public static int stop2_inner_defect_size_min = 20;
        public static int stop2_arclength_area_ratio = 100;

        static void Main(string[] args)
        {

            //讀圖
            string[] filenamelist = Directory.GetFiles(@".\images\", "*.jpg", SearchOption.AllDirectories);
            //string[] filenamelist = Directory.GetFiles(@".\images\", "4.jpg", SearchOption.AllDirectories);
            if (Directory.Exists("result\\NG"))
            {
                Directory.Delete("result\\NG", true);
                Directory.Delete("result\\OK", true);
                Directory.CreateDirectory("result\\NG");
                Directory.CreateDirectory("result\\OK");
            }
            else
            {
                Directory.CreateDirectory("result\\NG");
                Directory.CreateDirectory("result\\OK");
            }
            //debug
            int fileindex = 0;

            foreach (string filename in filenamelist)
            {
                fileindex++;
                Mat src = Cv2.ImRead(filename, ImreadModes.Grayscale);

                Console.WriteLine(filename);

                Stop2_Detector(src, filename.Substring(9));

                //================================
            }

            Console.WriteLine(OK_Count);
            Console.WriteLine(NG_Count);

            Console.ReadLine();
        }
        static void Stop2_Detector(Mat Src, string filename)
        {

            //========================================================================================

            Int64 OK_NG_flag = 0;
            Mat vis_rgb = Src.CvtColor(ColorConversionCodes.GRAY2RGB);
            //Console.WriteLine(vis_rgb.Size()+"  "+vis_rgb.Channels());

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            //=======================================================================================

            List<OpenCvSharp.Point[]> contours_final = Mask_innercicle(ref Src);
            //Src.SaveImage("./enhance/" + filename);
            Mat Src_copy = Mat.Zeros(Src.Size(), MatType.CV_8UC1);
            Src.CopyTo(Src_copy);

            //================outer defect====================================
            //Find outer defect return 應該要畫的區域
            int nLabels_outer = 0;//number of labels
            int[,] stats_outer = null;
            FindContour_and_outer_defect(Src, contours_final, ref nLabels_outer, out stats_outer, "outer");


            int nLabels_inner = 0;//number of labels
            int[,] stats_inner = null;
            FindContour_and_outer_defect(Src, contours_final, ref nLabels_inner, out stats_inner, "inner");

            //var ellipsecontour = Cv2.FitEllipse(contours_final[0]);
            //Cv2.Ellipse(vis_rgb, ellipsecontour, Scalar.Red, 2);
            

            List<Point[][]> canny_defect = canny_test(Src, contours_final, filename);
            foreach (Point[][] temp in canny_defect)
            {

                Cv2.DrawContours(vis_rgb, temp, -1, Scalar.Red, 3);
                OK_NG_flag = 1;
            }

            /*
            //====================Adaptive threshold inner defect==============================================
            List<Point[][]> Apaptive_Defect = AdaptiveThreshold_Based_Extract_Defect(Src, contours_final);
            */
            /*
            //====================MSER=========================================================================
            Mat img_MSER = Mat.Zeros(Src.Size(), MatType.CV_8UC1);
            Src.CopyTo(img_MSER);
            OpenCvSharp.Point offset_bounding_rec;

            MSER_Preprocessing(ref img_MSER, out offset_bounding_rec, contours_final);
            //img_MSER.SaveImage("./mser_proprecessing/" + filename);
            //6 0.9
            List<Point[][]> MSER_stop2 = My_MSER(7, stop2_inner_defect_size_min, 20000, 1.2, img_MSER, ref vis_rgb, 0);

            //Src_copy.SaveImage("./enhance/" + filename);

            foreach (Point[][] temp in MSER_stop2)
            {
                Cv2.DrawContours(vis_rgb, temp, -1, Scalar.Red, 3, offset: offset_bounding_rec);
                OK_NG_flag = 1;
            }*/
            /*
            foreach (Point[][] temp in Apaptive_Defect)
            {
                Cv2.DrawContours(vis_rgb, temp, -1, Scalar.Blue, 3);
                OK_NG_flag = 1;
            }*/

            for (int i = 0; i < nLabels_inner; i++)
            {

                int area = stats_inner[i, 4];

                if (area < 200000 && area < stop2_out_defect_size_max && area > stop2_out_defect_size_min)
                {
                    //Console.WriteLine(area);
                    vis_rgb.Rectangle(new Rect(stats_inner[i, 0], stats_inner[i, 1], stats_inner[i, 2], stats_inner[i, 3]), Scalar.Green, 3);
                    OK_NG_flag = 1;
                }
            }
            
            //outer  毛邊
            for (int i = 0; i < nLabels_outer; i++)
            {

                int area = stats_outer[i, 4];

                if (area < 200000 && area < stop2_out_defect_size_max && area > 200)
                {
                    //Console.WriteLine(area);
                    vis_rgb.Rectangle(new Rect(stats_outer[i, 0], stats_outer[i, 1], stats_outer[i, 2], stats_outer[i, 3]), Scalar.Green, 3);
                    OK_NG_flag = 1;
                }
            }
            

            Console.WriteLine(OK_NG_flag == 1 ? "NG" : "OK");

            //Src.SaveImage("./result/test" + fileindex + ".jpg");
            if (OK_NG_flag == 1)
            {
                vis_rgb.SaveImage("./result/NG/test" + filename);
                NG_Count++;
            }
            else
            {
                vis_rgb.SaveImage("./result/OK/test" + filename);
                OK_Count++;
            }
            //==================================================================
            
            watch.Stop();
            //印出時間
            Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");


        }

        static List<Point[][]> canny_test(Mat Src, List<OpenCvSharp.Point[]> contours_final, string fileindex)
        {
            Mat Canny_Src = Mat.Zeros(Src.Size(), MatType.CV_8UC1);
            //用adaptive threshold 濾出瑕疵
            Cv2.Blur(Src, Canny_Src, new OpenCvSharp.Size(5, 7));

            Cv2.Canny(Canny_Src, Canny_Src, 90, 0);
            
            
            Mat kernel = Mat.Ones(5, 5, MatType.CV_8UC1);//改變凹角大小
            Canny_Src = Canny_Src.MorphologyEx(MorphTypes.Close, kernel);
            
            OpenCvSharp.Point[][] temp = new Point[1][];
            
            temp[0] = contours_final[0];
            Cv2.DrawContours(Canny_Src, temp, -1, 0, 3);
            temp[0] = contours_final[1];
            Cv2.DrawContours(Canny_Src, temp, -1, 0, 3);
            
            
            Canny_Src.SaveImage("./result/canny/test" + fileindex);

            // denoise
            Point[][] contours;
            HierarchyIndex[] hierarchly;
            Cv2.FindContours(Canny_Src, out contours, out hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            List<OpenCvSharp.Point[][]> final_area = new List<OpenCvSharp.Point[][]>();
            Mat img_temp = Mat.Zeros(Canny_Src.Size(), MatType.CV_8UC1);
            foreach (OpenCvSharp.Point[] contour_now in contours)
            {

                //用bounding rec 濾出white noise
                RotatedRect BoundingRectangle = Cv2.MinAreaRect(contour_now);
                if(BoundingRectangle.Size.Height * BoundingRectangle.Size.Width > 500)
                {
                    OpenCvSharp.Point[][] temp_final = new Point[1][];//記得放在裡面宣告
                    //Console.WriteLine("Arc Length: " + (Cv2.ArcLength(contour_now, true) + " Area: " + Cv2.ContourArea(contour_now))+" Length/Area:" +(Cv2.ArcLength(contour_now, true) / Cv2.ContourArea(contour_now)));
                    OpenCvSharp.Point[] approx = Cv2.ApproxPolyDP(contour_now, 0.000, true);
                    temp_final[0] = approx;
                    Cv2.DrawContours(img_temp, temp_final, -1, 255, -1);
                    final_area.Add(temp_final);
                }

            }


            //img_temp.SaveImage("./result/canny/test" + fileindex);
            return final_area;

        }

        static List<Point[]> Mask_innercicle(ref Mat img)
        {
            Mat img_gaussian = Mat.Zeros(img.Size(), MatType.CV_8UC1);
            Cv2.GaussianBlur(img, img_gaussian, new OpenCvSharp.Size(21, 21), 0, 0);

            Mat thresh1 = img_gaussian.Threshold(180, 255, ThresholdTypes.Binary);

            thresh1.SaveImage("threshold.jpg");

            Point[][] contours;
            HierarchyIndex[] hierarchly;
            Cv2.FindContours(thresh1, out contours, out hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            // find final circle 
            List<Point[]> contours_final = new List<Point[]>();

            foreach (OpenCvSharp.Point[] contour_now in contours)
            {
                if (Cv2.ContourArea(contour_now) > 1000000 && Cv2.ContourArea(contour_now) < 2500000)
                {
                    contours_final.Add(contour_now);
                }

            }
            ///OpenCvSharp.Point[][] temp = new Point[1][];//for draw on image

            Point[] contours_approx_innercircle;
            var contour_innercircle = contours_final[1];
            //temp[0] = contour_now;

            Point2f center;
            float radius;
            //Cv2.DrawContours(vis_rgb, temp, -1, Scalar.Green, thickness: -1);
            contours_approx_innercircle = Cv2.ApproxPolyDP(contour_innercircle, 0.001, true);//speedup
            Cv2.MinEnclosingCircle(contours_approx_innercircle, out center, out radius);
            //Cv2.Circle(img, (Point)center, (int)radius+ stop1_inner_circle_radius, 255, thickness: -1);
            //Cv2.Circle(vis_rgb, (Point)center, (int)radius, Scalar.White, thickness: -1);

            //==================================================outer contour - inner contour=====================================
            // variable
            OpenCvSharp.Point[][] temp = new Point[1][];


            // inner contour
            Mat inner_contour_img = Mat.Zeros(img.Size(), MatType.CV_8UC1);
            //Point[] inner_contour = Cv2.ConvexHull(contours_final[1]);
            temp[0] = contours_final[1];
            //Cv2.DrawContours(inner_contour_img, temp, -1, 255, -1);
            Cv2.DrawContours(inner_contour_img, temp, -1, 255, -1);
            //Cv2.Circle(inner_contour_img, (Point)center, (int)radius + stop1_inner_circle_radius, 255, thickness: -1);

            // outer contour
            Mat outer_contour_img = Mat.Zeros(img.Size(), MatType.CV_8UC1);
            Mat outer_contour_img2 = new Mat(img.Size(), MatType.CV_8UC1, new Scalar(255));//initilize Mat with the value 255

            temp[0] = contours_final[0];
            Cv2.DrawContours(outer_contour_img, temp, -1, 255, -1);
            //outer contour2 in order to make mask area = 255
            Cv2.DrawContours(outer_contour_img2, temp, -1, 0, -1);
            //outer - inner
            Mat diff_mask = outer_contour_img - inner_contour_img;
            Mat diff_mask2 = inner_contour_img + outer_contour_img2;

            Mat image = Mat.Zeros(img.Size(), MatType.CV_8UC1);
            img.CopyTo(image, diff_mask);
            //in order to make mask area = 255
            img = image + diff_mask2;


            return contours_final;
        }
        //======================adaptivebased=========================
        static List<Point[][]> AdaptiveThreshold_Based_Extract_Defect(Mat Src, List<OpenCvSharp.Point[]> contours_final)
        {
            //=========prepare adaptive threshold input
            Mat Adaptive_Src = Mat.Zeros(Src.Size(), MatType.CV_8UC1);
            //用adaptive threshold 濾出瑕疵
            Cv2.GaussianBlur(Src, Adaptive_Src, new OpenCvSharp.Size(3, 3), 0, 0);

            Cv2.AdaptiveThreshold(Adaptive_Src, Adaptive_Src, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 45, 105 / 10);

            //讓黑白相反(not opetation)
            Mat Src_255 = new Mat(Adaptive_Src.Size(), MatType.CV_8UC1, new Scalar(255));
            Cv2.Subtract(Src_255, Adaptive_Src, Adaptive_Src);


            // denoise
            Point[][] contours;
            HierarchyIndex[] hierarchly;
            Cv2.FindContours(Adaptive_Src, out contours, out hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
            OpenCvSharp.Point[][] temp = new Point[1][];

            foreach (OpenCvSharp.Point[] contour_now in contours)
            {
                if (Cv2.ContourArea(contour_now) < 100)
                {
                    //Console.WriteLine("Arc Length: " + (Cv2.ArcLength(contour_now, true) + " Area: " + Cv2.ContourArea(contour_now))+" Length/Area:" +(Cv2.ArcLength(contour_now, true) / Cv2.ContourArea(contour_now)));
                    OpenCvSharp.Point[] approx = Cv2.ApproxPolyDP(contour_now, 0.000, true);
                    temp[0] = approx;
                    Cv2.DrawContours(Adaptive_Src, temp, -1, 0, -1);

                }

            }

            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(13, 7));
            Adaptive_Src = Adaptive_Src.MorphologyEx(MorphTypes.Close, kernel);

            //=========================吃掉邊界=======================================

            temp[0] = contours_final[0];
            Cv2.DrawContours(Adaptive_Src, temp, -1, 0, 30);
            temp[0] = contours_final[1];
            Cv2.DrawContours(Adaptive_Src, temp, -1, 0, 30);

            //Adaptive_Src.SaveImage("a.jpg");
            //上面已經得到defect圖，用Find_Defect_Contour_and_Extract萃取出來
            return Find_Defect_Contour_and_Extract(Src, Adaptive_Src, contours_final);
        }
        static List<Point[][]> Find_Defect_Contour_and_Extract(Mat Original_image, Mat Src, List<OpenCvSharp.Point[]> contours_final)
        {
            Mat vis_rgb = Original_image.CvtColor(ColorConversionCodes.GRAY2RGB);
            List<OpenCvSharp.Point[][]> final_area = new List<OpenCvSharp.Point[][]>();
            //==============================找到圓心=======================
            Point2f center;
            float radius;
            Cv2.MinEnclosingCircle(contours_final[0], out center, out radius);

            //=============================================================

            Point[][] contours;
            HierarchyIndex[] hierarchly;
            Cv2.FindContours(Src, out contours, out hierarchly, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);


            // Extract defect candidate
            foreach (OpenCvSharp.Point[] contour_now in contours)
            {
                if (Cv2.ContourArea(contour_now) > 150)
                {
                    //Console.WriteLine(Cv2.ContourArea(contour_now));
                    OpenCvSharp.Point[][] temp = new Point[1][];
                    OpenCvSharp.Point[] approx = Cv2.ApproxPolyDP(contour_now, 0.000, true);
                    temp[0] = approx;
                    /*
                    Cv2.DrawContours(defect_image, temp, -1, 255, -1);
                    defect_image.SaveImage("./contour/" + filename);
                    */

            // find the distance between contour and center  如果不是白色的瑕疵，而且輪廓和圓心的距離滿足條件
            if ((!Whitenoise(Original_image, contour_now)) && Distance_between_contour_and_center(center, approx))//(!Whitenoise(Original_image, contour_now)) &&
                    {
                        final_area.Add(temp);
                    }



                }

            }
            /*
            if (OK_NG_flag == 0)
                vis_rgb.SaveImage("./OK/" + filename);
            else
                vis_rgb.SaveImage("./NG/" + filename);
            */
            return final_area;
        }
        static bool Whitenoise(Mat Src, OpenCvSharp.Point[] contour)
        {
            OpenCvSharp.Point[][] temp = new Point[1][];
            Mat now_defect_image = Mat.Zeros(Src.Size(), MatType.CV_8UC1);

            OpenCvSharp.Point[] approx = Cv2.ApproxPolyDP(contour, 0.000, true);
            temp[0] = approx;
            Cv2.DrawContours(now_defect_image, temp, -1, 255, -1);

            //畫出外包矩形
            RotatedRect BoundingRectangle = Cv2.MinAreaRect(approx);
            Mat mask_image = Mat.Zeros(Src.Size(), MatType.CV_8UC1);
            Cv2.Ellipse(mask_image, BoundingRectangle, 255, -1, LineTypes.AntiAlias);
            //Console.WriteLine(BoundingRectangle.Size.Height* BoundingRectangle.Size.Width);

            //面積太大 一定不是white noise
            if (BoundingRectangle.Size.Height * BoundingRectangle.Size.Width > 700)
            {
                return false;
            }
            double mean_in_area = 0, min_in_area = 0, max_in_area = 0;
            mean_in_area = Src.Mean(mask_image)[0];

            Src.MinMaxLoc(out min_in_area, out max_in_area, out _, out _, mask_image);
            //Console.WriteLine("mean: " + mean_in_area + " min: "+ min_in_area + " max: " + max_in_area);
            //mask_image.SaveImage("./contour2.jpg");

            if (mean_in_area > 130)
                return true;
            else
                return false;
        }
        static bool Distance_between_contour_and_center(OpenCvSharp.Point2f center, OpenCvSharp.Point[] contour)
        {
            double diff = 0;
            bool glass_flag = false;
            List<double> diff_list = new List<double>();
            List<int> x_list = new List<int>();
            List<int> y_list = new List<int>();
            int x1 = (int)center.X;
            int y1 = (int)center.Y;
            foreach (OpenCvSharp.Point contour_point in contour)
            {
                int x2 = contour_point.X;
                int y2 = contour_point.Y;
                x_list.Add(x2);
                y_list.Add(y2);
                diff_list.Add(Math.Sqrt(Math.Pow((x1 - x2), 2) + Math.Pow((y1 - y2), 2)));
                //Console.WriteLine(Math.Sqrt(Math.Pow((x1 - x2), 2) + Math.Pow((y1 - y2), 2)));
            }

            int x_max = x_list.Max();
            int x_min = x_list.Min();
            int y_max = y_list.Max();
            int y_min = y_list.Min();
            //Console.WriteLine("x_max " + x_max + " x_min " + x_min + " y_max " + y_max + " y_min " + y_min);
            // 玻璃上的裂縫

            if (((y_max < 710 && y_max > 630) && (y_min < 710 && y_min > 630)))
            {
                glass_flag = true;
            }
            //Console.WriteLine("\nMax: " + diff_list.Max());
            //Console.WriteLine("Min: " + diff_list.Min());
            diff = diff_list.Max() - diff_list.Min();
            //Console.WriteLine(diff);
            //Console.WriteLine(diff_list.Max());

            //RotatedRect rotateRect = Cv2.MinAreaRect(contour);
            //Console.WriteLine(rotateRect.Size.Width + " "+ rotateRect.Size.Height);
            return !(glass_flag) && (diff > 10 || diff_list.Max() > 700);


        }
        //============================================================

        //======================outer defect==========================
        static void FindContour_and_outer_defect(Mat img, List<Point[]> contours_final, ref int nLabels, out int[,] stats, string mode)
        {
            // variable
            OpenCvSharp.Point[][] temp = new Point[1][];
            
            //0: 內圈 ; 1: 外圈
            OpenCvSharp.Point[] contour_now;
            int out_defect_size_min = 0;
            if (mode == "inner")
            {
                contour_now = contours_final[0];
            }
            else
            {
                contour_now = contours_final[1];
            }

            // Convex hull


            var ellipsecontour = Cv2.FitEllipse(contour_now);

            Mat convex_mask_img = Mat.Zeros(img.Size(), MatType.CV_8UC1);
            Cv2.Ellipse(convex_mask_img, ellipsecontour, 255, -1);


            // Contour
            temp[0] = contour_now;
            Mat contour_mask_img = Mat.Zeros(img.Size(), MatType.CV_8UC1);
            Cv2.DrawContours(contour_mask_img, temp, -1, 255, -1);


            Mat diff_image = contour_mask_img ^ convex_mask_img;


            //Opening
            Mat kernel = Mat.Ones(5, 5, MatType.CV_8UC1);//改變凹角大小
            diff_image = diff_image.MorphologyEx(MorphTypes.Open, kernel);


            //=========================吃掉邊界=======================================
            //temp[0] = contour_now;
            //Cv2.DrawContours(diff_image, temp, -1, 0, 4);
            //================================================================
            convex_mask_img.SaveImage("./" + mode + "convex" + ".jpg");
            contour_mask_img.SaveImage("./" + mode + "contour" + ".jpg");
            diff_image.SaveImage("./" + mode + "mask" + ".jpg");
            //Connected Component
            var labelMat = new MatOfInt();
            var statsMat = new MatOfInt();// Row: number of labels Column: 5
            var centroidsMat = new MatOfDouble();
            nLabels = Cv2.ConnectedComponentsWithStats(diff_image, labelMat, statsMat, centroidsMat);

            var labels = labelMat.ToRectangularArray();
            stats = statsMat.ToRectangularArray();
            var centroids = centroidsMat.ToRectangularArray();




        }
        //============================================================

        //=========================MSER based====================================
        static Mat[] set_shift_image(ref Mat img)
        {
            float[,,] data = new float[4, 2, 3] {   { { 1,0,15},    { 0,1,-15}  },
                                                {   { 1,0,15},    { 0,1,15}   },
                                                {   { 1,0,-15},   { 0,1,-15}  },
                                                {   { 1,0,-15},   { 0,1,15}   }
                                            };

            Mat[] out_image = new Mat[4];
            for (int i = 0; i < 4; i++)
            {
                out_image[i] = new Mat(2, 3, MatType.CV_32F);
                out_image[i].Set(0, 0, data[i, 0, 0]);
                out_image[i].Set(0, 1, data[i, 0, 1]);
                out_image[i].Set(0, 2, data[i, 0, 2]);
                out_image[i].Set(1, 0, data[i, 1, 0]);
                out_image[i].Set(1, 1, data[i, 1, 1]);
                out_image[i].Set(1, 2, data[i, 1, 2]);
                /*
                Console.WriteLine(out_image[i].At<float>(0, 0));
                Console.WriteLine(out_image[i].At<float>(0, 1));
                Console.WriteLine(out_image[i].At<float>(0, 2));
                Console.WriteLine(out_image[i].At<float>(1, 0));
                Console.WriteLine(out_image[i].At<float>(1, 1));
                Console.WriteLine(out_image[i].At<float>(1, 2));
                */

            }

            return out_image;

        }
        static List<Point[][]> My_MSER(int my_delta, int my_minArea, int my_maxArea, double my_maxVariation, Mat img, ref Mat img_rgb, int big_flag)
        {
            //img.SaveImage("img_detected.jpg");

            List<Point[][]> final_area = new List<Point[][]>();
            Point[][] contours;
            Rect[] bboxes;
            MSER mser = MSER.Create(delta: my_delta, minArea: my_minArea, maxArea: my_maxArea, maxVariation: my_maxVariation);
            mser.DetectRegions(img, out contours, out bboxes);

            //====================================Local Majority Vote

            // to speed up, create four shift image first
            var shift_mat = set_shift_image(ref img);
            Mat[] neighbor_img = new Mat[4];
            for (int i = 0; i < 4; i++)
            {
                neighbor_img[i] = new Mat();
                var imageCenter = new Point2f(img.Cols / 2f, img.Rows / 2f);
                var rotationMat = Cv2.GetRotationMatrix2D(imageCenter, 100, 1.3);
                Cv2.WarpAffine(img, neighbor_img[i], shift_mat[i], img.Size());
                //neighbor_img[i].SaveImage("./shift_image" + i + ".jpg");
            }

            //for each contour, apply local majority vote
            foreach (Point[] now_contour in contours)
            {

                OpenCvSharp.Point[][] temp = new Point[1][];

                Point[] Convex_hull = Cv2.ConvexHull(now_contour);
                Point[] Approx = Cv2.ApproxPolyDP(now_contour, 0.5, true);

                RotatedRect rotateRect = Cv2.MinAreaRect(Approx);
                //Debug
                //Console.WriteLine(Cv2.ContourArea(Approx)+" "+ rotateRect.Size.Height / rotateRect.Size.Width+ " "+rotateRect.Size.Width / rotateRect.Size.Height);

                if (Cv2.ContourArea(Approx) > 10000 || (Cv2.ContourArea(Approx) < stop2_inner_defect_size_min || ((rotateRect.Size.Height / rotateRect.Size.Width)) > stop2_arclength_area_ratio || ((rotateRect.Size.Width / rotateRect.Size.Height)) > stop2_arclength_area_ratio))
                    continue;

                //======================intensity in the area
                temp[0] = Approx;
                double mean_in_area_temp = 0, min_in_area_temp = 0;
                Mat mask_img_temp = Mat.Zeros(img.Size(), MatType.CV_8UC1);
                Cv2.DrawContours(mask_img_temp, temp, -1, 255, thickness: -1);//notice the difference between temp = Approx and Convex_hull
                mean_in_area_temp = img.Mean(mask_img_temp)[0];
                img.MinMaxLoc(out min_in_area_temp, out _, out _, out _, mask_img_temp);
                //Console.WriteLine(min_in_area_temp + " " + mean_in_area_temp);
                if (min_in_area_temp > 100 || mean_in_area_temp > 130)
                    continue;

                // Convex hull
                temp[0] = Approx;
                if (big_flag == 0)//small area: local majority vote
                {

                    //Cv2.Polylines(img_rgb, temp, true, new Scalar(0, 0, 255), 1);
                    //inside the area
                    double mean_in_area = 0, min_in_area = 0;
                    Mat mask_img = Mat.Zeros(img.Size(), MatType.CV_8UC1);
                    Cv2.DrawContours(mask_img, temp, -1, 255, thickness: -1);//notice the difference between temp = Approx and Convex_hull
                    mean_in_area = img.Mean(mask_img)[0];
                    img.MinMaxLoc(out min_in_area, out _, out _, out _, mask_img);

                    //Console.WriteLine(min_in_area + " " + mean_in_area);

                    //test 
                    /*
                    Mat mask2 = img.LessThan(230);
                    for (int i = 0; i < img.Cols; i++) {
                        for (int j = 0; j < img.Rows; j++) 
                            if(mask2.At<bool>(i, j)==false)
                                Console.Write(mask2.At<bool>(i,j)+ " ");

                        Console.Write("\n");

                    }
                    */
                    //neighbor
                    double[] mean_neighbor = { 255, 255, 255, 255 };
                    double[] min_neighbor = { 255, 255, 255, 255 };
                    for (int i = 0; i < 4; i++)
                    {
                        //先把 img > 230 的變成 0，再餵進 shift 裡面
                        //先把 mask 乘上另一個mask(>230的mask)
                        //Mat mask_neighbor_img = neighbor_img[i].GreaterThan(0);
                        //Console.WriteLine(mask_neighbor_img.At<int>(0,1));
                        // create final mask
                        Mat mask2 = neighbor_img[i].LessThan(225).ToMat();
                        mask2.ConvertTo(mask2, MatType.CV_8U, 1.0 / 255.0);

                        Mat mask_final = Mat.Zeros(img.Size(), MatType.CV_8UC1);
                        mask_img.CopyTo(mask_final, mask2);

                        //mask_final.SaveImage("./mask" + i + ".jpg");

                        mean_neighbor[i] = neighbor_img[i].Mean(mask_final)[0];
                        //compute min:
                        //neighbor_img[i].MinMaxLoc(out min_neighbor[i], out _, out _, out _, mask_img);
                        //Console.WriteLine(min_neighbor[i] + " " + mean_neighbor[i]);

                    }
                    int vote = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (mean_in_area > mean_neighbor[i])
                            vote++;
                    }
                    if (vote > 2 || min_in_area > 100 || mean_in_area > 130)
                    {
                        //Debug
                        //Console.WriteLine(vote + " " + min_in_area + " ", min_in_area);
                        continue;
                    }
                    else
                        //Cv2.Polylines(img_rgb, temp, true, new Scalar(0, 0, 255), 1);
                        //Console.WriteLine("--");
                        final_area.Add(temp);

                }
                else
                {
                    //Console.WriteLine("--");
                    //Cv2.Polylines(img_rgb, temp, true, new Scalar(0, 0, 255), 1);
                    final_area.Add(temp);
                }
            }
            //Console.WriteLine(final_area.Count);
            return final_area;

        }
        static void MSER_Preprocessing(ref Mat img, out OpenCvSharp.Point offset_bounding_rec, List<OpenCvSharp.Point[]> contours_final)
        {
            OpenCvSharp.Point[][] temp = new Point[1][];

            Cv2.GaussianBlur(img, img, new OpenCvSharp.Size(7, 7), 0, 0);

            /*
            //忽略內圈和外圈一些面積
            OpenCvSharp.Point[][] temp = new Point[1][];
            temp[0] = contours_final[0];
            Cv2.DrawContours(img_copy, temp, -1, 255, 40);
            temp[0] = contours_final[1];
            Cv2.DrawContours(img_copy, temp, -1, 255, 40);
            */

            //忽略外圈一些面積
            temp[0] = contours_final[1];
            Cv2.DrawContours(img, temp, -1, 255, 100);
            //temp[0] = contours_final[0];
            //Cv2.DrawContours(img, temp, -1, 255, 20);

            //200原因:外圈預留空間
            var biggestContourRect = Cv2.BoundingRect(contours_final[0]);
            var expand_rect = new Rect(biggestContourRect.TopLeft.X - 200, biggestContourRect.TopLeft.Y - 200, biggestContourRect.Width + 200, biggestContourRect.Height + 200);
            img = new Mat(img, expand_rect);
            offset_bounding_rec = expand_rect.TopLeft;




        }
        //============================================================
    }
}