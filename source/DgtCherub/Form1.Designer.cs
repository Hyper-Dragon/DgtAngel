
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
            System.Windows.Forms.TabPage TabPageAbout;
            System.Windows.Forms.PictureBox pictureBox1;
            System.Windows.Forms.TabPage TabPageTest;
            System.Windows.Forms.Panel panel1;
            this.ButtonSendTestMsg2 = new System.Windows.Forms.Button();
            this.ButtonSendTestMsg1 = new System.Windows.Forms.Button();
            this.LinkLabelAbout1 = new System.Windows.Forms.LinkLabel();
            this.button1 = new System.Windows.Forms.Button();
            this.TabControlSidePanel = new System.Windows.Forms.TabControl();
            this.TabPageConfig = new System.Windows.Forms.TabPage();
            this.CheckBoxShowInbound = new System.Windows.Forms.CheckBox();
            this.ButtonRabbitConfig = new System.Windows.Forms.Button();
            this.CheckBoxOnTop = new System.Windows.Forms.CheckBox();
            this.TabPageBoards = new System.Windows.Forms.TabPage();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.PictureBoxRemote = new System.Windows.Forms.PictureBox();
            this.PictureBoxLocal = new System.Windows.Forms.PictureBox();
            this.TextBoxConsole = new System.Windows.Forms.TextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.ToolStripStatusLabelVersion = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            toolStripStatusLabelSpacer = new System.Windows.Forms.ToolStripStatusLabel();
            groupBox1 = new System.Windows.Forms.GroupBox();
            TabPageAbout = new System.Windows.Forms.TabPage();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            TabPageTest = new System.Windows.Forms.TabPage();
            panel1 = new System.Windows.Forms.Panel();
            groupBox1.SuspendLayout();
            TabPageAbout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(pictureBox1)).BeginInit();
            TabPageTest.SuspendLayout();
            panel1.SuspendLayout();
            this.TabControlSidePanel.SuspendLayout();
            this.TabPageConfig.SuspendLayout();
            this.TabPageBoards.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBoxRemote)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBoxLocal)).BeginInit();
            this.statusStrip1.SuspendLayout();
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
            // TabPageAbout
            // 
            resources.ApplyResources(TabPageAbout, "TabPageAbout");
            TabPageAbout.Controls.Add(this.LinkLabelAbout1);
            TabPageAbout.Controls.Add(pictureBox1);
            TabPageAbout.Name = "TabPageAbout";
            TabPageAbout.UseVisualStyleBackColor = true;
            // 
            // LinkLabelAbout1
            // 
            resources.ApplyResources(this.LinkLabelAbout1, "LinkLabelAbout1");
            this.LinkLabelAbout1.AutoEllipsis = true;
            this.LinkLabelAbout1.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.LinkLabelAbout1.Name = "LinkLabelAbout1";
            this.LinkLabelAbout1.TabStop = true;
            // 
            // pictureBox1
            // 
            resources.ApplyResources(pictureBox1, "pictureBox1");
            pictureBox1.Name = "pictureBox1";
            pictureBox1.TabStop = false;
            // 
            // TabPageTest
            // 
            resources.ApplyResources(TabPageTest, "TabPageTest");
            TabPageTest.Controls.Add(groupBox1);
            TabPageTest.Controls.Add(this.button1);
            TabPageTest.Name = "TabPageTest";
            TabPageTest.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            resources.ApplyResources(this.button1, "button1");
            this.button1.Name = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.ButtonRabbitConfig_Click);
            // 
            // panel1
            // 
            resources.ApplyResources(panel1, "panel1");
            panel1.Controls.Add(this.TabControlSidePanel);
            panel1.Controls.Add(this.TextBoxConsole);
            panel1.Name = "panel1";
            // 
            // TabControlSidePanel
            // 
            resources.ApplyResources(this.TabControlSidePanel, "TabControlSidePanel");
            this.TabControlSidePanel.CausesValidation = false;
            this.TabControlSidePanel.Controls.Add(TabPageAbout);
            this.TabControlSidePanel.Controls.Add(this.TabPageConfig);
            this.TabControlSidePanel.Controls.Add(TabPageTest);
            this.TabControlSidePanel.Controls.Add(this.TabPageBoards);
            this.TabControlSidePanel.HotTrack = true;
            this.TabControlSidePanel.Multiline = true;
            this.TabControlSidePanel.Name = "TabControlSidePanel";
            this.TabControlSidePanel.SelectedIndex = 0;
            this.TabControlSidePanel.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            // 
            // TabPageConfig
            // 
            resources.ApplyResources(this.TabPageConfig, "TabPageConfig");
            this.TabPageConfig.Controls.Add(this.CheckBoxShowInbound);
            this.TabPageConfig.Controls.Add(this.ButtonRabbitConfig);
            this.TabPageConfig.Controls.Add(this.CheckBoxOnTop);
            this.TabPageConfig.Name = "TabPageConfig";
            this.TabPageConfig.UseVisualStyleBackColor = true;
            // 
            // CheckBoxShowInbound
            // 
            resources.ApplyResources(this.CheckBoxShowInbound, "CheckBoxShowInbound");
            this.CheckBoxShowInbound.Checked = true;
            this.CheckBoxShowInbound.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CheckBoxShowInbound.Name = "CheckBoxShowInbound";
            this.CheckBoxShowInbound.UseVisualStyleBackColor = true;
            this.CheckBoxShowInbound.CheckedChanged += new System.EventHandler(this.CheckBoxShowInbound_CheckedChanged);
            // 
            // ButtonRabbitConfig
            // 
            resources.ApplyResources(this.ButtonRabbitConfig, "ButtonRabbitConfig");
            this.ButtonRabbitConfig.Name = "ButtonRabbitConfig";
            this.ButtonRabbitConfig.UseVisualStyleBackColor = true;
            this.ButtonRabbitConfig.Click += new System.EventHandler(this.ButtonRabbitConfig_Click);
            // 
            // CheckBoxOnTop
            // 
            resources.ApplyResources(this.CheckBoxOnTop, "CheckBoxOnTop");
            this.CheckBoxOnTop.Checked = true;
            this.CheckBoxOnTop.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CheckBoxOnTop.Name = "CheckBoxOnTop";
            this.CheckBoxOnTop.UseVisualStyleBackColor = true;
            this.CheckBoxOnTop.CheckedChanged += new System.EventHandler(this.CheckBoxOnTop_CheckedChanged);
            // 
            // TabPageBoards
            // 
            resources.ApplyResources(this.TabPageBoards, "TabPageBoards");
            this.TabPageBoards.Controls.Add(this.label2);
            this.TabPageBoards.Controls.Add(this.label1);
            this.TabPageBoards.Controls.Add(this.PictureBoxRemote);
            this.TabPageBoards.Controls.Add(this.PictureBoxLocal);
            this.TabPageBoards.Name = "TabPageBoards";
            this.TabPageBoards.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.BackColor = System.Drawing.Color.Silver;
            this.label2.Name = "label2";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.BackColor = System.Drawing.Color.Silver;
            this.label1.Name = "label1";
            // 
            // PictureBoxRemote
            // 
            resources.ApplyResources(this.PictureBoxRemote, "PictureBoxRemote");
            this.PictureBoxRemote.Name = "PictureBoxRemote";
            this.PictureBoxRemote.TabStop = false;
            // 
            // PictureBoxLocal
            // 
            resources.ApplyResources(this.PictureBoxLocal, "PictureBoxLocal");
            this.PictureBoxLocal.Name = "PictureBoxLocal";
            this.PictureBoxLocal.TabStop = false;
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
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Name = "menuStrip1";
            // 
            // Form1
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(panel1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.DoubleBuffered = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.Form1_Shown);
            groupBox1.ResumeLayout(false);
            TabPageAbout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(pictureBox1)).EndInit();
            TabPageTest.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            this.TabControlSidePanel.ResumeLayout(false);
            this.TabPageConfig.ResumeLayout(false);
            this.TabPageBoards.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PictureBoxRemote)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBoxLocal)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ButtonSendTestMsg1;
        private System.Windows.Forms.TabControl TabControlSidePanel;
        private System.Windows.Forms.TabPage TabPageConfig;
        private System.Windows.Forms.TabPage TabPageTest;
        private System.Windows.Forms.TextBox TextBoxConsole;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.MenuStrip menuStrip1;

        private System.Windows.Forms.Button ButtonSendTestMsg2;
        private System.Windows.Forms.ToolStripStatusLabel ToolStripStatusLabelVersion;
        private System.Windows.Forms.CheckBox CheckBoxOnTop;
        private System.Windows.Forms.Button ButtonRabbitConfig;
        private System.Windows.Forms.CheckBox CheckBoxShowInbound;
        private System.Windows.Forms.LinkLabel LinkLabelAbout1;
        private System.Windows.Forms.TabPage TabPageBoards;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox PictureBoxRemote;
        private System.Windows.Forms.PictureBox PictureBoxLocal;
        private System.Windows.Forms.Button button1;
    }
}

