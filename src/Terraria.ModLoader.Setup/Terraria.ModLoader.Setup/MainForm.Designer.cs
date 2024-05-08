using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Terraria.ModLoader.Setup
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
			components = new Container();
			var resources = new ComponentResourceManager(typeof(MainForm));
			buttonCancel = new Button();
			progressBar = new ProgressBar();
			labelStatus = new Label();
			buttonSetup = new Button();
			buttonDecompile = new Button();
			buttonDiffTerraria = new Button();
			buttonPatchTerraria = new Button();
			buttonPatchModLoader = new Button();
			buttonDiffModLoader = new Button();
			buttonPatchNitrate = new Button();
			buttonDiffNitrate = new Button();
			toolTipButtons = new ToolTip(components);
			buttonRegenerateSource = new Button();
			buttonDiffTerrariaNetCore = new Button();
			buttonPatchTerrariaNetCore = new Button();
			mainMenuStrip = new MenuStrip();
			menuItemOptions = new ToolStripMenuItem();
			menuItemTerraria = new ToolStripMenuItem();
			menuItemTmlPath = new ToolStripMenuItem();
			formatDecompiledOutputToolStripMenuItem = new ToolStripMenuItem();
			toolsToolStripMenuItem = new ToolStripMenuItem();
			decompileServerToolStripMenuItem = new ToolStripMenuItem();
			formatCodeToolStripMenuItem = new ToolStripMenuItem();
			hookGenToolStripMenuItem = new ToolStripMenuItem();
			simplifierToolStripMenuItem = new ToolStripMenuItem();
			patchToolStripMenuItem = new ToolStripMenuItem();
			exactToolStripMenuItem = new ToolStripMenuItem();
			offsetToolStripMenuItem = new ToolStripMenuItem();
			fuzzyToolStripMenuItem = new ToolStripMenuItem();
			labelWorkingDirectoryDisplay = new Label();
			labelWorkingDirectory = new Label();
			mainMenuStrip.SuspendLayout();
			SuspendLayout();
			// 
			// buttonCancel
			// 
			buttonCancel.Anchor = AnchorStyles.Bottom;
			buttonCancel.DialogResult = DialogResult.Cancel;
			buttonCancel.Enabled = false;
			buttonCancel.Location = new Point(145, 526);
			buttonCancel.Margin = new Padding(4, 3, 4, 3);
			buttonCancel.Name = "buttonCancel";
			buttonCancel.Size = new Size(96, 27);
			buttonCancel.TabIndex = 14;
			buttonCancel.Text = "Cancel";
			buttonCancel.UseVisualStyleBackColor = true;
			buttonCancel.Click += buttonCancel_Click;
			// 
			// progressBar
			// 
			progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			progressBar.Location = new Point(14, 493);
			progressBar.Margin = new Padding(4, 3, 4, 3);
			progressBar.Name = "progressBar";
			progressBar.Size = new Size(356, 27);
			progressBar.TabIndex = 13;
			// 
			// labelStatus
			// 
			labelStatus.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
			labelStatus.BorderStyle = BorderStyle.Fixed3D;
			labelStatus.Location = new Point(13, 253);
			labelStatus.Margin = new Padding(4, 0, 4, 0);
			labelStatus.Name = "labelStatus";
			labelStatus.Size = new Size(356, 228);
			labelStatus.TabIndex = 12;
			labelStatus.TextAlign = ContentAlignment.BottomLeft;
			// 
			// buttonSetup
			// 
			buttonSetup.Anchor = AnchorStyles.Top;
			buttonSetup.Location = new Point(38, 49);
			buttonSetup.Margin = new Padding(4, 3, 4, 3);
			buttonSetup.Name = "buttonSetup";
			buttonSetup.Size = new Size(150, 26);
			buttonSetup.TabIndex = 1;
			buttonSetup.Text = "Setup";
			toolTipButtons.SetToolTip(buttonSetup, "Complete environment setup for working on tModLoader source\r\nEquivalent to Decompile+Patch+SetupDebug\r\nEdit the source in src/staging/tModLoader then run Diff tModLoader and commit the /patches folder");
			buttonSetup.UseVisualStyleBackColor = true;
			buttonSetup.Click += buttonTask_Click;
			// 
			// buttonDecompile
			// 
			buttonDecompile.Anchor = AnchorStyles.Top;
			buttonDecompile.DialogResult = DialogResult.Cancel;
			buttonDecompile.Location = new Point(196, 49);
			buttonDecompile.Margin = new Padding(4, 3, 4, 3);
			buttonDecompile.Name = "buttonDecompile";
			buttonDecompile.Size = new Size(150, 26);
			buttonDecompile.TabIndex = 2;
			buttonDecompile.Text = "Decompile";
			toolTipButtons.SetToolTip(buttonDecompile, "Uses ILSpy to decompile Terraria\r\nAlso decompiles server classes not included in the client binary\r\nOutputs to src/staging/decompiled");
			buttonDecompile.UseVisualStyleBackColor = true;
			buttonDecompile.Click += buttonTask_Click;
			// 
			// buttonDiffTerraria
			// 
			buttonDiffTerraria.Anchor = AnchorStyles.Top;
			buttonDiffTerraria.DialogResult = DialogResult.Cancel;
			buttonDiffTerraria.Location = new Point(38, 117);
			buttonDiffTerraria.Margin = new Padding(4, 3, 4, 3);
			buttonDiffTerraria.Name = "buttonDiffTerraria";
			buttonDiffTerraria.Size = new Size(150, 26);
			buttonDiffTerraria.TabIndex = 4;
			buttonDiffTerraria.Text = "Diff Terraria";
			toolTipButtons.SetToolTip(buttonDiffTerraria, "Recalculates the Terraria patches\r\nDiffs the src/staging/Terraria directory\r\nUsed for fixing decompilation errors\r\n");
			buttonDiffTerraria.UseVisualStyleBackColor = true;
			buttonDiffTerraria.Click += buttonTask_Click;
			// 
			// buttonPatchTerraria
			// 
			buttonPatchTerraria.Anchor = AnchorStyles.Top;
			buttonPatchTerraria.DialogResult = DialogResult.Cancel;
			buttonPatchTerraria.Location = new Point(196, 117);
			buttonPatchTerraria.Margin = new Padding(4, 3, 4, 3);
			buttonPatchTerraria.Name = "buttonPatchTerraria";
			buttonPatchTerraria.Size = new Size(150, 26);
			buttonPatchTerraria.TabIndex = 5;
			buttonPatchTerraria.Text = "Patch Terraria";
			toolTipButtons.SetToolTip(buttonPatchTerraria, "Applies patches to fix decompile errors\r\nLeaves functionality unchanged\r\nPatched source is located in src/staging/Terraria");
			buttonPatchTerraria.UseVisualStyleBackColor = true;
			buttonPatchTerraria.Click += buttonTask_Click;
			// 
			// buttonPatchModLoader
			// 
			buttonPatchModLoader.Anchor = AnchorStyles.Top;
			buttonPatchModLoader.DialogResult = DialogResult.Cancel;
			buttonPatchModLoader.Location = new Point(196, 183);
			buttonPatchModLoader.Margin = new Padding(4, 3, 4, 3);
			buttonPatchModLoader.Name = "buttonPatchModLoader";
			buttonPatchModLoader.Size = new Size(150, 26);
			buttonPatchModLoader.TabIndex = 9;
			buttonPatchModLoader.Text = "Patch tModLoader";
			toolTipButtons.SetToolTip(buttonPatchModLoader, "Applies tModLoader patches to Terraria\r\nEdit the source code in src/staging/tModLoader after this phase\r\nInternally formats the Terraria sources before patching");
			buttonPatchModLoader.UseVisualStyleBackColor = true;
			buttonPatchModLoader.Click += buttonTask_Click;
			// 
			// buttonDiffModLoader
			// 
			buttonDiffModLoader.Anchor = AnchorStyles.Top;
			buttonDiffModLoader.DialogResult = DialogResult.Cancel;
			buttonDiffModLoader.Location = new Point(38, 183);
			buttonDiffModLoader.Margin = new Padding(4, 3, 4, 3);
			buttonDiffModLoader.Name = "buttonDiffModLoader";
			buttonDiffModLoader.Size = new Size(150, 26);
			buttonDiffModLoader.TabIndex = 8;
			buttonDiffModLoader.Text = "Diff tModLoader";
			toolTipButtons.SetToolTip(buttonDiffModLoader, resources.GetString("buttonDiffModLoader.ToolTip"));
			buttonDiffModLoader.UseVisualStyleBackColor = true;
			buttonDiffModLoader.Click += buttonTask_Click;
			// 
			// buttonPatchNitrate
			// 
			buttonPatchNitrate.Anchor = AnchorStyles.Top;
			buttonPatchNitrate.DialogResult = DialogResult.Cancel;
			buttonPatchNitrate.Location = new Point(196, 215);
			buttonPatchNitrate.Margin = new Padding(4, 3, 4, 3);
			buttonPatchNitrate.Name = "buttonPatchNitrate";
			buttonPatchNitrate.Size = new Size(150, 26);
			buttonPatchNitrate.TabIndex = 11;
			buttonPatchNitrate.Text = "Patch Nitrate";
			toolTipButtons.SetToolTip(buttonPatchNitrate, "Apply Nitrate patches");
			buttonPatchNitrate.UseVisualStyleBackColor = true;
			buttonPatchNitrate.Click += buttonTask_Click;
			// 
			// buttonDiffNitrate
			// 
			buttonDiffNitrate.Anchor = AnchorStyles.Top;
			buttonDiffNitrate.DialogResult = DialogResult.Cancel;
			buttonDiffNitrate.Location = new Point(38, 215);
			buttonDiffNitrate.Margin = new Padding(4, 3, 4, 3);
			buttonDiffNitrate.Name = "buttonDiffNitrate";
			buttonDiffNitrate.Size = new Size(150, 26);
			buttonDiffNitrate.TabIndex = 10;
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
			// buttonRegenerateSource
			// 
			buttonRegenerateSource.Anchor = AnchorStyles.Top;
			buttonRegenerateSource.DialogResult = DialogResult.Cancel;
			buttonRegenerateSource.Location = new Point(38, 81);
			buttonRegenerateSource.Margin = new Padding(4, 3, 4, 3);
			buttonRegenerateSource.Name = "buttonRegenerateSource";
			buttonRegenerateSource.Size = new Size(308, 27);
			buttonRegenerateSource.TabIndex = 3;
			buttonRegenerateSource.Text = "Regenerate Source";
			toolTipButtons.SetToolTip(buttonRegenerateSource, "Regenerates all the source files\r\nUse this after pulling from the repo\r\nEquivalent to Setup without Decompile");
			buttonRegenerateSource.UseVisualStyleBackColor = true;
			buttonRegenerateSource.Click += buttonTask_Click;
			// 
			// buttonDiffTerrariaNetCore
			// 
			buttonDiffTerrariaNetCore.Anchor = AnchorStyles.Top;
			buttonDiffTerrariaNetCore.DialogResult = DialogResult.Cancel;
			buttonDiffTerrariaNetCore.Location = new Point(38, 150);
			buttonDiffTerrariaNetCore.Margin = new Padding(4, 3, 4, 3);
			buttonDiffTerrariaNetCore.Name = "buttonDiffTerrariaNetCore";
			buttonDiffTerrariaNetCore.Size = new Size(150, 26);
			buttonDiffTerrariaNetCore.TabIndex = 6;
			buttonDiffTerrariaNetCore.Text = "Diff TerrariaNetCore";
			toolTipButtons.SetToolTip(buttonDiffTerrariaNetCore, "Recalculates the Terraria patches\r\nDiffs the src/staging/Terraria directory\r\nUsed for fixing decompilation errors\r\n");
			buttonDiffTerrariaNetCore.UseVisualStyleBackColor = true;
			buttonDiffTerrariaNetCore.Click += buttonTask_Click;
			// 
			// buttonPatchTerrariaNetCore
			// 
			buttonPatchTerrariaNetCore.Anchor = AnchorStyles.Top;
			buttonPatchTerrariaNetCore.DialogResult = DialogResult.Cancel;
			buttonPatchTerrariaNetCore.Location = new Point(196, 150);
			buttonPatchTerrariaNetCore.Margin = new Padding(4, 3, 4, 3);
			buttonPatchTerrariaNetCore.Name = "buttonPatchTerrariaNetCore";
			buttonPatchTerrariaNetCore.Size = new Size(150, 26);
			buttonPatchTerrariaNetCore.TabIndex = 7;
			buttonPatchTerrariaNetCore.Text = "Patch TerrariaNetCore";
			toolTipButtons.SetToolTip(buttonPatchTerrariaNetCore, "Applies patches to fix decompile errors\r\nLeaves functionality unchanged\r\nPatched source is located in src/staging/Terraria");
			buttonPatchTerrariaNetCore.UseVisualStyleBackColor = true;
			buttonPatchTerrariaNetCore.Click += buttonTask_Click;
			// 
			// mainMenuStrip
			// 
			mainMenuStrip.Items.AddRange(new ToolStripItem[] { menuItemOptions, toolsToolStripMenuItem, patchToolStripMenuItem });
			mainMenuStrip.Location = new Point(0, 0);
			mainMenuStrip.Name = "mainMenuStrip";
			mainMenuStrip.Padding = new Padding(7, 2, 0, 2);
			mainMenuStrip.Size = new Size(384, 24);
			mainMenuStrip.TabIndex = 9;
			mainMenuStrip.Text = "menuStrip1";
			mainMenuStrip.ItemClicked += mainMenuStrip_ItemClicked;
			// 
			// menuItemOptions
			// 
			menuItemOptions.DropDownItems.AddRange(new ToolStripItem[] { menuItemTerraria, menuItemTmlPath, formatDecompiledOutputToolStripMenuItem });
			menuItemOptions.Name = "menuItemOptions";
			menuItemOptions.Size = new Size(61, 20);
			menuItemOptions.Text = "Options";
			// 
			// menuItemTerraria
			// 
			menuItemTerraria.Name = "menuItemTerraria";
			menuItemTerraria.Size = new Size(268, 22);
			menuItemTerraria.Text = "Select Terraria";
			menuItemTerraria.Click += menuItemTerraria_Click;
			// 
			// menuItemTmlPath
			// 
			menuItemTmlPath.Name = "menuItemTmlPath";
			menuItemTmlPath.Size = new Size(268, 22);
			menuItemTmlPath.Text = "Select Custom TML Output Directory";
			menuItemTmlPath.Click += menuItemTmlPath_Click;
			// 
			// formatDecompiledOutputToolStripMenuItem
			// 
			formatDecompiledOutputToolStripMenuItem.Name = "formatDecompiledOutputToolStripMenuItem";
			formatDecompiledOutputToolStripMenuItem.Size = new Size(268, 22);
			formatDecompiledOutputToolStripMenuItem.Text = "Format Decompiled Output";
			formatDecompiledOutputToolStripMenuItem.Click += formatDecompiledOutputToolStripMenuItem_Click;
			// 
			// toolsToolStripMenuItem
			// 
			toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { decompileServerToolStripMenuItem, formatCodeToolStripMenuItem, hookGenToolStripMenuItem, simplifierToolStripMenuItem });
			toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
			toolsToolStripMenuItem.Size = new Size(46, 20);
			toolsToolStripMenuItem.Text = "Tools";
			// 
			// decompileServerToolStripMenuItem
			// 
			decompileServerToolStripMenuItem.Name = "decompileServerToolStripMenuItem";
			decompileServerToolStripMenuItem.Size = new Size(166, 22);
			decompileServerToolStripMenuItem.Text = "Decompile Server";
			decompileServerToolStripMenuItem.Click += menuItemDecompileServer_Click;
			// 
			// formatCodeToolStripMenuItem
			// 
			formatCodeToolStripMenuItem.Name = "formatCodeToolStripMenuItem";
			formatCodeToolStripMenuItem.Size = new Size(166, 22);
			formatCodeToolStripMenuItem.Text = "Formatter";
			formatCodeToolStripMenuItem.Click += menuItemFormatCode_Click;
			// 
			// hookGenToolStripMenuItem
			// 
			hookGenToolStripMenuItem.Name = "hookGenToolStripMenuItem";
			hookGenToolStripMenuItem.Size = new Size(166, 22);
			hookGenToolStripMenuItem.Text = "HookGen";
			hookGenToolStripMenuItem.Click += menuItemHookGen_Click;
			// 
			// simplifierToolStripMenuItem
			// 
			simplifierToolStripMenuItem.Name = "simplifierToolStripMenuItem";
			simplifierToolStripMenuItem.Size = new Size(166, 22);
			simplifierToolStripMenuItem.Text = "Simplifier";
			simplifierToolStripMenuItem.Click += simplifierToolStripMenuItem_Click;
			// 
			// patchToolStripMenuItem
			// 
			patchToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { exactToolStripMenuItem, offsetToolStripMenuItem, fuzzyToolStripMenuItem });
			patchToolStripMenuItem.Name = "patchToolStripMenuItem";
			patchToolStripMenuItem.Size = new Size(49, 20);
			patchToolStripMenuItem.Text = "Patch";
			// 
			// exactToolStripMenuItem
			// 
			exactToolStripMenuItem.Name = "exactToolStripMenuItem";
			exactToolStripMenuItem.Size = new Size(106, 22);
			exactToolStripMenuItem.Text = "Exact";
			exactToolStripMenuItem.Click += exactToolStripMenuItem_Click;
			// 
			// offsetToolStripMenuItem
			// 
			offsetToolStripMenuItem.Name = "offsetToolStripMenuItem";
			offsetToolStripMenuItem.Size = new Size(106, 22);
			offsetToolStripMenuItem.Text = "Offset";
			offsetToolStripMenuItem.Click += offsetToolStripMenuItem_Click;
			// 
			// fuzzyToolStripMenuItem
			// 
			fuzzyToolStripMenuItem.Name = "fuzzyToolStripMenuItem";
			fuzzyToolStripMenuItem.Size = new Size(106, 22);
			fuzzyToolStripMenuItem.Text = "Fuzzy";
			fuzzyToolStripMenuItem.Click += fuzzyToolStripMenuItem_Click;
			// 
			// labelWorkingDirectoryDisplay
			// 
			labelWorkingDirectoryDisplay.AutoEllipsis = true;
			labelWorkingDirectoryDisplay.BorderStyle = BorderStyle.Fixed3D;
			labelWorkingDirectoryDisplay.Location = new Point(119, 24);
			labelWorkingDirectoryDisplay.Name = "labelWorkingDirectoryDisplay";
			labelWorkingDirectoryDisplay.Size = new Size(251, 18);
			labelWorkingDirectoryDisplay.TabIndex = 0;
			labelWorkingDirectoryDisplay.Text = "Working Directory Here";
			// 
			// labelWorkingDirectory
			// 
			labelWorkingDirectory.AutoSize = true;
			labelWorkingDirectory.Location = new Point(12, 24);
			labelWorkingDirectory.Name = "labelWorkingDirectory";
			labelWorkingDirectory.Size = new Size(106, 15);
			labelWorkingDirectory.TabIndex = 13;
			labelWorkingDirectory.Text = "Working Directory:";
			// 
			// MainForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(384, 561);
			Controls.Add(labelWorkingDirectory);
			Controls.Add(labelWorkingDirectoryDisplay);
			Controls.Add(buttonPatchTerrariaNetCore);
			Controls.Add(buttonDiffTerrariaNetCore);
			Controls.Add(buttonDiffModLoader);
			Controls.Add(buttonPatchNitrate);
			Controls.Add(buttonDiffNitrate);
			Controls.Add(labelStatus);
			Controls.Add(buttonDiffTerraria);
			Controls.Add(buttonRegenerateSource);
			Controls.Add(buttonPatchModLoader);
			Controls.Add(buttonPatchTerraria);
			Controls.Add(progressBar);
			Controls.Add(buttonCancel);
			Controls.Add(buttonDecompile);
			Controls.Add(buttonSetup);
			Controls.Add(mainMenuStrip);
			Icon = (Icon)resources.GetObject("$this.Icon");
			MainMenuStrip = mainMenuStrip;
			Margin = new Padding(4, 3, 4, 3);
			Name = "MainForm";
			Text = "Nitrate Dev Setup";
			mainMenuStrip.ResumeLayout(false);
			mainMenuStrip.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Button buttonCancel;
        private ProgressBar progressBar;
        private Label labelStatus;
        private Button buttonSetup;
        private Button buttonDecompile;
        private Button buttonDiffTerraria;
        private Button buttonPatchTerraria;
        private Button buttonPatchModLoader;
        private Button buttonPatchNitrate;
        private Button buttonDiffModLoader;
        private Button buttonDiffNitrate;
        private ToolTip toolTipButtons;
        private MenuStrip mainMenuStrip;
        private ToolStripMenuItem menuItemOptions;
        private ToolStripMenuItem menuItemTerraria;
        private Button buttonRegenerateSource;
		private ToolStripMenuItem toolsToolStripMenuItem;
		private ToolStripMenuItem decompileServerToolStripMenuItem;
		private ToolStripMenuItem formatCodeToolStripMenuItem;
		private ToolStripMenuItem hookGenToolStripMenuItem;
		private ToolStripMenuItem patchToolStripMenuItem;
		private ToolStripMenuItem exactToolStripMenuItem;
		private ToolStripMenuItem offsetToolStripMenuItem;
		private ToolStripMenuItem fuzzyToolStripMenuItem;
		private ToolStripMenuItem simplifierToolStripMenuItem;
		private ToolStripMenuItem formatDecompiledOutputToolStripMenuItem;
		private ToolStripMenuItem menuItemTmlPath;
		private Button buttonDiffTerrariaNetCore;
		private Button buttonPatchTerrariaNetCore;
		private Label labelWorkingDirectoryDisplay;
		private Label labelWorkingDirectory;
	}
}

