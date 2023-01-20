using Emgu.CV.Structure;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using Emgu.CV.Dnn;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using System.CodeDom;
using Emgu.CV.UI;
using System.Diagnostics;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Office2019.Excel.RichData2;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Emgu.CV.CvEnum;

namespace Signtex
{
    public partial class menu : Form
    {

        string prototxt = ".\\files\\pose_deploy.prototxt";
        string modelpath = ".\\files\\pose_iter_102000.caffemodel";

        public menu()
        {

            InitializeComponent();

        }

        private void proccessToolStripMenuItem_Click(object sender, EventArgs e)
        {
          
            try
            {
                //dialog.Filter = "Im files (*.jpg;*.png;*.jpeg;*.bmp;) | *.jpg;*.png;*.jpeg;*.bmp; | All Files (*.*) | *.*;";
                OpenFileDialog dialog = new OpenFileDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var frame = new Image<Bgr, byte>(dialog.FileName);
                    var img = frame.Clone().SmoothGaussian(3);
                    var blob = DnnInvoke.BlobFromImage(img, 1.0 / 255.0, new Size(368, 368), new MCvScalar(0, 0, 0));
                    var net = DnnInvoke.ReadNetFromCaffe(prototxt, modelpath);


                    net.SetInput(blob);
                    net.SetPreferableBackend(Emgu.CV.Dnn.Backend.OpenCV);

                    var output = net.Forward();

                    var H = output.SizeOfDimension[2];
                    var W = output.SizeOfDimension[3];

                    var probMap = output.GetData();

                    int nPoints = 22;
                    int[,] POSE_PAIRS = new int[,] { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 4 }, { 0, 5 }, { 5, 6 }, { 6, 7 },
                    { 7, 8 }, { 0, 9 }, { 9, 10 }, { 10, 11 }, { 11, 12 }, { 0, 13 }, { 13, 14 }, { 14, 15 }, { 15, 16 },
                    { 0, 17 }, { 17, 18 }, { 18, 19 }, { 19, 20 } };

                    var points = new List<Point>();

                    for (int i = 0; i < nPoints; i++)
                    {
                        Matrix<float> matrix = new Matrix<float>(H, W);
                        for (int row = 0; row < H; row++)
                        {
                            for (int col = 0; col < W; col++)
                            {
                                matrix[row, col] = (float)probMap.GetValue(0, i, row, col);
                            }
                        }


                        double minVal = 0, maxVal = 0;
                        Point minLoc = default, maxLoc = default;
                        CvInvoke.MinMaxLoc(matrix, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                        var x = (img.Width * maxLoc.X) / W;
                        var y = (img.Height * maxLoc.Y) / H;

                        var p = new Point(x, y);
                        points.Add(p);
                        CvInvoke.Circle(img, p, 5, new MCvScalar(0, 255, 0), -1);
                        CvInvoke.PutText(img, i.ToString(), p, FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
                    }
                    //draw lines

                    for (int i = 0; i < POSE_PAIRS.GetLongLength(0); i++)
                    {
                        var start_index = POSE_PAIRS[i, 0];
                        var end_index = POSE_PAIRS[i, 1];
                        if (points.Contains(points[start_index]) && points.Contains(points[end_index]))
                        {
                            CvInvoke.Line(img, points[start_index], points[end_index], new MCvScalar(0, 0, 255), 2);
                        }


                    }

                    pictureBox1.Image = img.ToBitmap();
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
    }
}
