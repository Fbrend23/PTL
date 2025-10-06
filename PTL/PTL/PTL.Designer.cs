namespace PTL
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.formsPlot1 = new ScottPlot.WinForms.FormsPlot();
            this.comboYearFrom = new System.Windows.Forms.ComboBox();
            this.comboYearTo = new System.Windows.Forms.ComboBox();
            this.checkedListCities = new System.Windows.Forms.CheckedListBox();
            this.comboGranularity = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // formsPlot1
            // 
            this.formsPlot1.BackColor = System.Drawing.Color.Fuchsia;
            this.formsPlot1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.formsPlot1.DisplayScale = 1F;
            this.formsPlot1.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.formsPlot1.Location = new System.Drawing.Point(158, 100);
            this.formsPlot1.Name = "formsPlot1";
            this.formsPlot1.Size = new System.Drawing.Size(630, 338);
            this.formsPlot1.TabIndex = 0;
            this.formsPlot1.Load += new System.EventHandler(this.formsPlot1_Load);
            // 
            // comboYearFrom
            // 
            this.comboYearFrom.BackColor = System.Drawing.Color.HotPink;
            this.comboYearFrom.FormattingEnabled = true;
            this.comboYearFrom.Location = new System.Drawing.Point(28, 64);
            this.comboYearFrom.Name = "comboYearFrom";
            this.comboYearFrom.Size = new System.Drawing.Size(121, 23);
            this.comboYearFrom.TabIndex = 3;
            // 
            // comboYearTo
            // 
            this.comboYearTo.BackColor = System.Drawing.Color.HotPink;
            this.comboYearTo.FormattingEnabled = true;
            this.comboYearTo.Location = new System.Drawing.Point(158, 64);
            this.comboYearTo.Name = "comboYearTo";
            this.comboYearTo.Size = new System.Drawing.Size(121, 23);
            this.comboYearTo.TabIndex = 4;
            // 
            // checkedListCities
            // 
            this.checkedListCities.BackColor = System.Drawing.Color.MediumOrchid;
            this.checkedListCities.FormattingEnabled = true;
            this.checkedListCities.Location = new System.Drawing.Point(28, 109);
            this.checkedListCities.Name = "checkedListCities";
            this.checkedListCities.Size = new System.Drawing.Size(120, 310);
            this.checkedListCities.TabIndex = 5;
            // 
            // comboGranularity
            // 
            this.comboGranularity.BackColor = System.Drawing.Color.SeaGreen;
            this.comboGranularity.FormattingEnabled = true;
            this.comboGranularity.Location = new System.Drawing.Point(422, 64);
            this.comboGranularity.Name = "comboGranularity";
            this.comboGranularity.Size = new System.Drawing.Size(121, 23);
            this.comboGranularity.TabIndex = 6;
            this.comboGranularity.SelectedIndexChanged += new System.EventHandler(this.comboGranularity_SelectedIndexChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Fuchsia;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.comboGranularity);
            this.Controls.Add(this.checkedListCities);
            this.Controls.Add(this.comboYearTo);
            this.Controls.Add(this.comboYearFrom);
            this.Controls.Add(this.formsPlot1);
            this.ForeColor = System.Drawing.Color.Orange;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Plot Thoses Lines";
            this.ResumeLayout(false);

        }

        #endregion

        private ScottPlot.WinForms.FormsPlot formsPlot1;
        private ComboBox comboYearFrom;
        private ComboBox comboYearTo;
        private CheckedListBox checkedListCities;
        private ComboBox comboGranularity;
    }
}