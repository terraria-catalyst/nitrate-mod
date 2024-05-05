using System;

namespace Terraria.ModLoader.Setup
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
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
			components = new System.ComponentModel.Container();
			var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			buttonCancel = new System.Windows.Forms.Button();
			progressBar = new System.Windows.Forms.ProgressBar();
			labelStatus = new System.Windows.Forms.Label();
			buttonSetup = new System.Windows.Forms.Button();
			buttonDecompile = new System.Windows.Forms.Button();
			buttonDiffTerraria = new System.Windows.Forms.Button();
			buttonPatchTerraria = new System.Windows.Forms.Button();
			buttonPatchModLoader = new System.Windows.Forms.Button();
			buttonDiffModLoader = new System.Windows.Forms.Button();
			buttonPatchNitrate = new System.Windows.Forms.Button();
			buttonDiffNitrate = new System.Windows.Forms.Button();
			toolTipButtons = new System.Windows.Forms.ToolTip(components);
			buttonRegenSource = new System.Windows.Forms.Button();
			buttonDiffTerrariaNetCore = new System.Windows.Forms.Button();
			buttonPatchTerrariaNetCore = new System.Windows.Forms.Button();
			mainMenuStrip = new System.Windows.Forms.MenuStrip();
			menuItemOptions = new System.Windows.Forms.ToolStripMenuItem();
			menuItemTerraria = new System.Windows.Forms.ToolStripMenuItem();
			menuItemTmlPath = new System.Windows.Forms.ToolStripMenuItem();
			resetTimeStampOptimizationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			formatDecompiledOutputToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			decompileServerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			formatCodeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			hookGenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			simplifierToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			patchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			exactToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			offsetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			fuzzyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			labelWorkingDirectory = new System.Windows.Forms.Label();
			mainMenuStrip.SuspendLayout();
			SuspendLayout();
			// 
			// buttonCancel
			// 
			buttonCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			buttonCancel.Enabled = false;
			buttonCancel.Location = new System.Drawing.Point(158, 486);
			buttonCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			buttonCancel.Name = "buttonCancel";
			buttonCancel.Size = new System.Drawing.Size(96, 27);
			buttonCancel.TabIndex = 6;
			buttonCancel.Text = "Cancel";
			buttonCancel.UseVisualStyleBackColor = true;
			buttonCancel.Click += buttonCancel_Click;
			// 
			// progressBar
			// 
			progressBar.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			progressBar.Location = new System.Drawing.Point(14, 452);
			progressBar.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			progressBar.Name = "progressBar";
			progressBar.Size = new System.Drawing.Size(379, 27);
			progressBar.TabIndex = 1;
			// 
			// labelStatus
			// 
			labelStatus.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
			labelStatus.Location = new System.Drawing.Point(14, 246);
			labelStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			labelStatus.Name = "labelStatus";
			labelStatus.Size = new System.Drawing.Size(377, 202);
			labelStatus.TabIndex = 3;
			labelStatus.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			// 
			// buttonSetup
			// 
			buttonSetup.Anchor = System.Windows.Forms.AnchorStyles.Top;
			buttonSetup.Location = new System.Drawing.Point(52, 48);
			buttonSetup.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			buttonSetup.Name = "buttonSetup";
			buttonSetup.Size = new System.Drawing.Size(150, 27);
			buttonSetup.TabIndex = 0;
			buttonSetup.Text = "Setup";
			toolTipButtons.SetToolTip(buttonSetup, "Complete environment setup for working on tModLoader source\r\nEquivalent to Decompile+Patch+SetupDebug\r\nEdit the source in src/tModLoader then run Diff tModLoader and commit the /patches folder");
			buttonSetup.UseVisualStyleBackColor = true;
			buttonSetup.Click += buttonTask_Click;
			// 
			// buttonDecompile
			// 
			buttonDecompile.Anchor = System.Windows.Forms.AnchorStyles.Top;
			buttonDecompile.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			buttonDecompile.Location = new System.Drawing.Point(210, 48);
			buttonDecompile.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			buttonDecompile.Name = "buttonDecompile";
			buttonDecompile.Size = new System.Drawing.Size(150, 27);
			buttonDecompile.TabIndex = 1;
			buttonDecompile.Text = "Decompile";
			toolTipButtons.SetToolTip(buttonDecompile, "Uses ILSpy to decompile Terraria\r\nAlso decompiles server classes not included in the client binary\r\nOutputs to src/decompiled");
			buttonDecompile.UseVisualStyleBackColor = true;
			buttonDecompile.Click += buttonTask_Click;
			// 
			// buttonDiffTerraria
			// 
			buttonDiffTerraria.Anchor = System.Windows.Forms.AnchorStyles.Top;
			buttonDiffTerraria.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			buttonDiffTerraria.Location = new System.Drawing.Point(52, 82);
			buttonDiffTerraria.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			buttonDiffTerraria.Name = "buttonDiffTerraria";
			buttonDiffTerraria.Size = new System.Drawing.Size(150, 27);
			buttonDiffTerraria.TabIndex = 2;
			buttonDiffTerraria.Text = "Diff Terraria";
			toolTipButtons.SetToolTip(buttonDiffTerraria, "Recalculates the Terraria patches\r\nDiffs the src/Terraria directory\r\nUsed for fixing decompilation errors\r\n");
			buttonDiffTerraria.UseVisualStyleBackColor = true;
			buttonDiffTerraria.Click += buttonTask_Click;
			// 
			// buttonPatchTerraria
			// 
			buttonPatchTerraria.Anchor = System.Windows.Forms.AnchorStyles.Top;
			buttonPatchTerraria.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			buttonPatchTerraria.Location = new System.Drawing.Point(210, 82);
			buttonPatchTerraria.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			buttonPatchTerraria.Name = "buttonPatchTerraria";
			buttonPatchTerraria.Size = new System.Drawing.Size(150, 27);
			buttonPatchTerraria.TabIndex = 3;
			buttonPatchTerraria.Text = "Patch Terraria";
			toolTipButtons.SetToolTip(buttonPatchTerraria, "Applies patches to fix decompile errors\r\nLeaves functionality unchanged\r\nPatched source is located in src/Terraria");
			buttonPatchTerraria.UseVisualStyleBackColor = true;
			buttonPatchTerraria.Click += buttonTask_Click;
			// 
			// buttonPatchModLoader
			// 
			buttonPatchModLoader.Anchor = System.Windows.Forms.AnchorStyles.Top;
			buttonPatchModLoader.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			buttonPatchModLoader.Location = new System.Drawing.Point(210, 149);
			buttonPatchModLoader.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			buttonPatchModLoader.Name = "buttonPatchModLoader";
			buttonPatchModLoader.Size = new System.Drawing.Size(150, 27);
			buttonPatchModLoader.TabIndex = 6;
			buttonPatchModLoader.Text = "Patch tModLoader";
			toolTipButtons.SetToolTip(buttonPatchModLoader, "Applies tModLoader patches to Terraria\r\nEdit the source code in src/tModLoader after this phase\r\nInternally formats the Terraria sources before patching");
			buttonPatchModLoader.UseVisualStyleBackColor = true;
			buttonPatchModLoader.Click += buttonTask_Click;
			// 
			// buttonDiffModLoader
			// 
			buttonDiffModLoader.Anchor = System.Windows.Forms.AnchorStyles.Top;
			buttonDiffModLoader.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			buttonDiffModLoader.Location = new System.Drawing.Point(52, 149);
			buttonDiffModLoader.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			buttonDiffModLoader.Name = "buttonDiffModLoader";
			buttonDiffModLoader.Size = new System.Drawing.Size(150, 27);
			buttonDiffModLoader.TabIndex = 7;
			buttonDiffModLoader.Text = "Diff tModLoader";
			toolTipButtons.SetToolTip(buttonDiffModLoader, resources.GetString("buttonDiffModLoader.ToolTip"));
			buttonDiffModLoader.UseVisualStyleBackColor = true;
			buttonDiffModLoader.Click += buttonTask_Click;
			// 
			// buttonPatchNitrate
			// 
			buttonPatchNitrate.Anchor = System.Windows.Forms.AnchorStyles.Top;
			buttonPatchNitrate.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			buttonPatchNitrate.Location = new System.Drawing.Point(210, 181);
			buttonPatchNitrate.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			buttonPatchNitrate.Name = "buttonPatchNitrate";
			buttonPatchNitrate.Size = new System.Drawing.Size(150, 27);
			buttonPatchNitrate.TabIndex = 8;
			buttonPatchNitrate.Text = "Patch Nitrate";
			toolTipButtons.SetToolTip(buttonPatchNitrate, "Apply Nitrate patches");
			buttonPatchNitrate.UseVisualStyleBackColor = true;
			buttonPatchNitrate.Click += buttonTask_Click;
			// 
			// buttonDiffNitrate
			// 
			buttonDiffNitrate.Anchor = System.Windows.Forms.AnchorStyles.Top;
			buttonDiffNitrate.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			buttonDiffNitrate.Location = new System.Drawing.Point(52, 181);
			buttonDiffNitrate.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			buttonDiffNitrate.Name = "buttonDiffNitrate";
			buttonDiffNitrate.Size = new System.Drawing.Size(150, 27);
			buttonDiffNitrate.TabIndex = 9;
			buttonDiffNitrate.Text = "Diff Nitrate";
			toolTipButtons.SetToolTip(buttonDiffNitrate, "Diff Nitrate patches");
			buttonDiffNitrate.UseVisualStyleBackColor = true;
			buttonDiffNitrate.Click += buttonTask_Click;
			// 
			// toolTipButtons
			// 
			toolTipButtons.AutomaticDelay = 200;
			toolTipButtons.AutoPopDelay = 0;
			toolTipButtons.InitialDelay = 200;
			toolTipButtons.ReshowDelay = 40;
			toolTipButtons.Popup += toolTipButtons_Popup;
			// 
			// buttonRegenSource
			// 
			buttonRegenSource.Anchor = System.Windows.Forms.AnchorStyles.Top;
			buttonRegenSource.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			buttonRegenSource.Location = new System.Drawing.Point(52, 216);
			buttonRegenSource.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			buttonRegenSource.Name = "buttonRegenSource";
			buttonRegenSource.Size = new System.Drawing.Size(308, 27);
			buttonRegenSource.TabIndex = 3;
			buttonRegenSource.Text = "Regenerate Source";
			toolTipButtons.SetToolTip(buttonRegenSource, "Regenerates all the source files\r\nUse this after pulling from the repo\r\nEquivalent to Setup without Decompile");
			buttonRegenSource.UseVisualStyleBackColor = true;
			buttonRegenSource.Click += buttonTask_Click;
			// 
			// buttonDiffTerrariaNetCore
			// 
			buttonDiffTerrariaNetCore.Anchor = System.Windows.Forms.AnchorStyles.Top;
			buttonDiffTerrariaNetCore.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			buttonDiffTerrariaNetCore.Location = new System.Drawing.Point(52, 116);
			buttonDiffTerrariaNetCore.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			buttonDiffTerrariaNetCore.Name = "buttonDiffTerrariaNetCore";
			buttonDiffTerrariaNetCore.Size = new System.Drawing.Size(150, 27);
			buttonDiffTerrariaNetCore.TabIndex = 4;
			buttonDiffTerrariaNetCore.Text = "Diff TerrariaNetCore";
			toolTipButtons.SetToolTip(buttonDiffTerrariaNetCore, "Recalculates the Terraria patches\r\nDiffs the src/Terraria directory\r\nUsed for fixing decompilation errors\r\n");
			buttonDiffTerrariaNetCore.UseVisualStyleBackColor = true;
			buttonDiffTerrariaNetCore.Click += buttonTask_Click;
			// 
			// buttonPatchTerrariaNetCore
			// 
			buttonPatchTerrariaNetCore.Anchor = System.Windows.Forms.AnchorStyles.Top;
			buttonPatchTerrariaNetCore.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			buttonPatchTerrariaNetCore.Location = new System.Drawing.Point(210, 116);
			buttonPatchTerrariaNetCore.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			buttonPatchTerrariaNetCore.Name = "buttonPatchTerrariaNetCore";
			buttonPatchTerrariaNetCore.Size = new System.Drawing.Size(150, 27);
			buttonPatchTerrariaNetCore.TabIndex = 5;
			buttonPatchTerrariaNetCore.Text = "Patch TerrariaNetCore";
			toolTipButtons.SetToolTip(buttonPatchTerrariaNetCore, "Applies patches to fix decompile errors\r\nLeaves functionality unchanged\r\nPatched source is located in src/Terraria");
			buttonPatchTerrariaNetCore.UseVisualStyleBackColor = true;
			buttonPatchTerrariaNetCore.Click += buttonTask_Click;
			// 
			// mainMenuStrip
			// 
			mainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { menuItemOptions, toolsToolStripMenuItem, patchToolStripMenuItem });
			mainMenuStrip.Location = new System.Drawing.Point(0, 0);
			mainMenuStrip.Name = "mainMenuStrip";
			mainMenuStrip.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
			mainMenuStrip.Size = new System.Drawing.Size(407, 24);
			mainMenuStrip.TabIndex = 9;
			mainMenuStrip.Text = "menuStrip1";
			mainMenuStrip.ItemClicked += mainMenuStrip_ItemClicked;
			// 
			// menuItemOptions
			// 
			menuItemOptions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { menuItemTerraria, menuItemTmlPath, resetTimeStampOptimizationsToolStripMenuItem, formatDecompiledOutputToolStripMenuItem });
			menuItemOptions.Name = "menuItemOptions";
			menuItemOptions.Size = new System.Drawing.Size(61, 20);
			menuItemOptions.Text = "Options";
			// 
			// menuItemTerraria
			// 
			menuItemTerraria.Name = "menuItemTerraria";
			menuItemTerraria.Size = new System.Drawing.Size(268, 22);
			menuItemTerraria.Text = "Select Terraria";
			menuItemTerraria.Click += menuItemTerraria_Click;
			// 
			// menuItemTmlPath
			// 
			menuItemTmlPath.Name = "menuItemTmlPath";
			menuItemTmlPath.Size = new System.Drawing.Size(268, 22);
			menuItemTmlPath.Text = "Select Custom TML Output Directory";
			menuItemTmlPath.Click += menuItemTmlPath_Click;
			// 
			// resetTimeStampOptimizationsToolStripMenuItem
			// 
			resetTimeStampOptimizationsToolStripMenuItem.Name = "resetTimeStampOptimizationsToolStripMenuItem";
			resetTimeStampOptimizationsToolStripMenuItem.Size = new System.Drawing.Size(268, 22);
			resetTimeStampOptimizationsToolStripMenuItem.Text = "Reset TimeStamp Optimizations";
			resetTimeStampOptimizationsToolStripMenuItem.Click += menuItemResetTimeStampOptmizations_Click;
			// 
			// formatDecompiledOutputToolStripMenuItem
			// 
			formatDecompiledOutputToolStripMenuItem.Name = "formatDecompiledOutputToolStripMenuItem";
			formatDecompiledOutputToolStripMenuItem.Size = new System.Drawing.Size(268, 22);
			formatDecompiledOutputToolStripMenuItem.Text = "Format Decompiled Output";
			formatDecompiledOutputToolStripMenuItem.Click += formatDecompiledOutputToolStripMenuItem_Click;
			// 
			// toolsToolStripMenuItem
			// 
			toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { decompileServerToolStripMenuItem, formatCodeToolStripMenuItem, hookGenToolStripMenuItem, simplifierToolStripMenuItem });
			toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
			toolsToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
			toolsToolStripMenuItem.Text = "Tools";
			// 
			// decompileServerToolStripMenuItem
			// 
			decompileServerToolStripMenuItem.Name = "decompileServerToolStripMenuItem";
			decompileServerToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
			decompileServerToolStripMenuItem.Text = "Decompile Server";
			decompileServerToolStripMenuItem.Click += menuItemDecompileServer_Click;
			// 
			// formatCodeToolStripMenuItem
			// 
			formatCodeToolStripMenuItem.Name = "formatCodeToolStripMenuItem";
			formatCodeToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
			formatCodeToolStripMenuItem.Text = "Formatter";
			formatCodeToolStripMenuItem.Click += menuItemFormatCode_Click;
			// 
			// hookGenToolStripMenuItem
			// 
			hookGenToolStripMenuItem.Name = "hookGenToolStripMenuItem";
			hookGenToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
			hookGenToolStripMenuItem.Text = "HookGen";
			hookGenToolStripMenuItem.Click += menuItemHookGen_Click;
			// 
			// simplifierToolStripMenuItem
			// 
			simplifierToolStripMenuItem.Name = "simplifierToolStripMenuItem";
			simplifierToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
			simplifierToolStripMenuItem.Text = "Simplifier";
			simplifierToolStripMenuItem.Click += simplifierToolStripMenuItem_Click;
			// 
			// patchToolStripMenuItem
			// 
			patchToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { exactToolStripMenuItem, offsetToolStripMenuItem, fuzzyToolStripMenuItem });
			patchToolStripMenuItem.Name = "patchToolStripMenuItem";
			patchToolStripMenuItem.Size = new System.Drawing.Size(49, 20);
			patchToolStripMenuItem.Text = "Patch";
			// 
			// exactToolStripMenuItem
			// 
			exactToolStripMenuItem.Name = "exactToolStripMenuItem";
			exactToolStripMenuItem.Size = new System.Drawing.Size(106, 22);
			exactToolStripMenuItem.Text = "Exact";
			exactToolStripMenuItem.Click += exactToolStripMenuItem_Click;
			// 
			// offsetToolStripMenuItem
			// 
			offsetToolStripMenuItem.Name = "offsetToolStripMenuItem";
			offsetToolStripMenuItem.Size = new System.Drawing.Size(106, 22);
			offsetToolStripMenuItem.Text = "Offset";
			offsetToolStripMenuItem.Click += offsetToolStripMenuItem_Click;
			// 
			// fuzzyToolStripMenuItem
			// 
			fuzzyToolStripMenuItem.Name = "fuzzyToolStripMenuItem";
			fuzzyToolStripMenuItem.Size = new System.Drawing.Size(106, 22);
			fuzzyToolStripMenuItem.Text = "Fuzzy";
			fuzzyToolStripMenuItem.Click += fuzzyToolStripMenuItem_Click;
			// 
			// labelWorkingDirectory
			// 
			labelWorkingDirectory.AutoSize = true;
			labelWorkingDirectory.Location = new System.Drawing.Point(12, 30);
			labelWorkingDirectory.Name = "labelWorkingDirectory";
			labelWorkingDirectory.Size = new System.Drawing.Size(131, 15);
			labelWorkingDirectory.TabIndex = 12;
			labelWorkingDirectory.Text = "Working Directory Here";
			// 
			// MainForm
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size(407, 526);
			Controls.Add(labelWorkingDirectory);
			Controls.Add(buttonPatchTerrariaNetCore);
			Controls.Add(buttonDiffTerrariaNetCore);
			Controls.Add(buttonDiffModLoader);
			Controls.Add(buttonPatchNitrate);
			Controls.Add(buttonDiffNitrate);
			Controls.Add(labelStatus);
			Controls.Add(buttonDiffTerraria);
			Controls.Add(buttonRegenSource);
			Controls.Add(buttonPatchModLoader);
			Controls.Add(buttonPatchTerraria);
			Controls.Add(progressBar);
			Controls.Add(buttonCancel);
			Controls.Add(buttonDecompile);
			Controls.Add(buttonSetup);
			Controls.Add(mainMenuStrip);
			Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
			MainMenuStrip = mainMenuStrip;
			Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			Name = "MainForm";
			Text = "tModLoader Dev Setup";
			mainMenuStrip.ResumeLayout(false);
			mainMenuStrip.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Button buttonSetup;
        private System.Windows.Forms.Button buttonDecompile;
        private System.Windows.Forms.Button buttonDiffTerraria;
        private System.Windows.Forms.Button buttonPatchTerraria;
        private System.Windows.Forms.Button buttonPatchModLoader;
        private System.Windows.Forms.Button buttonPatchNitrate;
        private System.Windows.Forms.Button buttonDiffModLoader;
        private System.Windows.Forms.Button buttonDiffNitrate;
        private System.Windows.Forms.ToolTip toolTipButtons;
        private System.Windows.Forms.MenuStrip mainMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem menuItemOptions;
        private System.Windows.Forms.ToolStripMenuItem menuItemTerraria;
		private System.Windows.Forms.ToolStripMenuItem resetTimeStampOptimizationsToolStripMenuItem;
        private System.Windows.Forms.Button buttonRegenSource;
		private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem decompileServerToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem formatCodeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem hookGenToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem patchToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exactToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem offsetToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem fuzzyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem simplifierToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem formatDecompiledOutputToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem menuItemTmlPath;
		private System.Windows.Forms.Button buttonDiffTerrariaNetCore;
		private System.Windows.Forms.Button buttonPatchTerrariaNetCore;
		private System.Windows.Forms.Label labelWorkingDirectory;
	}
}

