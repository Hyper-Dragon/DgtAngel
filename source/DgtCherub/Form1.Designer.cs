
namespace DgtCherub
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelSpacer;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            System.Windows.Forms.GroupBox groupBox1;
            this.ButtonSendTestMsg2 = new System.Windows.Forms.Button();
            this.ButtonSendTestMsg1 = new System.Windows.Forms.Button();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.TextBoxConsole = new System.Windows.Forms.TextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.ToolStripStatusLabelVersion = new System.Windows.Forms.ToolStripStatusLabel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            toolStripStatusLabelSpacer = new System.Windows.Forms.ToolStripStatusLabel();
            groupBox1 = new System.Windows.Forms.GroupBox();
            groupBox1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripStatusLabelSpacer
            // 
            resources.ApplyResources(toolStripStatusLabelSpacer, "toolStripStatusLabelSpacer");
            toolStripStatusLabelSpacer.Name = "toolStripStatusLabelSpacer";
            toolStripStatusLabelSpacer.Spring = true;
            // 
            // groupBox1
            // 
            resources.ApplyResources(groupBox1, "groupBox1");
            groupBox1.Controls.Add(this.ButtonSendTestMsg2);
            groupBox1.Controls.Add(this.ButtonSendTestMsg1);
            groupBox1.Name = "groupBox1";
            groupBox1.TabStop = false;
            // 
            // ButtonSendTestMsg2
            // 
            resources.ApplyResources(this.ButtonSendTestMsg2, "ButtonSendTestMsg2");
            this.ButtonSendTestMsg2.Name = "ButtonSendTestMsg2";
            this.ButtonSendTestMsg2.UseVisualStyleBackColor = true;
            this.ButtonSendTestMsg2.Click += new System.EventHandler(this.ButtonSendTestMsg2_Click);
            // 
            // ButtonSendTestMsg1
            // 
            resources.ApplyResources(this.ButtonSendTestMsg1, "ButtonSendTestMsg1");
            this.ButtonSendTestMsg1.Name = "ButtonSendTestMsg1";
            this.ButtonSendTestMsg1.UseVisualStyleBackColor = true;
            this.ButtonSendTestMsg1.Click += new System.EventHandler(this.ButtonSendTestMsg1_Click);
            // 
            // radioButton1
            // 
            resources.ApplyResources(this.radioButton1, "radioButton1");
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.TabStop = true;
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // tabControl1
            // 
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            // 
            // tabPage1
            // 
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Controls.Add(groupBox1);
            this.tabPage1.Controls.Add(this.radioButton1);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            resources.ApplyResources(this.tabPage2, "tabPage2");
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // TextBoxConsole
            // 
            resources.ApplyResources(this.TextBoxConsole, "TextBoxConsole");
            this.TextBoxConsole.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TextBoxConsole.Name = "TextBoxConsole";
            this.TextBoxConsole.ReadOnly = true;
            this.TextBoxConsole.TabStop = false;
            // 
            // statusStrip1
            // 
            resources.ApplyResources(this.statusStrip1, "statusStrip1");
            this.statusStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Visible;
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            toolStripStatusLabelSpacer,
            this.ToolStripStatusLabelVersion});
            this.statusStrip1.Name = "statusStrip1";
            // 
            // ToolStripStatusLabelVersion
            // 
            resources.ApplyResources(this.ToolStripStatusLabelVersion, "ToolStripStatusLabelVersion");
            this.ToolStripStatusLabelVersion.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ToolStripStatusLabelVersion.Name = "ToolStripStatusLabelVersion";
            // 
            // panel1
            // 
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Controls.Add(this.tabControl1);
            this.panel1.Controls.Add(this.TextBoxConsole);
            this.panel1.Name = "panel1";
            // 
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Name = "menuStrip1";
            // 
            // Form1
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.DoubleBuffered = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Shown += new System.EventHandler(this.Form1_Shown);
            groupBox1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ButtonSendTestMsg1;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TextBox TextBoxConsole;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.MenuStrip menuStrip1;

        private System.Windows.Forms.Button ButtonSendTestMsg2;
        private System.Windows.Forms.ToolStripStatusLabel ToolStripStatusLabelVersion;
    }
}

