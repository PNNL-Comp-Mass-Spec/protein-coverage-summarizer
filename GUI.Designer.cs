using System.ComponentModel;
using System.Windows.Forms;

namespace ProteinCoverageSummarizerGUI
{
    partial class GUI
    {
        // Ignore Spelling: txt, mnu, fra, cmd, chk, cbo, dg, lbl, rtf, Sql, Tol

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            this.components = new System.ComponentModel.Container();
            this.fraProteinInputFilePath = new System.Windows.Forms.GroupBox();
            this.cmdProteinSelectFile = new System.Windows.Forms.Button();
            this.txtProteinInputFilePath = new System.Windows.Forms.TextBox();
            this.MainMenuControl = new System.Windows.Forms.MainMenu(this.components);
            this.mnuFile = new System.Windows.Forms.MenuItem();
            this.mnuFileSelectInputFile = new System.Windows.Forms.MenuItem();
            this.mnuPeptideInputFile = new System.Windows.Forms.MenuItem();
            this.mnuFileSelectOutputFile = new System.Windows.Forms.MenuItem();
            this.mnuFileSep1 = new System.Windows.Forms.MenuItem();
            this.mnuFileLoadOptions = new System.Windows.Forms.MenuItem();
            this.mnuFileSaveDefaultOptions = new System.Windows.Forms.MenuItem();
            this.mnuFileSep2 = new System.Windows.Forms.MenuItem();
            this.mnuFileExit = new System.Windows.Forms.MenuItem();
            this.mnuEdit = new System.Windows.Forms.MenuItem();
            this.mnuEditShowRTF = new System.Windows.Forms.MenuItem();
            this.mnuEditResetOptions = new System.Windows.Forms.MenuItem();
            this.mnuHelp = new System.Windows.Forms.MenuItem();
            this.mnuHelpAbout = new System.Windows.Forms.MenuItem();
            this.fraPeptideInputFilePath = new System.Windows.Forms.GroupBox();
            this.cmdPeptideSelectFile = new System.Windows.Forms.Button();
            this.txtPeptideInputFilePath = new System.Windows.Forms.TextBox();
            this.fraProcessingOptions = new System.Windows.Forms.GroupBox();
            this.fraMassCalculationOptions = new System.Windows.Forms.GroupBox();
            this.fraDigestionOptions = new System.Windows.Forms.GroupBox();
            this.txtMinimumSLiCScore = new System.Windows.Forms.TextBox();
            this.fraPeakMatchingOptions = new System.Windows.Forms.GroupBox();
            this.fraSqlServerOptions = new System.Windows.Forms.GroupBox();
            this.fraUniquenessBinningOptions = new System.Windows.Forms.GroupBox();
            this.cmdPastePMThresholdsList = new System.Windows.Forms.Button();
            this.cboPMPredefinedThresholds = new System.Windows.Forms.ComboBox();
            this.cmdPMThresholdsAutoPopulate = new System.Windows.Forms.Button();
            this.cmdClearPMThresholdsList = new System.Windows.Forms.Button();
            this.cboMassTolType = new System.Windows.Forms.ComboBox();
            this.tbsOptions = new System.Windows.Forms.TabControl();
            this.TabPageFileFormatOptions = new System.Windows.Forms.TabPage();
            this.lblStatus = new System.Windows.Forms.Label();
            this.cmdExit = new System.Windows.Forms.Button();
            this.cmdStart = new System.Windows.Forms.Button();
            this.cmdAbort = new System.Windows.Forms.Button();
            this.lblProgress = new System.Windows.Forms.Label();
            this.fraOptions = new System.Windows.Forms.GroupBox();
            this.chkIgnoreILDifferences = new System.Windows.Forms.CheckBox();
            this.chkMatchPeptidePrefixAndSuffixToProtein = new System.Windows.Forms.CheckBox();
            this.chkSearchAllProteinsSkipCoverageComputationSteps = new System.Windows.Forms.CheckBox();
            this.chkSaveProteinToPeptideMappingFile = new System.Windows.Forms.CheckBox();
            this.chkSaveSourceDataPlusProteinsFile = new System.Windows.Forms.CheckBox();
            this.chkSearchAllProteinsForPeptideSequence = new System.Windows.Forms.CheckBox();
            this.chkOutputProteinSequence = new System.Windows.Forms.CheckBox();
            this.chkTrackPeptideCounts = new System.Windows.Forms.CheckBox();
            this.chkRemoveSymbolCharacters = new System.Windows.Forms.CheckBox();
            this.fraPeptideDelimitedFileOptions = new System.Windows.Forms.GroupBox();
            this.cboPeptideInputFileColumnOrdering = new System.Windows.Forms.ComboBox();
            this.Label1 = new System.Windows.Forms.Label();
            this.chkPeptideFileSkipFirstLine = new System.Windows.Forms.CheckBox();
            this.txtPeptideInputFileColumnDelimiter = new System.Windows.Forms.TextBox();
            this.lblPeptideInputFileColumnDelimiter = new System.Windows.Forms.Label();
            this.cboPeptideInputFileColumnDelimiter = new System.Windows.Forms.ComboBox();
            this.lblInputFileNotes = new System.Windows.Forms.Label();
            this.fraProteinDelimitedFileOptions = new System.Windows.Forms.GroupBox();
            this.chkProteinFileSkipFirstLine = new System.Windows.Forms.CheckBox();
            this.cboProteinInputFileColumnOrdering = new System.Windows.Forms.ComboBox();
            this.lblProteinInputFileColumnOrdering = new System.Windows.Forms.Label();
            this.txtProteinInputFileColumnDelimiter = new System.Windows.Forms.TextBox();
            this.lblProteinInputFileColumnDelimiter = new System.Windows.Forms.Label();
            this.cboProteinInputFileColumnDelimiter = new System.Windows.Forms.ComboBox();
            this.TabPagePeakMatchingThresholds = new System.Windows.Forms.TabPage();
            this.txtCoverage = new System.Windows.Forms.TextBox();
            this.txtRTFCode = new System.Windows.Forms.TextBox();
            this.txtCustomProteinSequence = new System.Windows.Forms.TextBox();
            this.lblCustomProteinSequence = new System.Windows.Forms.Label();
            this.chkAddSpace = new System.Windows.Forms.CheckBox();
            this.cboCharactersPerLine = new System.Windows.Forms.ComboBox();
            this.rtfRichTextBox = new System.Windows.Forms.RichTextBox();
            this.dgResults = new System.Windows.Forms.DataGrid();
            this.fraOutputFolderPath = new System.Windows.Forms.GroupBox();
            this.cmdSelectOutputFolder = new System.Windows.Forms.Button();
            this.txtOutputFolderPath = new System.Windows.Forms.TextBox();
            this.fraProteinInputFilePath.SuspendLayout();
            this.fraPeptideInputFilePath.SuspendLayout();
            this.tbsOptions.SuspendLayout();
            this.TabPageFileFormatOptions.SuspendLayout();
            this.fraOptions.SuspendLayout();
            this.fraPeptideDelimitedFileOptions.SuspendLayout();
            this.fraProteinDelimitedFileOptions.SuspendLayout();
            this.TabPagePeakMatchingThresholds.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgResults)).BeginInit();
            this.fraOutputFolderPath.SuspendLayout();
            this.SuspendLayout();
            // 
            // fraProteinInputFilePath
            // 
            this.fraProteinInputFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fraProteinInputFilePath.Controls.Add(this.cmdProteinSelectFile);
            this.fraProteinInputFilePath.Controls.Add(this.txtProteinInputFilePath);
            this.fraProteinInputFilePath.Location = new System.Drawing.Point(10, 18);
            this.fraProteinInputFilePath.Name = "fraProteinInputFilePath";
            this.fraProteinInputFilePath.Size = new System.Drawing.Size(1001, 56);
            this.fraProteinInputFilePath.TabIndex = 0;
            this.fraProteinInputFilePath.TabStop = false;
            this.fraProteinInputFilePath.Text = "Protein Input File Path (FASTA or Tab-delimited)";
            // 
            // cmdProteinSelectFile
            // 
            this.cmdProteinSelectFile.Location = new System.Drawing.Point(10, 18);
            this.cmdProteinSelectFile.Name = "cmdProteinSelectFile";
            this.cmdProteinSelectFile.Size = new System.Drawing.Size(96, 28);
            this.cmdProteinSelectFile.TabIndex = 0;
            this.cmdProteinSelectFile.Text = "Select file";
            this.cmdProteinSelectFile.Click += new System.EventHandler(this.cmdProteinSelectFile_Click);
            // 
            // txtProteinInputFilePath
            // 
            this.txtProteinInputFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProteinInputFilePath.Location = new System.Drawing.Point(125, 21);
            this.txtProteinInputFilePath.Name = "txtProteinInputFilePath";
            this.txtProteinInputFilePath.Size = new System.Drawing.Size(857, 22);
            this.txtProteinInputFilePath.TabIndex = 1;
            this.txtProteinInputFilePath.TextChanged += new System.EventHandler(this.txtProteinInputFilePath_TextChanged);
            this.txtProteinInputFilePath.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtProteinInputFilePath_KeyPress);
            // 
            // MainMenuControl
            // 
            this.MainMenuControl.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuFile,
            this.mnuEdit,
            this.mnuHelp});
            // 
            // mnuFile
            // 
            this.mnuFile.Index = 0;
            this.mnuFile.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuFileSelectInputFile,
            this.mnuPeptideInputFile,
            this.mnuFileSelectOutputFile,
            this.mnuFileSep1,
            this.mnuFileLoadOptions,
            this.mnuFileSaveDefaultOptions,
            this.mnuFileSep2,
            this.mnuFileExit});
            this.mnuFile.Text = "&File";
            // 
            // mnuFileSelectInputFile
            // 
            this.mnuFileSelectInputFile.Index = 0;
            this.mnuFileSelectInputFile.Text = "Select Protein &Input File...";
            this.mnuFileSelectInputFile.Click += new System.EventHandler(this.mnuFileSelectInputFile_Click);
            // 
            // mnuPeptideInputFile
            // 
            this.mnuPeptideInputFile.Index = 1;
            this.mnuPeptideInputFile.Text = "Select Peptide I&nput File...";
            this.mnuPeptideInputFile.Click += new System.EventHandler(this.mnuPeptideInputFile_Click);
            // 
            // mnuFileSelectOutputFile
            // 
            this.mnuFileSelectOutputFile.Index = 2;
            this.mnuFileSelectOutputFile.Text = "Select &Output File...";
            this.mnuFileSelectOutputFile.Click += new System.EventHandler(this.mnuFileSelectOutputFile_Click);
            // 
            // mnuFileSep1
            // 
            this.mnuFileSep1.Index = 3;
            this.mnuFileSep1.Text = "-";
            // 
            // mnuFileLoadOptions
            // 
            this.mnuFileLoadOptions.Index = 4;
            this.mnuFileLoadOptions.Text = "Load Options ...";
            this.mnuFileLoadOptions.Click += new System.EventHandler(this.mnuFileLoadOptions_Click);
            // 
            // mnuFileSaveDefaultOptions
            // 
            this.mnuFileSaveDefaultOptions.Index = 5;
            this.mnuFileSaveDefaultOptions.Text = "Save &Default Options";
            this.mnuFileSaveDefaultOptions.Click += new System.EventHandler(this.mnuFileSaveDefaultOptions_Click);
            // 
            // mnuFileSep2
            // 
            this.mnuFileSep2.Index = 6;
            this.mnuFileSep2.Text = "-";
            // 
            // mnuFileExit
            // 
            this.mnuFileExit.Index = 7;
            this.mnuFileExit.Text = "E&xit";
            this.mnuFileExit.Click += new System.EventHandler(this.mnuFileExit_Click);
            // 
            // mnuEdit
            // 
            this.mnuEdit.Index = 1;
            this.mnuEdit.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuEditShowRTF,
            this.mnuEditResetOptions});
            this.mnuEdit.Text = "&Edit";
            // 
            // mnuEditShowRTF
            // 
            this.mnuEditShowRTF.Index = 0;
            this.mnuEditShowRTF.Text = "Show RTF Code";
            this.mnuEditShowRTF.Click += new System.EventHandler(this.mnuEditShowRTF_Click);
            // 
            // mnuEditResetOptions
            // 
            this.mnuEditResetOptions.Index = 1;
            this.mnuEditResetOptions.Text = "&Reset options to Defaults";
            this.mnuEditResetOptions.Click += new System.EventHandler(this.mnuEditResetOptions_Click);
            // 
            // mnuHelp
            // 
            this.mnuHelp.Index = 2;
            this.mnuHelp.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mnuHelpAbout});
            this.mnuHelp.Text = "&Help";
            // 
            // mnuHelpAbout
            // 
            this.mnuHelpAbout.Index = 0;
            this.mnuHelpAbout.Text = "&About";
            this.mnuHelpAbout.Click += new System.EventHandler(this.mnuHelpAbout_Click);
            // 
            // fraPeptideInputFilePath
            // 
            this.fraPeptideInputFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fraPeptideInputFilePath.Controls.Add(this.cmdPeptideSelectFile);
            this.fraPeptideInputFilePath.Controls.Add(this.txtPeptideInputFilePath);
            this.fraPeptideInputFilePath.Location = new System.Drawing.Point(10, 83);
            this.fraPeptideInputFilePath.Name = "fraPeptideInputFilePath";
            this.fraPeptideInputFilePath.Size = new System.Drawing.Size(1001, 55);
            this.fraPeptideInputFilePath.TabIndex = 1;
            this.fraPeptideInputFilePath.TabStop = false;
            this.fraPeptideInputFilePath.Text = "Peptide Input File Path (Tab-delimited)";
            // 
            // cmdPeptideSelectFile
            // 
            this.cmdPeptideSelectFile.Location = new System.Drawing.Point(10, 18);
            this.cmdPeptideSelectFile.Name = "cmdPeptideSelectFile";
            this.cmdPeptideSelectFile.Size = new System.Drawing.Size(96, 28);
            this.cmdPeptideSelectFile.TabIndex = 0;
            this.cmdPeptideSelectFile.Text = "Select file";
            this.cmdPeptideSelectFile.Click += new System.EventHandler(this.cmdPeptideSelectFile_Click);
            // 
            // txtPeptideInputFilePath
            // 
            this.txtPeptideInputFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPeptideInputFilePath.Location = new System.Drawing.Point(125, 21);
            this.txtPeptideInputFilePath.Name = "txtPeptideInputFilePath";
            this.txtPeptideInputFilePath.Size = new System.Drawing.Size(857, 22);
            this.txtPeptideInputFilePath.TabIndex = 1;
            this.txtPeptideInputFilePath.TextChanged += new System.EventHandler(this.txtPeptideInputFilePath_TextChanged);
            this.txtPeptideInputFilePath.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtPeptideInputFilePath_KeyPress);
            // 
            // fraProcessingOptions
            // 
            this.fraProcessingOptions.Location = new System.Drawing.Point(8, 8);
            this.fraProcessingOptions.Name = "fraProcessingOptions";
            this.fraProcessingOptions.Size = new System.Drawing.Size(360, 152);
            this.fraProcessingOptions.TabIndex = 0;
            this.fraProcessingOptions.TabStop = false;
            this.fraProcessingOptions.Text = "Processing Options";
            // 
            // fraMassCalculationOptions
            // 
            this.fraMassCalculationOptions.Location = new System.Drawing.Point(376, 80);
            this.fraMassCalculationOptions.Name = "fraMassCalculationOptions";
            this.fraMassCalculationOptions.Size = new System.Drawing.Size(248, 80);
            this.fraMassCalculationOptions.TabIndex = 1;
            this.fraMassCalculationOptions.TabStop = false;
            this.fraMassCalculationOptions.Text = "Mass Calculation Options";
            // 
            // fraDigestionOptions
            // 
            this.fraDigestionOptions.Location = new System.Drawing.Point(8, 168);
            this.fraDigestionOptions.Name = "fraDigestionOptions";
            this.fraDigestionOptions.Size = new System.Drawing.Size(616, 112);
            this.fraDigestionOptions.TabIndex = 2;
            this.fraDigestionOptions.TabStop = false;
            this.fraDigestionOptions.Text = "Digestion Options";
            // 
            // txtMinimumSLiCScore
            // 
            this.txtMinimumSLiCScore.Location = new System.Drawing.Point(144, 104);
            this.txtMinimumSLiCScore.Name = "txtMinimumSLiCScore";
            this.txtMinimumSLiCScore.Size = new System.Drawing.Size(40, 22);
            this.txtMinimumSLiCScore.TabIndex = 5;
            // 
            // fraPeakMatchingOptions
            // 
            this.fraPeakMatchingOptions.Location = new System.Drawing.Point(232, 48);
            this.fraPeakMatchingOptions.Name = "fraPeakMatchingOptions";
            this.fraPeakMatchingOptions.Size = new System.Drawing.Size(392, 136);
            this.fraPeakMatchingOptions.TabIndex = 2;
            this.fraPeakMatchingOptions.TabStop = false;
            // 
            // fraSqlServerOptions
            // 
            this.fraSqlServerOptions.Location = new System.Drawing.Point(576, 192);
            this.fraSqlServerOptions.Name = "fraSqlServerOptions";
            this.fraSqlServerOptions.Size = new System.Drawing.Size(376, 112);
            this.fraSqlServerOptions.TabIndex = 4;
            this.fraSqlServerOptions.TabStop = false;
            this.fraSqlServerOptions.Visible = false;
            // 
            // fraUniquenessBinningOptions
            // 
            this.fraUniquenessBinningOptions.Location = new System.Drawing.Point(8, 144);
            this.fraUniquenessBinningOptions.Name = "fraUniquenessBinningOptions";
            this.fraUniquenessBinningOptions.Size = new System.Drawing.Size(208, 136);
            this.fraUniquenessBinningOptions.TabIndex = 3;
            this.fraUniquenessBinningOptions.TabStop = false;
            // 
            // cmdPastePMThresholdsList
            // 
            this.cmdPastePMThresholdsList.Location = new System.Drawing.Point(456, 96);
            this.cmdPastePMThresholdsList.Name = "cmdPastePMThresholdsList";
            this.cmdPastePMThresholdsList.Size = new System.Drawing.Size(104, 24);
            this.cmdPastePMThresholdsList.TabIndex = 6;
            this.cmdPastePMThresholdsList.Text = "Paste Values";
            // 
            // cboPMPredefinedThresholds
            // 
            this.cboPMPredefinedThresholds.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPMPredefinedThresholds.Location = new System.Drawing.Point(336, 256);
            this.cboPMPredefinedThresholds.Name = "cboPMPredefinedThresholds";
            this.cboPMPredefinedThresholds.Size = new System.Drawing.Size(264, 24);
            this.cboPMPredefinedThresholds.TabIndex = 5;
            // 
            // cmdPMThresholdsAutoPopulate
            // 
            this.cmdPMThresholdsAutoPopulate.Location = new System.Drawing.Point(336, 224);
            this.cmdPMThresholdsAutoPopulate.Name = "cmdPMThresholdsAutoPopulate";
            this.cmdPMThresholdsAutoPopulate.Size = new System.Drawing.Size(104, 24);
            this.cmdPMThresholdsAutoPopulate.TabIndex = 4;
            this.cmdPMThresholdsAutoPopulate.Text = "Auto-Populate";
            // 
            // cmdClearPMThresholdsList
            // 
            this.cmdClearPMThresholdsList.Location = new System.Drawing.Point(456, 128);
            this.cmdClearPMThresholdsList.Name = "cmdClearPMThresholdsList";
            this.cmdClearPMThresholdsList.Size = new System.Drawing.Size(104, 24);
            this.cmdClearPMThresholdsList.TabIndex = 7;
            this.cmdClearPMThresholdsList.Text = "Clear List";
            // 
            // cboMassTolType
            // 
            this.cboMassTolType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMassTolType.Location = new System.Drawing.Point(144, 224);
            this.cboMassTolType.Name = "cboMassTolType";
            this.cboMassTolType.Size = new System.Drawing.Size(136, 24);
            this.cboMassTolType.TabIndex = 2;
            // 
            // tbsOptions
            // 
            this.tbsOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbsOptions.Controls.Add(this.TabPageFileFormatOptions);
            this.tbsOptions.Controls.Add(this.TabPagePeakMatchingThresholds);
            this.tbsOptions.Location = new System.Drawing.Point(10, 222);
            this.tbsOptions.Name = "tbsOptions";
            this.tbsOptions.SelectedIndex = 0;
            this.tbsOptions.Size = new System.Drawing.Size(1001, 451);
            this.tbsOptions.TabIndex = 3;
            // 
            // TabPageFileFormatOptions
            // 
            this.TabPageFileFormatOptions.Controls.Add(this.lblStatus);
            this.TabPageFileFormatOptions.Controls.Add(this.cmdExit);
            this.TabPageFileFormatOptions.Controls.Add(this.cmdStart);
            this.TabPageFileFormatOptions.Controls.Add(this.cmdAbort);
            this.TabPageFileFormatOptions.Controls.Add(this.lblProgress);
            this.TabPageFileFormatOptions.Controls.Add(this.fraOptions);
            this.TabPageFileFormatOptions.Controls.Add(this.fraPeptideDelimitedFileOptions);
            this.TabPageFileFormatOptions.Controls.Add(this.fraProteinDelimitedFileOptions);
            this.TabPageFileFormatOptions.Location = new System.Drawing.Point(4, 25);
            this.TabPageFileFormatOptions.Name = "TabPageFileFormatOptions";
            this.TabPageFileFormatOptions.Size = new System.Drawing.Size(993, 422);
            this.TabPageFileFormatOptions.TabIndex = 2;
            this.TabPageFileFormatOptions.Text = "File Format Options";
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.Location = new System.Drawing.Point(656, 69);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(309, 51);
            this.lblStatus.TabIndex = 7;
            this.lblStatus.Text = "Status ...";
            // 
            // cmdExit
            // 
            this.cmdExit.Location = new System.Drawing.Point(662, 204);
            this.cmdExit.Name = "cmdExit";
            this.cmdExit.Size = new System.Drawing.Size(116, 37);
            this.cmdExit.TabIndex = 5;
            this.cmdExit.Text = "E&xit";
            this.cmdExit.Click += new System.EventHandler(this.cmdExit_Click);
            // 
            // cmdStart
            // 
            this.cmdStart.Location = new System.Drawing.Point(662, 148);
            this.cmdStart.Name = "cmdStart";
            this.cmdStart.Size = new System.Drawing.Size(116, 37);
            this.cmdStart.TabIndex = 4;
            this.cmdStart.Text = "&Start";
            this.cmdStart.Click += new System.EventHandler(this.cmdStart_Click);
            // 
            // cmdAbort
            // 
            this.cmdAbort.Location = new System.Drawing.Point(662, 148);
            this.cmdAbort.Name = "cmdAbort";
            this.cmdAbort.Size = new System.Drawing.Size(116, 37);
            this.cmdAbort.TabIndex = 4;
            this.cmdAbort.Text = "Abort";
            this.cmdAbort.Click += new System.EventHandler(this.cmdAbort_Click);
            // 
            // lblProgress
            // 
            this.lblProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblProgress.Location = new System.Drawing.Point(656, 15);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(309, 51);
            this.lblProgress.TabIndex = 3;
            this.lblProgress.Text = "Progress ...";
            // 
            // fraOptions
            // 
            this.fraOptions.Controls.Add(this.chkIgnoreILDifferences);
            this.fraOptions.Controls.Add(this.chkMatchPeptidePrefixAndSuffixToProtein);
            this.fraOptions.Controls.Add(this.chkSearchAllProteinsSkipCoverageComputationSteps);
            this.fraOptions.Controls.Add(this.chkSaveProteinToPeptideMappingFile);
            this.fraOptions.Controls.Add(this.chkSaveSourceDataPlusProteinsFile);
            this.fraOptions.Controls.Add(this.chkSearchAllProteinsForPeptideSequence);
            this.fraOptions.Controls.Add(this.chkOutputProteinSequence);
            this.fraOptions.Controls.Add(this.chkTrackPeptideCounts);
            this.fraOptions.Controls.Add(this.chkRemoveSymbolCharacters);
            this.fraOptions.Location = new System.Drawing.Point(10, 252);
            this.fraOptions.Name = "fraOptions";
            this.fraOptions.Size = new System.Drawing.Size(814, 163);
            this.fraOptions.TabIndex = 2;
            this.fraOptions.TabStop = false;
            this.fraOptions.Text = "Options";
            // 
            // chkIgnoreILDifferences
            // 
            this.chkIgnoreILDifferences.Location = new System.Drawing.Point(480, 108);
            this.chkIgnoreILDifferences.Name = "chkIgnoreILDifferences";
            this.chkIgnoreILDifferences.Size = new System.Drawing.Size(269, 25);
            this.chkIgnoreILDifferences.TabIndex = 7;
            this.chkIgnoreILDifferences.Text = "Ignore I/L Differences";
            // 
            // chkMatchPeptidePrefixAndSuffixToProtein
            // 
            this.chkMatchPeptidePrefixAndSuffixToProtein.Location = new System.Drawing.Point(19, 138);
            this.chkMatchPeptidePrefixAndSuffixToProtein.Name = "chkMatchPeptidePrefixAndSuffixToProtein";
            this.chkMatchPeptidePrefixAndSuffixToProtein.Size = new System.Drawing.Size(394, 25);
            this.chkMatchPeptidePrefixAndSuffixToProtein.TabIndex = 6;
            this.chkMatchPeptidePrefixAndSuffixToProtein.Text = "Match peptide prefix and suffix letters to protein sequence";
            // 
            // chkSearchAllProteinsSkipCoverageComputationSteps
            // 
            this.chkSearchAllProteinsSkipCoverageComputationSteps.Location = new System.Drawing.Point(480, 78);
            this.chkSearchAllProteinsSkipCoverageComputationSteps.Name = "chkSearchAllProteinsSkipCoverageComputationSteps";
            this.chkSearchAllProteinsSkipCoverageComputationSteps.Size = new System.Drawing.Size(314, 25);
            this.chkSearchAllProteinsSkipCoverageComputationSteps.TabIndex = 3;
            this.chkSearchAllProteinsSkipCoverageComputationSteps.Text = "Skip coverage computation (faster)";
            // 
            // chkSaveProteinToPeptideMappingFile
            // 
            this.chkSaveProteinToPeptideMappingFile.Location = new System.Drawing.Point(480, 18);
            this.chkSaveProteinToPeptideMappingFile.Name = "chkSaveProteinToPeptideMappingFile";
            this.chkSaveProteinToPeptideMappingFile.Size = new System.Drawing.Size(296, 25);
            this.chkSaveProteinToPeptideMappingFile.TabIndex = 2;
            this.chkSaveProteinToPeptideMappingFile.Text = "Save protein to peptide mapping details";
            this.chkSaveProteinToPeptideMappingFile.CheckedChanged += new System.EventHandler(this.chkSearchAllProteinsSaveDetails_CheckedChanged);
            // 
            // chkSaveSourceDataPlusProteinsFile
            // 
            this.chkSaveSourceDataPlusProteinsFile.Location = new System.Drawing.Point(480, 48);
            this.chkSaveSourceDataPlusProteinsFile.Name = "chkSaveSourceDataPlusProteinsFile";
            this.chkSaveSourceDataPlusProteinsFile.Size = new System.Drawing.Size(288, 25);
            this.chkSaveSourceDataPlusProteinsFile.TabIndex = 1;
            this.chkSaveSourceDataPlusProteinsFile.Text = "Create source data plus proteins file";
            this.chkSaveSourceDataPlusProteinsFile.CheckedChanged += new System.EventHandler(this.chkSearchAllProteinsForPeptideSequence_CheckedChanged);
            // 
            // chkSearchAllProteinsForPeptideSequence
            // 
            this.chkSearchAllProteinsForPeptideSequence.Location = new System.Drawing.Point(19, 48);
            this.chkSearchAllProteinsForPeptideSequence.Name = "chkSearchAllProteinsForPeptideSequence";
            this.chkSearchAllProteinsForPeptideSequence.Size = new System.Drawing.Size(288, 25);
            this.chkSearchAllProteinsForPeptideSequence.TabIndex = 1;
            this.chkSearchAllProteinsForPeptideSequence.Text = "Search All Proteins For Peptide Sequence";
            this.chkSearchAllProteinsForPeptideSequence.CheckedChanged += new System.EventHandler(this.chkSearchAllProteinsForPeptideSequence_CheckedChanged);
            // 
            // chkOutputProteinSequence
            // 
            this.chkOutputProteinSequence.Location = new System.Drawing.Point(19, 18);
            this.chkOutputProteinSequence.Name = "chkOutputProteinSequence";
            this.chkOutputProteinSequence.Size = new System.Drawing.Size(211, 25);
            this.chkOutputProteinSequence.TabIndex = 0;
            this.chkOutputProteinSequence.Text = "Output Protein Sequence";
            // 
            // chkTrackPeptideCounts
            // 
            this.chkTrackPeptideCounts.Location = new System.Drawing.Point(19, 78);
            this.chkTrackPeptideCounts.Name = "chkTrackPeptideCounts";
            this.chkTrackPeptideCounts.Size = new System.Drawing.Size(317, 25);
            this.chkTrackPeptideCounts.TabIndex = 4;
            this.chkTrackPeptideCounts.Text = "Track Unique And Non-Unique Peptide Counts";
            // 
            // chkRemoveSymbolCharacters
            // 
            this.chkRemoveSymbolCharacters.Location = new System.Drawing.Point(19, 108);
            this.chkRemoveSymbolCharacters.Name = "chkRemoveSymbolCharacters";
            this.chkRemoveSymbolCharacters.Size = new System.Drawing.Size(442, 25);
            this.chkRemoveSymbolCharacters.TabIndex = 5;
            this.chkRemoveSymbolCharacters.Text = "Remove non-letter characters from protein and peptide sequences";
            // 
            // fraPeptideDelimitedFileOptions
            // 
            this.fraPeptideDelimitedFileOptions.Controls.Add(this.cboPeptideInputFileColumnOrdering);
            this.fraPeptideDelimitedFileOptions.Controls.Add(this.Label1);
            this.fraPeptideDelimitedFileOptions.Controls.Add(this.chkPeptideFileSkipFirstLine);
            this.fraPeptideDelimitedFileOptions.Controls.Add(this.txtPeptideInputFileColumnDelimiter);
            this.fraPeptideDelimitedFileOptions.Controls.Add(this.lblPeptideInputFileColumnDelimiter);
            this.fraPeptideDelimitedFileOptions.Controls.Add(this.cboPeptideInputFileColumnDelimiter);
            this.fraPeptideDelimitedFileOptions.Controls.Add(this.lblInputFileNotes);
            this.fraPeptideDelimitedFileOptions.Location = new System.Drawing.Point(10, 129);
            this.fraPeptideDelimitedFileOptions.Name = "fraPeptideDelimitedFileOptions";
            this.fraPeptideDelimitedFileOptions.Size = new System.Drawing.Size(643, 120);
            this.fraPeptideDelimitedFileOptions.TabIndex = 1;
            this.fraPeptideDelimitedFileOptions.TabStop = false;
            this.fraPeptideDelimitedFileOptions.Text = "Peptide Delimited Input File Options";
            // 
            // cboPeptideInputFileColumnOrdering
            // 
            this.cboPeptideInputFileColumnOrdering.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPeptideInputFileColumnOrdering.DropDownWidth = 70;
            this.cboPeptideInputFileColumnOrdering.Location = new System.Drawing.Point(106, 28);
            this.cboPeptideInputFileColumnOrdering.Name = "cboPeptideInputFileColumnOrdering";
            this.cboPeptideInputFileColumnOrdering.Size = new System.Drawing.Size(316, 24);
            this.cboPeptideInputFileColumnOrdering.TabIndex = 1;
            this.cboPeptideInputFileColumnOrdering.SelectedIndexChanged += new System.EventHandler(this.cboPeptideInputFileColumnOrdering_SelectedIndexChanged);
            // 
            // Label1
            // 
            this.Label1.Location = new System.Drawing.Point(10, 28);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(96, 18);
            this.Label1.TabIndex = 0;
            this.Label1.Text = "Column Order";
            // 
            // chkPeptideFileSkipFirstLine
            // 
            this.chkPeptideFileSkipFirstLine.Location = new System.Drawing.Point(317, 65);
            this.chkPeptideFileSkipFirstLine.Name = "chkPeptideFileSkipFirstLine";
            this.chkPeptideFileSkipFirstLine.Size = new System.Drawing.Size(288, 27);
            this.chkPeptideFileSkipFirstLine.TabIndex = 5;
            this.chkPeptideFileSkipFirstLine.Text = "Skip first line in peptide input file";
            // 
            // txtPeptideInputFileColumnDelimiter
            // 
            this.txtPeptideInputFileColumnDelimiter.Location = new System.Drawing.Point(230, 65);
            this.txtPeptideInputFileColumnDelimiter.MaxLength = 1;
            this.txtPeptideInputFileColumnDelimiter.Name = "txtPeptideInputFileColumnDelimiter";
            this.txtPeptideInputFileColumnDelimiter.Size = new System.Drawing.Size(39, 22);
            this.txtPeptideInputFileColumnDelimiter.TabIndex = 4;
            this.txtPeptideInputFileColumnDelimiter.Text = ";";
            // 
            // lblPeptideInputFileColumnDelimiter
            // 
            this.lblPeptideInputFileColumnDelimiter.Location = new System.Drawing.Point(10, 65);
            this.lblPeptideInputFileColumnDelimiter.Name = "lblPeptideInputFileColumnDelimiter";
            this.lblPeptideInputFileColumnDelimiter.Size = new System.Drawing.Size(115, 18);
            this.lblPeptideInputFileColumnDelimiter.TabIndex = 2;
            this.lblPeptideInputFileColumnDelimiter.Text = "Column Delimiter";
            // 
            // cboPeptideInputFileColumnDelimiter
            // 
            this.cboPeptideInputFileColumnDelimiter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPeptideInputFileColumnDelimiter.DropDownWidth = 70;
            this.cboPeptideInputFileColumnDelimiter.Location = new System.Drawing.Point(134, 65);
            this.cboPeptideInputFileColumnDelimiter.Name = "cboPeptideInputFileColumnDelimiter";
            this.cboPeptideInputFileColumnDelimiter.Size = new System.Drawing.Size(84, 24);
            this.cboPeptideInputFileColumnDelimiter.TabIndex = 3;
            // 
            // lblInputFileNotes
            // 
            this.lblInputFileNotes.Location = new System.Drawing.Point(10, 95);
            this.lblInputFileNotes.Name = "lblInputFileNotes";
            this.lblInputFileNotes.Size = new System.Drawing.Size(585, 18);
            this.lblInputFileNotes.TabIndex = 6;
            this.lblInputFileNotes.Text = "Note: prefix and suffix residues will be automatically removed from the input pep" +
    "tides";
            // 
            // fraProteinDelimitedFileOptions
            // 
            this.fraProteinDelimitedFileOptions.Controls.Add(this.chkProteinFileSkipFirstLine);
            this.fraProteinDelimitedFileOptions.Controls.Add(this.cboProteinInputFileColumnOrdering);
            this.fraProteinDelimitedFileOptions.Controls.Add(this.lblProteinInputFileColumnOrdering);
            this.fraProteinDelimitedFileOptions.Controls.Add(this.txtProteinInputFileColumnDelimiter);
            this.fraProteinDelimitedFileOptions.Controls.Add(this.lblProteinInputFileColumnDelimiter);
            this.fraProteinDelimitedFileOptions.Controls.Add(this.cboProteinInputFileColumnDelimiter);
            this.fraProteinDelimitedFileOptions.Location = new System.Drawing.Point(10, 18);
            this.fraProteinDelimitedFileOptions.Name = "fraProteinDelimitedFileOptions";
            this.fraProteinDelimitedFileOptions.Size = new System.Drawing.Size(604, 102);
            this.fraProteinDelimitedFileOptions.TabIndex = 0;
            this.fraProteinDelimitedFileOptions.TabStop = false;
            this.fraProteinDelimitedFileOptions.Text = "Protein Delimited Input File Options";
            // 
            // chkProteinFileSkipFirstLine
            // 
            this.chkProteinFileSkipFirstLine.Location = new System.Drawing.Point(317, 65);
            this.chkProteinFileSkipFirstLine.Name = "chkProteinFileSkipFirstLine";
            this.chkProteinFileSkipFirstLine.Size = new System.Drawing.Size(259, 27);
            this.chkProteinFileSkipFirstLine.TabIndex = 5;
            this.chkProteinFileSkipFirstLine.Text = "Skip first line in protein input file";
            // 
            // cboProteinInputFileColumnOrdering
            // 
            this.cboProteinInputFileColumnOrdering.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboProteinInputFileColumnOrdering.DropDownWidth = 70;
            this.cboProteinInputFileColumnOrdering.Location = new System.Drawing.Point(106, 28);
            this.cboProteinInputFileColumnOrdering.Name = "cboProteinInputFileColumnOrdering";
            this.cboProteinInputFileColumnOrdering.Size = new System.Drawing.Size(470, 24);
            this.cboProteinInputFileColumnOrdering.TabIndex = 1;
            // 
            // lblProteinInputFileColumnOrdering
            // 
            this.lblProteinInputFileColumnOrdering.Location = new System.Drawing.Point(10, 30);
            this.lblProteinInputFileColumnOrdering.Name = "lblProteinInputFileColumnOrdering";
            this.lblProteinInputFileColumnOrdering.Size = new System.Drawing.Size(96, 18);
            this.lblProteinInputFileColumnOrdering.TabIndex = 0;
            this.lblProteinInputFileColumnOrdering.Text = "Column Order";
            // 
            // txtProteinInputFileColumnDelimiter
            // 
            this.txtProteinInputFileColumnDelimiter.Location = new System.Drawing.Point(230, 65);
            this.txtProteinInputFileColumnDelimiter.MaxLength = 1;
            this.txtProteinInputFileColumnDelimiter.Name = "txtProteinInputFileColumnDelimiter";
            this.txtProteinInputFileColumnDelimiter.Size = new System.Drawing.Size(39, 22);
            this.txtProteinInputFileColumnDelimiter.TabIndex = 4;
            this.txtProteinInputFileColumnDelimiter.Text = ";";
            // 
            // lblProteinInputFileColumnDelimiter
            // 
            this.lblProteinInputFileColumnDelimiter.Location = new System.Drawing.Point(10, 67);
            this.lblProteinInputFileColumnDelimiter.Name = "lblProteinInputFileColumnDelimiter";
            this.lblProteinInputFileColumnDelimiter.Size = new System.Drawing.Size(115, 18);
            this.lblProteinInputFileColumnDelimiter.TabIndex = 2;
            this.lblProteinInputFileColumnDelimiter.Text = "Column Delimiter";
            // 
            // cboProteinInputFileColumnDelimiter
            // 
            this.cboProteinInputFileColumnDelimiter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboProteinInputFileColumnDelimiter.DropDownWidth = 70;
            this.cboProteinInputFileColumnDelimiter.Location = new System.Drawing.Point(134, 65);
            this.cboProteinInputFileColumnDelimiter.Name = "cboProteinInputFileColumnDelimiter";
            this.cboProteinInputFileColumnDelimiter.Size = new System.Drawing.Size(84, 24);
            this.cboProteinInputFileColumnDelimiter.TabIndex = 3;
            // 
            // TabPagePeakMatchingThresholds
            // 
            this.TabPagePeakMatchingThresholds.Controls.Add(this.txtCoverage);
            this.TabPagePeakMatchingThresholds.Controls.Add(this.txtRTFCode);
            this.TabPagePeakMatchingThresholds.Controls.Add(this.txtCustomProteinSequence);
            this.TabPagePeakMatchingThresholds.Controls.Add(this.lblCustomProteinSequence);
            this.TabPagePeakMatchingThresholds.Controls.Add(this.chkAddSpace);
            this.TabPagePeakMatchingThresholds.Controls.Add(this.cboCharactersPerLine);
            this.TabPagePeakMatchingThresholds.Controls.Add(this.rtfRichTextBox);
            this.TabPagePeakMatchingThresholds.Controls.Add(this.dgResults);
            this.TabPagePeakMatchingThresholds.Location = new System.Drawing.Point(4, 25);
            this.TabPagePeakMatchingThresholds.Name = "TabPagePeakMatchingThresholds";
            this.TabPagePeakMatchingThresholds.Size = new System.Drawing.Size(1199, 527);
            this.TabPagePeakMatchingThresholds.TabIndex = 3;
            this.TabPagePeakMatchingThresholds.Text = "Results Browser";
            this.TabPagePeakMatchingThresholds.Visible = false;
            // 
            // txtCoverage
            // 
            this.txtCoverage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.txtCoverage.Location = new System.Drawing.Point(614, 468);
            this.txtCoverage.Name = "txtCoverage";
            this.txtCoverage.ReadOnly = true;
            this.txtCoverage.Size = new System.Drawing.Size(260, 22);
            this.txtCoverage.TabIndex = 7;
            this.txtCoverage.Text = "Coverage: 0%  (0 / 0)";
            this.txtCoverage.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtCoverage_KeyPress);
            // 
            // txtRTFCode
            // 
            this.txtRTFCode.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.txtRTFCode.Location = new System.Drawing.Point(86, 18);
            this.txtRTFCode.Multiline = true;
            this.txtRTFCode.Name = "txtRTFCode";
            this.txtRTFCode.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtRTFCode.Size = new System.Drawing.Size(519, 358);
            this.txtRTFCode.TabIndex = 1;
            this.txtRTFCode.WordWrap = false;
            // 
            // txtCustomProteinSequence
            // 
            this.txtCustomProteinSequence.AcceptsReturn = true;
            this.txtCustomProteinSequence.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.txtCustomProteinSequence.Location = new System.Drawing.Point(106, 470);
            this.txtCustomProteinSequence.Multiline = true;
            this.txtCustomProteinSequence.Name = "txtCustomProteinSequence";
            this.txtCustomProteinSequence.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtCustomProteinSequence.Size = new System.Drawing.Size(499, 45);
            this.txtCustomProteinSequence.TabIndex = 6;
            this.txtCustomProteinSequence.Click += new System.EventHandler(this.txtCustomProteinSequence_Click);
            this.txtCustomProteinSequence.TextChanged += new System.EventHandler(this.txtCustomProteinSequence_TextChanged);
            this.txtCustomProteinSequence.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtCustomProteinSequence_KeyPress);
            // 
            // lblCustomProteinSequence
            // 
            this.lblCustomProteinSequence.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCustomProteinSequence.Location = new System.Drawing.Point(5, 470);
            this.lblCustomProteinSequence.Name = "lblCustomProteinSequence";
            this.lblCustomProteinSequence.Size = new System.Drawing.Size(105, 37);
            this.lblCustomProteinSequence.TabIndex = 5;
            this.lblCustomProteinSequence.Text = "Custom Protein Sequence";
            // 
            // chkAddSpace
            // 
            this.chkAddSpace.Location = new System.Drawing.Point(883, 7);
            this.chkAddSpace.Name = "chkAddSpace";
            this.chkAddSpace.Size = new System.Drawing.Size(144, 29);
            this.chkAddSpace.TabIndex = 3;
            this.chkAddSpace.Text = "Add space every 10 residues";
            this.chkAddSpace.CheckedChanged += new System.EventHandler(this.chkAddSpace_CheckStateChanged);
            // 
            // cboCharactersPerLine
            // 
            this.cboCharactersPerLine.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCharactersPerLine.Location = new System.Drawing.Point(614, 12);
            this.cboCharactersPerLine.Name = "cboCharactersPerLine";
            this.cboCharactersPerLine.Size = new System.Drawing.Size(260, 24);
            this.cboCharactersPerLine.TabIndex = 2;
            this.cboCharactersPerLine.SelectedIndexChanged += new System.EventHandler(this.cboCharactersPerLine_SelectedIndexChanged);
            // 
            // rtfRichTextBox
            // 
            this.rtfRichTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtfRichTextBox.Location = new System.Drawing.Point(614, 46);
            this.rtfRichTextBox.Name = "rtfRichTextBox";
            this.rtfRichTextBox.Size = new System.Drawing.Size(574, 413);
            this.rtfRichTextBox.TabIndex = 4;
            this.rtfRichTextBox.Text = "";
            this.rtfRichTextBox.WordWrap = false;
            // 
            // dgResults
            // 
            this.dgResults.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.dgResults.CaptionText = "Results";
            this.dgResults.DataMember = "";
            this.dgResults.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.dgResults.Location = new System.Drawing.Point(2, 18);
            this.dgResults.Name = "dgResults";
            this.dgResults.PreferredColumnWidth = 80;
            this.dgResults.Size = new System.Drawing.Size(605, 442);
            this.dgResults.TabIndex = 0;
            this.dgResults.CurrentCellChanged += new System.EventHandler(this.dgResults_CurrentCellChanged);
            // 
            // fraOutputFolderPath
            // 
            this.fraOutputFolderPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fraOutputFolderPath.Controls.Add(this.cmdSelectOutputFolder);
            this.fraOutputFolderPath.Controls.Add(this.txtOutputFolderPath);
            this.fraOutputFolderPath.Location = new System.Drawing.Point(10, 148);
            this.fraOutputFolderPath.Name = "fraOutputFolderPath";
            this.fraOutputFolderPath.Size = new System.Drawing.Size(1001, 64);
            this.fraOutputFolderPath.TabIndex = 2;
            this.fraOutputFolderPath.TabStop = false;
            this.fraOutputFolderPath.Text = "Output folder path";
            // 
            // cmdSelectOutputFolder
            // 
            this.cmdSelectOutputFolder.Location = new System.Drawing.Point(10, 18);
            this.cmdSelectOutputFolder.Name = "cmdSelectOutputFolder";
            this.cmdSelectOutputFolder.Size = new System.Drawing.Size(96, 37);
            this.cmdSelectOutputFolder.TabIndex = 0;
            this.cmdSelectOutputFolder.Text = "Select folder";
            this.cmdSelectOutputFolder.Click += new System.EventHandler(this.cmdSelectOutputFolder_Click);
            // 
            // txtOutputFolderPath
            // 
            this.txtOutputFolderPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutputFolderPath.Location = new System.Drawing.Point(125, 21);
            this.txtOutputFolderPath.Name = "txtOutputFolderPath";
            this.txtOutputFolderPath.Size = new System.Drawing.Size(857, 22);
            this.txtOutputFolderPath.TabIndex = 1;
            this.txtOutputFolderPath.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtOutputFolderPath_KeyPress);
            // 
            // GUI
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 15);
            this.ClientSize = new System.Drawing.Size(1030, 683);
            this.Controls.Add(this.fraOutputFolderPath);
            this.Controls.Add(this.tbsOptions);
            this.Controls.Add(this.fraPeptideInputFilePath);
            this.Controls.Add(this.fraProteinInputFilePath);
            this.Menu = this.MainMenuControl;
            this.Name = "GUI";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Protein Coverage Summarizer";
            this.fraProteinInputFilePath.ResumeLayout(false);
            this.fraProteinInputFilePath.PerformLayout();
            this.fraPeptideInputFilePath.ResumeLayout(false);
            this.fraPeptideInputFilePath.PerformLayout();
            this.tbsOptions.ResumeLayout(false);
            this.TabPageFileFormatOptions.ResumeLayout(false);
            this.fraOptions.ResumeLayout(false);
            this.fraPeptideDelimitedFileOptions.ResumeLayout(false);
            this.fraPeptideDelimitedFileOptions.PerformLayout();
            this.fraProteinDelimitedFileOptions.ResumeLayout(false);
            this.fraProteinDelimitedFileOptions.PerformLayout();
            this.TabPagePeakMatchingThresholds.ResumeLayout(false);
            this.TabPagePeakMatchingThresholds.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgResults)).EndInit();
            this.fraOutputFolderPath.ResumeLayout(false);
            this.fraOutputFolderPath.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private MainMenu MainMenuControl;
        private MenuItem mnuFile;
        private MenuItem mnuFileSelectInputFile;
        private MenuItem mnuFileSelectOutputFile;
        private MenuItem mnuFileSep1;
        private MenuItem mnuFileSaveDefaultOptions;
        private MenuItem mnuFileSep2;
        private MenuItem mnuFileExit;
        private MenuItem mnuEdit;
        private MenuItem mnuEditResetOptions;
        private MenuItem mnuHelp;
        private MenuItem mnuHelpAbout;
        private GroupBox fraProteinInputFilePath;
        private Button cmdProteinSelectFile;
        private GroupBox fraPeptideInputFilePath;
        private Button cmdPeptideSelectFile;
        private TextBox txtPeptideInputFilePath;
        private GroupBox fraProcessingOptions;
        private GroupBox fraMassCalculationOptions;
        private GroupBox fraDigestionOptions;
        private TextBox txtMinimumSLiCScore;
        private GroupBox fraPeakMatchingOptions;
        private GroupBox fraSqlServerOptions;
        private GroupBox fraUniquenessBinningOptions;
        private Button cmdPastePMThresholdsList;
        private ComboBox cboPMPredefinedThresholds;
        private Button cmdPMThresholdsAutoPopulate;
        private Button cmdClearPMThresholdsList;
        private ComboBox cboMassTolType;
        private TabControl tbsOptions;
        private TabPage TabPageFileFormatOptions;
        private TabPage TabPagePeakMatchingThresholds;
        private GroupBox fraProteinDelimitedFileOptions;
        private ComboBox cboProteinInputFileColumnOrdering;
        private Label lblProteinInputFileColumnOrdering;
        private Label lblProteinInputFileColumnDelimiter;
        private ComboBox cboProteinInputFileColumnDelimiter;
        private GroupBox fraPeptideDelimitedFileOptions;
        private TextBox txtPeptideInputFileColumnDelimiter;
        private Label lblPeptideInputFileColumnDelimiter;
        private ComboBox cboPeptideInputFileColumnDelimiter;
        private GroupBox fraOptions;
        private CheckBox chkSearchAllProteinsForPeptideSequence;
        private CheckBox chkOutputProteinSequence;
        private CheckBox chkTrackPeptideCounts;
        private CheckBox chkRemoveSymbolCharacters;
        private Button cmdStart;
        private MenuItem mnuPeptideInputFile;
        private DataGrid dgResults;
        private Label lblProgress;
        private CheckBox chkAddSpace;
        private ComboBox cboCharactersPerLine;
        private RichTextBox rtfRichTextBox;
        private CheckBox chkPeptideFileSkipFirstLine;
        private Label Label1;
        private CheckBox chkProteinFileSkipFirstLine;
        private Button cmdAbort;
        private Button cmdExit;
        private ComboBox cboPeptideInputFileColumnOrdering;
        private TextBox txtProteinInputFileColumnDelimiter;
        private MenuItem mnuFileLoadOptions;
        private TextBox txtCustomProteinSequence;
        private Label lblCustomProteinSequence;
        private TextBox txtRTFCode;
        private MenuItem mnuEditShowRTF;
        private TextBox txtCoverage;
        private GroupBox fraOutputFolderPath;
        private TextBox txtOutputFolderPath;
        private Button cmdSelectOutputFolder;
        private CheckBox chkSearchAllProteinsSkipCoverageComputationSteps;
        private Label lblInputFileNotes;
        private CheckBox chkSaveProteinToPeptideMappingFile;
        private CheckBox chkMatchPeptidePrefixAndSuffixToProtein;
        private TextBox txtProteinInputFilePath;
        private Label lblStatus;
        private CheckBox chkIgnoreILDifferences;
        private CheckBox chkSaveSourceDataPlusProteinsFile;
    }
}