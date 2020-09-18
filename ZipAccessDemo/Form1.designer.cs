namespace NetEti.DemoApplications
{
  partial class Form1
  {
    /// <summary>
    /// Erforderliche Designervariable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Verwendete Ressourcen bereinigen.
    /// </summary>
    /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Vom Windows Form-Designer generierter Code

    /// <summary>
    /// Erforderliche Methode für die Designerunterstützung.
    /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
    /// </summary>
    private void InitializeComponent()
    {
      this.listBox1 = new System.Windows.Forms.ListBox();
      this.progressBar1 = new System.Windows.Forms.ProgressBar();
      this.tbxFilePath = new System.Windows.Forms.TextBox();
      this.btnChooseFile = new System.Windows.Forms.Button();
      this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
      this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
      this.btnZipDirectory = new System.Windows.Forms.Button();
      this.btnChooseDir = new System.Windows.Forms.Button();
      this.tbxDirPath = new System.Windows.Forms.TextBox();
      this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
      this.label1 = new System.Windows.Forms.Label();
      this.label2 = new System.Windows.Forms.Label();
      this.btnUnzipAll = new System.Windows.Forms.Button();
      this.label3 = new System.Windows.Forms.Label();
      this.btnZipInfo = new System.Windows.Forms.Button();
      this.btnUzipFiles = new System.Windows.Forms.Button();
      this.SuspendLayout();
      // 
      // listBox1
      // 
      this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.listBox1.FormattingEnabled = true;
      this.listBox1.Location = new System.Drawing.Point(12, 118);
      this.listBox1.Name = "listBox1";
      this.listBox1.Size = new System.Drawing.Size(489, 134);
      this.listBox1.TabIndex = 1;
      // 
      // progressBar1
      // 
      this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.progressBar1.BackColor = System.Drawing.SystemColors.Control;
      this.progressBar1.ForeColor = System.Drawing.SystemColors.ControlText;
      this.progressBar1.Location = new System.Drawing.Point(12, 268);
      this.progressBar1.Name = "progressBar1";
      this.progressBar1.Size = new System.Drawing.Size(489, 23);
      this.progressBar1.TabIndex = 2;
      // 
      // tbxFilePath
      // 
      this.tbxFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.tbxFilePath.Location = new System.Drawing.Point(12, 79);
      this.tbxFilePath.Name = "tbxFilePath";
      this.tbxFilePath.Size = new System.Drawing.Size(459, 20);
      this.tbxFilePath.TabIndex = 3;
      // 
      // btnChooseFile
      // 
      this.btnChooseFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnChooseFile.Location = new System.Drawing.Point(468, 78);
      this.btnChooseFile.Name = "btnChooseFile";
      this.btnChooseFile.Size = new System.Drawing.Size(33, 20);
      this.btnChooseFile.TabIndex = 4;
      this.btnChooseFile.Text = "•••";
      this.btnChooseFile.UseVisualStyleBackColor = true;
      this.btnChooseFile.Click += new System.EventHandler(this.btnChooseFile_Click);
      // 
      // openFileDialog1
      // 
      this.openFileDialog1.FileName = "openFileDialog1";
      // 
      // btnZipDirectory
      // 
      this.btnZipDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnZipDirectory.Location = new System.Drawing.Point(405, 308);
      this.btnZipDirectory.Name = "btnZipDirectory";
      this.btnZipDirectory.Size = new System.Drawing.Size(90, 23);
      this.btnZipDirectory.TabIndex = 5;
      this.btnZipDirectory.Text = "ZipDirectory";
      this.btnZipDirectory.UseVisualStyleBackColor = true;
      this.btnZipDirectory.Click += new System.EventHandler(this.btnZipDirectory_Click);
      // 
      // btnChooseDir
      // 
      this.btnChooseDir.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnChooseDir.Location = new System.Drawing.Point(468, 28);
      this.btnChooseDir.Name = "btnChooseDir";
      this.btnChooseDir.Size = new System.Drawing.Size(33, 20);
      this.btnChooseDir.TabIndex = 7;
      this.btnChooseDir.Text = "•••";
      this.btnChooseDir.UseVisualStyleBackColor = true;
      this.btnChooseDir.Click += new System.EventHandler(this.btnChooseDir_Click);
      // 
      // tbxDirPath
      // 
      this.tbxDirPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.tbxDirPath.Location = new System.Drawing.Point(12, 29);
      this.tbxDirPath.Name = "tbxDirPath";
      this.tbxDirPath.Size = new System.Drawing.Size(459, 20);
      this.tbxDirPath.TabIndex = 6;
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(12, 63);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(32, 13);
      this.label1.TabIndex = 8;
      this.label1.Text = "Datei";
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(12, 12);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(61, 13);
      this.label2.TabIndex = 9;
      this.label2.Text = "Verzeichnis";
      // 
      // btnUnzipAll
      // 
      this.btnUnzipAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnUnzipAll.Location = new System.Drawing.Point(309, 308);
      this.btnUnzipAll.Name = "btnUnzipAll";
      this.btnUnzipAll.Size = new System.Drawing.Size(90, 23);
      this.btnUnzipAll.TabIndex = 10;
      this.btnUnzipAll.Text = "UnzipAll";
      this.btnUnzipAll.UseVisualStyleBackColor = true;
      this.btnUnzipAll.Click += new System.EventHandler(this.btnUnzipAll_Click);
      // 
      // label3
      // 
      this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.label3.AutoSize = true;
      this.label3.Location = new System.Drawing.Point(12, 308);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(0, 13);
      this.label3.TabIndex = 11;
      // 
      // btnZipInfo
      // 
      this.btnZipInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnZipInfo.Location = new System.Drawing.Point(133, 308);
      this.btnZipInfo.Margin = new System.Windows.Forms.Padding(2);
      this.btnZipInfo.Name = "btnZipInfo";
      this.btnZipInfo.Size = new System.Drawing.Size(90, 23);
      this.btnZipInfo.TabIndex = 12;
      this.btnZipInfo.Text = "ZipInfo";
      this.btnZipInfo.UseVisualStyleBackColor = true;
      this.btnZipInfo.Click += new System.EventHandler(this.btnZipInfo_Click);
      // 
      // btnUzipFiles
      // 
      this.btnUzipFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnUzipFiles.Location = new System.Drawing.Point(228, 308);
      this.btnUzipFiles.Name = "btnUzipFiles";
      this.btnUzipFiles.Size = new System.Drawing.Size(75, 23);
      this.btnUzipFiles.TabIndex = 13;
      this.btnUzipFiles.Text = "Unzip Files";
      this.btnUzipFiles.UseVisualStyleBackColor = true;
      this.btnUzipFiles.Click += new System.EventHandler(this.btnUzipFiles_Click);
      // 
      // Form1
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(513, 353);
      this.Controls.Add(this.btnUzipFiles);
      this.Controls.Add(this.btnZipInfo);
      this.Controls.Add(this.label3);
      this.Controls.Add(this.btnUnzipAll);
      this.Controls.Add(this.label2);
      this.Controls.Add(this.label1);
      this.Controls.Add(this.btnChooseDir);
      this.Controls.Add(this.tbxDirPath);
      this.Controls.Add(this.btnZipDirectory);
      this.Controls.Add(this.btnChooseFile);
      this.Controls.Add(this.tbxFilePath);
      this.Controls.Add(this.progressBar1);
      this.Controls.Add(this.listBox1);
      this.Name = "Form1";
      this.Text = "ZipToolsDemo";
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion
    private System.Windows.Forms.ListBox listBox1;
    private System.Windows.Forms.ProgressBar progressBar1;
    private System.Windows.Forms.TextBox tbxFilePath;
    private System.Windows.Forms.Button btnChooseFile;
    private System.Windows.Forms.OpenFileDialog openFileDialog1;
    private System.Windows.Forms.SaveFileDialog saveFileDialog1;
    private System.Windows.Forms.Button btnZipDirectory;
    private System.Windows.Forms.Button btnChooseDir;
    private System.Windows.Forms.TextBox tbxDirPath;
    private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Button btnUnzipAll;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Button btnZipInfo;
    private System.Windows.Forms.Button btnUzipFiles;
  }
}

