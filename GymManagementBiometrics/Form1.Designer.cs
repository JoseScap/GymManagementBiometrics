namespace GymManagementBiometrics
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ButtonInit = new System.Windows.Forms.Button();
            this.LoggingGridView = new System.Windows.Forms.DataGridView();
            this.Message = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FingerprintBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.LoggingGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.FingerprintBox)).BeginInit();
            this.SuspendLayout();
            // 
            // ButtonInit
            // 
            this.ButtonInit.Location = new System.Drawing.Point(12, 12);
            this.ButtonInit.Name = "ButtonInit";
            this.ButtonInit.Size = new System.Drawing.Size(107, 25);
            this.ButtonInit.TabIndex = 0;
            this.ButtonInit.Text = "Iniciar lector";
            this.ButtonInit.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.ButtonInit.UseVisualStyleBackColor = true;
            this.ButtonInit.Click += new System.EventHandler(this.ButtonInit_Click);
            // 
            // LoggingGridView
            // 
            this.LoggingGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.LoggingGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.LoggingGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Message});
            this.LoggingGridView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.LoggingGridView.Location = new System.Drawing.Point(0, 379);
            this.LoggingGridView.Name = "LoggingGridView";
            this.LoggingGridView.Size = new System.Drawing.Size(1040, 246);
            this.LoggingGridView.TabIndex = 3;
            // 
            // Message
            // 
            this.Message.HeaderText = "Mensaje";
            this.Message.Name = "Message";
            this.Message.ReadOnly = true;
            // 
            // FingerprintBox
            // 
            this.FingerprintBox.Location = new System.Drawing.Point(643, 12);
            this.FingerprintBox.Name = "FingerprintBox";
            this.FingerprintBox.Size = new System.Drawing.Size(385, 361);
            this.FingerprintBox.TabIndex = 4;
            this.FingerprintBox.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1040, 625);
            this.Controls.Add(this.FingerprintBox);
            this.Controls.Add(this.LoggingGridView);
            this.Controls.Add(this.ButtonInit);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.LoggingGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.FingerprintBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button ButtonInit;
        private System.Windows.Forms.DataGridView LoggingGridView;
        private System.Windows.Forms.DataGridViewTextBoxColumn Message;
        private System.Windows.Forms.PictureBox FingerprintBox;
    }
}

