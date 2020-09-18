using System;
using System.ComponentModel;
using System.Windows.Forms;
using NetEti.FileTools.Zip;
using System.Collections.Generic;
using System.Linq;

namespace NetEti.DemoApplications
{
    /// <summary>
    /// Demo für ZipAccess.cs
    /// </summary>
    public partial class Form1 : Form
    {
        private readonly ZipAccess _zipAccessor;
        private bool _canceled;

        /// <summary>
        /// Constructor
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            this.tbxDirPath.Text = @"Test\Sub";
            this.tbxFilePath.Text = @"Test\Debug.zip";
            this.progressBar1.Minimum = 0;
            this.progressBar1.Maximum = 100;
            this._zipAccessor = new ZipAccess();
            this._zipAccessor.ZipProgressChanged += this.ShowZipProgress;
            this._zipAccessor.ZipProgressFinished += this.ShowZipProgressFinished;
        }

        private void btnZipInfo_Click(object sender, EventArgs e)
        {
            if (this._zipAccessor.IsZip(this.tbxFilePath.Text))
            {
                List<ZippedFileInfo> zipInfos = this._zipAccessor.GetZipEntryList(this.tbxFilePath.Text, "");
                foreach (ZippedFileInfo info in zipInfos)
                {
                    this.listBox1.Items.Add(info.ToString());
                }
            }
            else
            {
                MessageBox.Show(String.Format("'{0}' konnte nicht als Zip-File erkannt werden.", this.tbxFilePath.Text));
            }
        }

        private void btnUnzipAll_Click(object sender, EventArgs e)
        {
            this._canceled = false;
            this.listBox1.Items.Add("UnzipAll: " + this.tbxFilePath.Text + (tbxDirPath.Text.Equals("") ? "" : " nach " + tbxDirPath.Text));
            this.progressBar1.Value = 0;
            try
            {
                this._zipAccessor.UnZipArchive(this.tbxFilePath.Text, tbxDirPath.Text, "", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnUzipFiles_Click(object sender, EventArgs e)
        {
            this._canceled = false;
            this.listBox1.Items.Add("UnzipFiles: " + this.tbxFilePath.Text + (tbxDirPath.Text.Equals("") ? "" : " nach " + tbxDirPath.Text));
            this.progressBar1.Value = 0;
            try
            {
                // Test1
                string[] filePathes = this._zipAccessor.GetZipEntryFilePathes(this._zipAccessor
                  .GetZipEntryList(this.tbxFilePath.Text, null)).Where(p => p.StartsWith("obj")).ToArray();
                if (filePathes.Length > 0)
                {
                    this._zipAccessor.UnZipArchiveFiles(this.tbxFilePath.Text, tbxDirPath.Text, "", false, filePathes);
                }
                // Test2
                filePathes = this._zipAccessor.GetZipEntryFilePathes(this._zipAccessor
                  .GetZipEntryList(this.tbxFilePath.Text, null)).Where(p => p.EndsWith("00800377.TIF")).ToArray();
                if (filePathes.Length > 0)
                {
                    this._zipAccessor.UnZipArchiveFiles(this.tbxFilePath.Text, tbxDirPath.Text, "", false, filePathes);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnZipDirectory_Click(object sender, EventArgs e)
        {
            this._canceled = false;
            this.listBox1.Items.Add("ZipDirectory: " + this.tbxDirPath.Text + (this.tbxFilePath.Text.Equals("") ? "" : " nach " + this.tbxFilePath.Text));
            try
            {
                this._zipAccessor.ZipDirectory(this.tbxDirPath.Text, this.tbxFilePath.Text, null, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnChooseFile_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.CheckPathExists = true;
            this.openFileDialog1.CheckFileExists = false;
            this.openFileDialog1.InitialDirectory = this.tbxDirPath.Text;

            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.tbxFilePath.Text = this.openFileDialog1.FileName;
                if (this._zipAccessor.IsZip(this.tbxFilePath.Text))
                {
                    this.label3.Text = "zipped";
                }
                else
                {
                    this.label3.Text = "";
                }
            }
        }

        private void btnChooseDir_Click(object sender, EventArgs e)
        {
            this.folderBrowserDialog1.SelectedPath = this.tbxDirPath.Text;
            if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.tbxDirPath.Text = this.folderBrowserDialog1.SelectedPath;
            }
        }

        private void OnUpdateInfos(ProgressChangedEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<ProgressChangedEventArgs>(OnUpdateInfos), args);
            }
            else
            {
                this.progressBar1.Value = args.ProgressPercentage;
            }
        }

        private void ShowZipProgress(object sender, ProgressChangedEventArgs args)
        {
            if (this._canceled)
            {
                this._zipAccessor.Abort();
            }
            this.OnUpdateInfos(args);
        }

        private void ShowZipProgressFinished(object sender, ProgressChangedEventArgs args)
        {
            this.OnUpdateInfos(args);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this._canceled = true;
            this._zipAccessor.Dispose();
        }
    }
}
