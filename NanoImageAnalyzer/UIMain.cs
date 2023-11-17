using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace NanoImageAnalyzer
{
    public partial class UIMain : Form
    {
        private ScaleObject scale = null;
        private List<DrawingObject> objects = new List<DrawingObject>();
        private DrawingObject currObject = null;
        private Image image = null;
        private Bitmap drawingImage = null;
        private Pen bluePen = new Pen(Color.Blue, 3);
        private Pen yellowPen = new Pen(Color.Yellow, 3);
        private Pen scalePen = new Pen(Color.Green, 5);
        private Font font = new Font("Arial", 12);

        public UIMain()
        {
            InitializeComponent();

            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseUp += PictureBox_MouseUp;
            pictureBox.MouseMove += PictureBox_MouseMove;
        }

        private Point? GetCoords(Point p)
        {
            int realW = pictureBox.Image.Width;
            int realH = pictureBox.Image.Height;
            int currentW = pictureBox.ClientRectangle.Width;
            int currentH = pictureBox.ClientRectangle.Height;
            double zoomW = (currentW / (double)realW);
            double zoomH = (currentH / (double)realH);
            double zoomActual = Math.Min(zoomW, zoomH);
            double padX = zoomActual == zoomW ? 0 : (currentW - (zoomActual * realW)) / 2;
            double padY = zoomActual == zoomH ? 0 : (currentH - (zoomActual * realH)) / 2;

            int realX = (int)((p.X - padX) / zoomActual);
            int realY = (int)((p.Y - padY) / zoomActual);

            if (realX < 0 || realX > realW)
                return null;

            if (realY < 0 || realY > realH)
                return null;

            return new Point(realX, realY);
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (image == null)
                return;

            Point? p = GetCoords(e.Location);
            if (p == null)
                return;

            lblCoords.Text = $"({p.Value.X}, {p.Value.Y})";

            if (e.Button == MouseButtons.Left)
            {
                if (currObject == null)
                    return;

                currObject.B = p.Value;
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (rbScale.Checked)
                {
                    scale.B = p.Value;
                }
            }

            RefreshImage();
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (image == null)
                return;

            Point? p = GetCoords(e.Location);
            if (p == null)
                return;

            if (e.Button == MouseButtons.Left)
            {
                if (currObject == null)
                    return;

                currObject.B = p.Value;
                objects.Add(currObject);
                currObject = null;
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (rbUndo.Checked && objects.Count > 0)
                {
                    objects.RemoveAt(objects.Count - 1);
                }
                else if (rbScale.Checked)
                {
                    scale.B = p.Value;
                }
            }

            RefreshImage();
            RefreshData();
        }

        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (image == null)
                return;

            Point? p = GetCoords(e.Location);
            if (p == null)
                return;

            if (e.Button == MouseButtons.Left)
            {
                if (scale == null)
                    return;

                currObject = new DrawingObject()
                {
                    Scale = scale.Scale()
                };

                if (rbLine.Checked)
                    currObject.Type = DrawingType.Line;
                else if (rbCircle.Checked)
                    currObject.Type = DrawingType.Circle;

                currObject.A = p.Value;
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (rbScale.Checked)
                {
                    scale = new ScaleObject()
                    {
                        A = p.Value,
                        PhysicalLength = (double)numScale.Value
                    };
                }
            }

            RefreshImage();
        }

        private void btnLoadImage_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                lblImageName.Text = Path.GetFileName(openFileDialog.FileName);
                image = Image.FromFile(openFileDialog.FileName);
                RefreshImage();
            }
        }

        private void RefreshImage()
        {
            if (image == null)
                return;

            drawingImage = new Bitmap(image.Width, image.Height);
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

            using (Graphics g = Graphics.FromImage(drawingImage))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawImage(image, rect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);

                for (int i = 0; i < objects.Count; i++)
                {
                    DrawingObject obj = objects[i];

                    if (obj.B.X == 0 && obj.B.Y == 0)
                        continue;

                    if (obj.Type == DrawingType.Circle)
                    {
                        int radius = (int)obj.Radius();
                        Point midpoint = obj.Midpoint();
                        g.DrawEllipse(bluePen, midpoint.X - radius, midpoint.Y - radius, radius * 2, radius * 2);
                        g.DrawString(i.ToString(), font, Brushes.Blue, midpoint);
                    }
                    else if (obj.Type == DrawingType.Line)
                    {
                        g.DrawLine(bluePen, obj.A, obj.B);
                        g.DrawString(i.ToString(), font, Brushes.Blue, obj.A);
                    }
                }

                if (currObject != null && !(currObject.B.X == 0 && currObject.B.Y == 0))
                {
                    if (currObject.Type == DrawingType.Circle)
                    {
                        int radius = (int)currObject.Radius();
                        g.DrawEllipse(yellowPen, currObject.Midpoint().X - radius, currObject.Midpoint().Y - radius, radius * 2, radius * 2);
                    }
                    else if (currObject.Type == DrawingType.Line)
                    {
                        g.DrawLine(yellowPen, currObject.A, currObject.B);
                    }
                }

                if (scale != null && !(scale.B.X == 0 && scale.B.Y == 0))
                {
                    g.DrawLine(scalePen, scale.A, scale.B);
                }
            }

            pictureBox.Image = drawingImage;
            GC.Collect();
        }

        private void RefreshData()
        {
            treeView.Nodes.Clear();
            TreeNode scaleNode = treeView.Nodes.Add("Scale");
            TreeNode lineNode = treeView.Nodes.Add("Lines");
            TreeNode circleNode = treeView.Nodes.Add("Circles");

            for (int i = 0; i < objects.Count; i++)
            {
                DrawingObject obj = objects[i];
                TreeNode temp = null;

                if (obj.Type == DrawingType.Circle)
                {
                    temp = circleNode.Nodes.Add($"{i}: {obj.Length()} units");
                }
                else if (obj.Type == DrawingType.Line)
                {
                    temp = lineNode.Nodes.Add($"{i}: {obj.Length()} units");
                }

                if (temp != null)
                    temp.Tag = obj.Length();
            }

            for (int i = 1; i < lineNode.Nodes.Count; i += 2)
            {
                double l1 = (double)lineNode.Nodes[i - 1].Tag;
                double l2 = (double)lineNode.Nodes[i].Tag;
                lineNode.Nodes[i].Nodes.Add($"Aspect: {Math.Min(l1, l2) / Math.Max(l1, l2)}");
            }

            if (scale != null)
                scaleNode.Nodes.Add($"{scale.Scale()} units/px");

            treeView.ExpandAll();
        }

        private void btnCopy_ButtonClick(object sender, EventArgs e)
        {
            var treeViewStringBuilder = new StringBuilder();
            GetTreeViewNodesText(treeView.Nodes, treeViewStringBuilder);
            Clipboard.SetText(treeViewStringBuilder.ToString().Replace(":", "").Replace(" ", "\t"));
        }

        private void GetTreeViewNodesText(TreeNodeCollection nodesInCurrentLevel, StringBuilder sb)
        {
            foreach (TreeNode currentNode in nodesInCurrentLevel)
            {
                sb.AppendLine(currentNode.Text);
                GetTreeViewNodesText(currentNode.Nodes, sb);
            }
        }
    }
}
