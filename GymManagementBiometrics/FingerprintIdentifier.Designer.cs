namespace GymManagementBiometrics
{
    partial class FingerprintIdentifier
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
            this.FingerprintBox = new System.Windows.Forms.PictureBox();
            this.Status = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.FingerprintBox)).BeginInit();
            this.SuspendLayout();
            // 
            // FingerprintBox
            // 
            this.FingerprintBox.Location = new System.Drawing.Point(12, 12);
            this.FingerprintBox.Name = "FingerprintBox";
            this.FingerprintBox.Size = new System.Drawing.Size(337, 426);
            this.FingerprintBox.TabIndex = 0;
            this.FingerprintBox.TabStop = false;
            // 
            // Status
            // 
            this.Status.Enabled = false;
            this.Status.Location = new System.Drawing.Point(356, 417);
            this.Status.Name = "Status";
            this.Status.Size = new System.Drawing.Size(178, 20);
            this.Status.TabIndex = 1;
            // 
            // FingerprintIdentifier
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(546, 450);
            this.Controls.Add(this.Status);
            this.Controls.Add(this.FingerprintBox);
            this.Name = "FingerprintIdentifier";
            this.Text = "FingerprintIdentifier";
            this.Load += new System.EventHandler(this.FingerprintIdentifier_Load);
            ((System.ComponentModel.ISupportInitialize)(this.FingerprintBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox FingerprintBox;
        private System.Windows.Forms.TextBox Status;
    }
}