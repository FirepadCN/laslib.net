﻿using LasLibNet;
using LasLibNet.Model;
using LasLibNet.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Media3D;

namespace LasLib.net.Test
{
    public partial class FormMain : Form
    {
        string lasFile = "";

        LasReader lasReader = new LasReader();
        LasHeader lasHeader;

        bool isCompressed = true;

        public FormMain()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Open a las file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton1_Click(object sender, EventArgs e)
        {

            if (this.openFileDialog.ShowDialog() == DialogResult.OK)
            {
                this.tslFile.Text = this.openFileDialog.FileName;
                this.lasFile = this.tslFile.Text;
                if (lasReader.OpenReader(this.lasFile, ref this.isCompressed) == false)
                {
                    MessageBox.Show(lasReader.Error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                this.toolStripButton2_Click(null, null);
            }
        }

        /// <summary>
        /// Display info.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            lasHeader = this.lasReader.Header;

            #region 创建数据行

            this.dgvInfo.Rows.Add("Version", lasHeader.version_major + "." + lasHeader.version_minor);
            this.dgvInfo.Rows.Add("Header Size", lasHeader.header_size.ToString());
            this.dgvInfo.Rows.Add("Point Count", lasHeader.number_of_point_records.ToString());
            this.dgvInfo.Rows.Add("Point data format", lasHeader.point_data_format.ToString());
            this.dgvInfo.Rows.Add("X Max", lasHeader.max_x.ToString("f4"));
            this.dgvInfo.Rows.Add("X Min", lasHeader.min_x.ToString("f4"));
            this.dgvInfo.Rows.Add("Y Max", lasHeader.max_y.ToString("f4"));
            this.dgvInfo.Rows.Add("Y Min", lasHeader.min_y.ToString("f4"));
            #endregion

            Tools.DisplayClassProperties<LasHeader>(lasHeader);
        }

        /// <summary>
        /// Display all points
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton3_Click_1(object sender, EventArgs e)
        {
            this.tslMain.Text = " Reading data...";
            this.statusStrip1.Refresh();
            this.Cursor = Cursors.WaitCursor;

            #region Create DataTable
            DataTable dt = new DataTable();
            DataColumn colId = new DataColumn("colId", typeof(string));
            dt.Columns.Add(colId);
            DataColumn colX = new DataColumn("colX", typeof(string));
            dt.Columns.Add(colX);
            DataColumn colY = new DataColumn("colY", typeof(string));
            dt.Columns.Add(colY);
            DataColumn colZ = new DataColumn("colZ", typeof(string));
            dt.Columns.Add(colZ);
            DataColumn colI = new DataColumn("colI", typeof(string));
            dt.Columns.Add(colI);
            DataColumn colR = new DataColumn("R", typeof(string));
            dt.Columns.Add(colR);
            DataColumn colB = new DataColumn("colB", typeof(string));
            dt.Columns.Add(colB);
            DataColumn colG = new DataColumn("colG", typeof(string));
            dt.Columns.Add(colG);
            #endregion

            #region Add Data Row
            // Go to the first point
            lasReader.SeekPoint(0);
            if (this.dgvData.DataSource != null)
            {
                DataTable dt1 = (DataTable)this.dgvData.DataSource;
                if (dt1 != null)
                    dt1.Clear();
            }
            // Loop through number of points indicated
            for (int pointIndex = 0; pointIndex < this.lasHeader.number_of_point_records; pointIndex++)
            {
                // Read the point
                LasPoint_1_2_Format2 p =(LasPoint_1_2_Format2) lasReader.ReadPoint();
                if (p == null)
                {
                    MessageBox.Show(lasReader.Error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                }
                DataRow row = dt.NewRow();
                row[colX] = p.GeoX.ToString("f2");
                row[colY] = p.GeoY.ToString("f2");
                row[colZ] = p.GeoZ.ToString("f2");
                row[colI] = p.intensity.ToString();
                row[colR] = p.Red.ToString();
                row[colG] = p.Green.ToString();
                row[colB] = p.Blue.ToString();
                dt.Rows.Add(row);

            }
            #endregion

            this.dgvData.DataSource = dt;
            this.Cursor = Cursors.Default;
            this.tslMain.Text = "Data read successfully!";


        }

        /// <summary>
        /// Close the reader
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            this.lasReader.CloseReader();
            //this.dgvData.Rows.Clear();
            DataTable dt = (DataTable)this.dgvData.DataSource;
            if (dt != null)
                dt.Clear();
            this.dgvInfo.Rows.Clear();
            this.tslFile.Text = "No file opened!";
        }

        /// <summary>
        /// Create a new las file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsbCreateLas_Click(object sender, EventArgs e)
        {
            if (this.saveFileDialog.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show("You hav't choose a las file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            string FileName = this.saveFileDialog.FileName;

            // --- Write Example
            var point = new Point3D();
            var points = new List<Point3D>();

            point.X = 1000.0;
            point.Y = 2000.0;
            point.Z = 100.0;
            points.Add(point);

            point.X = 5000.0;
            point.Y = 6000.0;
            point.Z = 200.0;
            points.Add(point);

            LasWriter lazWriter = new LasWriter();
            var err = lazWriter.Init();
            if (err == true)
            {
                // Number of point records needs to be set
                lazWriter.Header.number_of_point_records = (uint)points.Count;

                // Header Min/Max needs to be set to extents of points
                lazWriter.Header.min_x = points[0].X; // LL Point
                lazWriter.Header.min_y = points[0].Y;
                lazWriter.Header.min_z = points[0].Z;
                lazWriter.Header.max_x = points[1].X; // UR Point
                lazWriter.Header.max_y = points[1].Y;
                lazWriter.Header.max_z = points[1].Z;

                // Open the writer and test for errors
                err = lazWriter.OpenWriter(FileName, true);
                if (err)
                {
                    double[] coordArray = new double[3];
                    foreach (var p in points)
                    {
                        coordArray[0] = p.X;
                        coordArray[1] = p.Y;
                        coordArray[2] = p.Z;

                        // Set the coordinates in the lazWriter object
                        lazWriter.SetCoordinates(coordArray);

                        // Set the classification to ground
                        lazWriter.Point.classification = 2;

                        // Write the point to the file
                        err = lazWriter.WritePoint();
                        if (!err) break;
                    }

                    // Close the writer to release the file (OS lock)
                    err = lazWriter.CloseWriter();
                    lazWriter = null;
                }
            }

            if (!err)
            {
                MessageBox.Show(lazWriter.Error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
                MessageBox.Show("Write las file succesfully!", "Congregation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // --- Upon completion, file should be 389 bytes
        }

        /// <summary>
        /// Clone to another las file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsbSaveAs_Click(object sender, EventArgs e)
        {
            if (this.saveFileDialog.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show("You HAVE TO choose a las file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            string FileName = this.saveFileDialog.FileName;

            LasWriter lazWriter = new LasWriter();
            lazWriter.SetHeader(lasHeader);

            bool result = lazWriter.OpenWriter(FileName, true);
            if (!result)
            {
                MessageBox.Show(lazWriter.Error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine("  #Open Writer failed : " + lazWriter.Error);
                lazWriter.CloseWriter();
                return;
            }


            //lasReader.SeekPoint(0);
            //// Loop through number of points indicated
            //for (int pointIndex = 0; pointIndex < this.lasHeader.number_of_point_records; pointIndex++)
            //{
            //    // Read the point
            //    LasPoint p = lasReader.ReadPoint();
            //    if (p == null)
            //    {
            //        MessageBox.Show(lasReader.Error, "Read Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //        Debug.WriteLine(" #Read point failed : " + lasReader.Error);
            //        break;
            //    }
            //    // display the member attributes of the point.
            //    Tools.DisplayClassProperties<LasPoint>(p);

            //    result = lazWriter.WritePoint(p);
            //    if (!result)
            //    {
            //        MessageBox.Show(lazWriter.Error, "Write failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //        Debug.WriteLine("  #Write point failed : " + lazWriter.Error);
            //        break;
            //    }
            //}
                        
            lazWriter.CloseWriter();

            if (!result)
            {
                MessageBox.Show("An error occurred while writing to the data", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
                MessageBox.Show("Save the data succesfully!", "Info.", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

    }
}
