// -------------------------------------------------------------------------------
// Written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA)
// Program started June 14, 2005
//
// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
// Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
// -------------------------------------------------------------------------------
//
// Licensed under the 2-Clause BSD License; you may not use this file except
// in compliance with the License.  You may obtain a copy of the License at
// https://opensource.org/licenses/BSD-2-Clause
//
// Copyright 2018 Battelle Memorial Institute

using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Ookii.Dialogs;
using PRISM;
using PRISM.FileProcessor;
using PRISMWin;
using ProteinCoverageSummarizer;
using ProteinFileReader;

namespace ProteinCoverageSummarizerGUI
{
    /// <summary>
    /// This program uses clsProteinCoverageSummarizer to read in a file with protein sequences along with
    /// an accompanying file with peptide sequences and compute the percent coverage of each of the proteins
    /// </summary>
    public class GUI : Form
    {
        #region " Windows Form Designer generated code "

        public GUI() : base()
        {
            base.Closing += GUI_Closing;

            // This call is required by the Windows Form Designer.
            InitializeComponent();

            // Add any initialization after the InitializeComponent() call
            InitializeControls();
        }

        // Form overrides dispose to clean up the component list.
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        // Required by the Windows Form Designer
        private IContainer components;
        #region "Designer generated code "
        // NOTE: The following procedure is required by the Windows Form Designer
        // It can be modified using the Windows Form Designer.
        // Do not modify it using the code editor.
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
            this.cmdExit = new System.Windows.Forms.Button();
            this.cmdStart = new System.Windows.Forms.Button();
            this.cmdAbort = new System.Windows.Forms.Button();
            this.lblProgress = new System.Windows.Forms.Label();
            this.fraOptions = new System.Windows.Forms.GroupBox();
            this.chkIgnoreILDifferences = new System.Windows.Forms.CheckBox();
            this.chkMatchPeptidePrefixAndSuffixToProtein = new System.Windows.Forms.CheckBox();
            this.chkSearchAllProteinsSkipCoverageComputationSteps = new System.Windows.Forms.CheckBox();
            this.chkSaveProteinToPeptideMappingFile = new System.Windows.Forms.CheckBox();
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
            this.lblStatus = new System.Windows.Forms.Label();
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
            this.fraProteinInputFilePath.Size = new System.Drawing.Size(885, 56);
            this.fraProteinInputFilePath.TabIndex = 0;
            this.fraProteinInputFilePath.TabStop = false;
            this.fraProteinInputFilePath.Text = "Protein Input File Path (Fasta or Tab-delimited)";
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
            this.txtProteinInputFilePath.Size = new System.Drawing.Size(741, 22);
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
            this.fraPeptideInputFilePath.Size = new System.Drawing.Size(885, 55);
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
            this.txtPeptideInputFilePath.Size = new System.Drawing.Size(741, 22);
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
            this.txtMinimumSLiCScore.Size = new System.Drawing.Size(40, 20);
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
            this.cboPMPredefinedThresholds.Size = new System.Drawing.Size(264, 25);
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
            this.cboMassTolType.Size = new System.Drawing.Size(136, 25);
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
            this.tbsOptions.Size = new System.Drawing.Size(885, 369);
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
            this.TabPageFileFormatOptions.Size = new System.Drawing.Size(877, 340);
            this.TabPageFileFormatOptions.TabIndex = 2;
            this.TabPageFileFormatOptions.Text = "File Format Options";
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
            this.lblProgress.Location = new System.Drawing.Point(657, 15);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(192, 51);
            this.lblProgress.TabIndex = 3;
            this.lblProgress.Text = "Progress ...";
            //
            // fraOptions
            //
            this.fraOptions.Controls.Add(this.chkIgnoreILDifferences);
            this.fraOptions.Controls.Add(this.chkMatchPeptidePrefixAndSuffixToProtein);
            this.fraOptions.Controls.Add(this.chkSearchAllProteinsSkipCoverageComputationSteps);
            this.fraOptions.Controls.Add(this.chkSaveProteinToPeptideMappingFile);
            this.fraOptions.Controls.Add(this.chkSearchAllProteinsForPeptideSequence);
            this.fraOptions.Controls.Add(this.chkOutputProteinSequence);
            this.fraOptions.Controls.Add(this.chkTrackPeptideCounts);
            this.fraOptions.Controls.Add(this.chkRemoveSymbolCharacters);
            this.fraOptions.Location = new System.Drawing.Point(10, 252);
            this.fraOptions.Name = "fraOptions";
            this.fraOptions.Size = new System.Drawing.Size(777, 163);
            this.fraOptions.TabIndex = 2;
            this.fraOptions.TabStop = false;
            this.fraOptions.Text = "Options";
            //
            // chkIgnoreILDifferences
            //
            this.chkIgnoreILDifferences.Location = new System.Drawing.Point(480, 111);
            this.chkIgnoreILDifferences.Name = "chkIgnoreILDifferences";
            this.chkIgnoreILDifferences.Size = new System.Drawing.Size(269, 18);
            this.chkIgnoreILDifferences.TabIndex = 7;
            this.chkIgnoreILDifferences.Text = "Ignore I/L Differences";
            //
            // chkMatchPeptidePrefixAndSuffixToProtein
            //
            this.chkMatchPeptidePrefixAndSuffixToProtein.Location = new System.Drawing.Point(19, 138);
            this.chkMatchPeptidePrefixAndSuffixToProtein.Name = "chkMatchPeptidePrefixAndSuffixToProtein";
            this.chkMatchPeptidePrefixAndSuffixToProtein.Size = new System.Drawing.Size(394, 19);
            this.chkMatchPeptidePrefixAndSuffixToProtein.TabIndex = 6;
            this.chkMatchPeptidePrefixAndSuffixToProtein.Text = "Match peptide prefix and suffix letters to protein sequence";
            //
            // chkSearchAllProteinsSkipCoverageComputationSteps
            //
            this.chkSearchAllProteinsSkipCoverageComputationSteps.Location = new System.Drawing.Point(480, 65);
            this.chkSearchAllProteinsSkipCoverageComputationSteps.Name = "chkSearchAllProteinsSkipCoverageComputationSteps";
            this.chkSearchAllProteinsSkipCoverageComputationSteps.Size = new System.Drawing.Size(269, 18);
            this.chkSearchAllProteinsSkipCoverageComputationSteps.TabIndex = 3;
            this.chkSearchAllProteinsSkipCoverageComputationSteps.Text = "Skip coverage computation (faster)";
            //
            // chkSaveProteinToPeptideMappingFile
            //
            this.chkSaveProteinToPeptideMappingFile.Location = new System.Drawing.Point(480, 46);
            this.chkSaveProteinToPeptideMappingFile.Name = "chkSaveProteinToPeptideMappingFile";
            this.chkSaveProteinToPeptideMappingFile.Size = new System.Drawing.Size(269, 19);
            this.chkSaveProteinToPeptideMappingFile.TabIndex = 2;
            this.chkSaveProteinToPeptideMappingFile.Text = "Save protein to peptide mapping details";
            this.chkSaveProteinToPeptideMappingFile.CheckedChanged += new System.EventHandler(this.chkSearchAllProteinsSaveDetails_CheckedChanged);
            //
            // chkSearchAllProteinsForPeptideSequence
            //
            this.chkSearchAllProteinsForPeptideSequence.Location = new System.Drawing.Point(19, 46);
            this.chkSearchAllProteinsForPeptideSequence.Name = "chkSearchAllProteinsForPeptideSequence";
            this.chkSearchAllProteinsForPeptideSequence.Size = new System.Drawing.Size(288, 28);
            this.chkSearchAllProteinsForPeptideSequence.TabIndex = 1;
            this.chkSearchAllProteinsForPeptideSequence.Text = "Search All Proteins For Peptide Sequence";
            this.chkSearchAllProteinsForPeptideSequence.CheckedChanged += new System.EventHandler(this.chkSearchAllProteinsForPeptideSequence_CheckedChanged);
            //
            // chkOutputProteinSequence
            //
            this.chkOutputProteinSequence.Location = new System.Drawing.Point(19, 18);
            this.chkOutputProteinSequence.Name = "chkOutputProteinSequence";
            this.chkOutputProteinSequence.Size = new System.Drawing.Size(211, 28);
            this.chkOutputProteinSequence.TabIndex = 0;
            this.chkOutputProteinSequence.Text = "Output Protein Sequence";
            //
            // chkTrackPeptideCounts
            //
            this.chkTrackPeptideCounts.Location = new System.Drawing.Point(19, 83);
            this.chkTrackPeptideCounts.Name = "chkTrackPeptideCounts";
            this.chkTrackPeptideCounts.Size = new System.Drawing.Size(317, 19);
            this.chkTrackPeptideCounts.TabIndex = 4;
            this.chkTrackPeptideCounts.Text = "Track Unique And Non-Unique Peptide Counts";
            //
            // chkRemoveSymbolCharacters
            //
            this.chkRemoveSymbolCharacters.Location = new System.Drawing.Point(19, 111);
            this.chkRemoveSymbolCharacters.Name = "chkRemoveSymbolCharacters";
            this.chkRemoveSymbolCharacters.Size = new System.Drawing.Size(442, 18);
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
            this.TabPagePeakMatchingThresholds.Size = new System.Drawing.Size(859, 340);
            this.TabPagePeakMatchingThresholds.TabIndex = 3;
            this.TabPagePeakMatchingThresholds.Text = "Results Browser";
            this.TabPagePeakMatchingThresholds.Visible = false;
            //
            // txtCoverage
            //
            this.txtCoverage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.txtCoverage.Location = new System.Drawing.Point(614, 282);
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
            this.txtRTFCode.Size = new System.Drawing.Size(519, 172);
            this.txtRTFCode.TabIndex = 1;
            this.txtRTFCode.WordWrap = false;
            //
            // txtCustomProteinSequence
            //
            this.txtCustomProteinSequence.AcceptsReturn = true;
            this.txtCustomProteinSequence.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.txtCustomProteinSequence.Location = new System.Drawing.Point(106, 283);
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
            this.lblCustomProteinSequence.Location = new System.Drawing.Point(5, 283);
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
            this.rtfRichTextBox.Size = new System.Drawing.Size(234, 227);
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
            this.dgResults.Size = new System.Drawing.Size(605, 256);
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
            this.fraOutputFolderPath.Size = new System.Drawing.Size(885, 64);
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
            this.txtOutputFolderPath.Size = new System.Drawing.Size(741, 22);
            this.txtOutputFolderPath.TabIndex = 1;
            this.txtOutputFolderPath.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtOutputFolderPath_KeyPress);
            //
            // lblStatus
            //
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.Location = new System.Drawing.Point(657, 69);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(192, 51);
            this.lblStatus.TabIndex = 7;
            this.lblStatus.Text = "Status ...";
            //
            // GUI
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 15);
            this.ClientSize = new System.Drawing.Size(914, 601);
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
        #endregion

        #region "Constants and Enums"
        private const string XML_SETTINGS_FILE_NAME = "ProteinCoverageSummarizerSettings.xml";
        private const string XML_SECTION_GUI_OPTIONS = "GUIOptions";

        private const string COVERAGE_RESULTS_DATA_TABLE = "T_Coverage_Results";
        private const string COL_NAME_PROTEIN_NAME = "Protein Name";
        private const string COL_NAME_PROTEIN_COVERAGE = "Percent Coverage";
        private const string COL_NAME_PROTEIN_DESCRIPTION = "Protein Description";
        private const string COL_NAME_NON_UNIQUE_PEPTIDE_COUNT = "Non Unique Peptide Count";
        private const string COL_NAME_UNIQUE_PEPTIDE_COUNT = "Unique Peptide Count";
        private const string COL_NAME_PROTEIN_RESIDUE_COUNT = "Protein Residue count";
        private const string COL_NAME_PROTEIN_SEQUENCE = "Protein Sequence";

        private const int PROTEIN_INPUT_FILE_INDEX_OFFSET = 1;

        private enum DelimiterCharConstants
        {
            Space = 0,
            Tab = 1,
            Comma = 2,
            Other = 3
        }

        private enum eSequenceDisplayConstants
        {
            UsePrevious = 0,
            UseDataGrid = 1,
            UseCustom = 2
        }
        #endregion

        #region "Classwide variables"
        private DataSet mDSCoverageResults;
        private int mProteinSequenceColIndex;
        private bool mProteinDescriptionColVisible;

        private clsProteinCoverageSummarizerRunner mProteinCoverageSummarizer;

        private string mXmlSettingsFilePath;
        private bool mSaveFullSettingsFileOnExit;
        private string mLastFolderUsed;

        #endregion

        #region "Properties"
        public bool KeepDB { get; set; }

        #endregion

        private void CloseProgram()
        {
            Close();
        }

        private void AutoDefineSearchAllProteins()
        {
            if (cboPeptideInputFileColumnOrdering.SelectedIndex == (int)DelimitedFileReader.eDelimitedFileFormatCode.SequenceOnly)
            {
                chkSearchAllProteinsForPeptideSequence.Checked = true;
            }
            else
            {
                chkSearchAllProteinsForPeptideSequence.Checked = false;
            }
        }

        private bool ConfirmInputFilePaths()
        {
            if (txtProteinInputFilePath.Text.Length == 0 & txtPeptideInputFilePath.Text.Length == 0)
            {
                ShowErrorMessage("Please define the input file paths", "Missing Value");
                txtProteinInputFilePath.Focus();
                return false;
            }
            else if (txtProteinInputFilePath.Text.Length == 0)
            {
                ShowErrorMessage("Please define Protein input file path", "Missing Value");
                txtProteinInputFilePath.Focus();
                return false;
            }
            else if (txtPeptideInputFilePath.Text.Length == 0)
            {
                ShowErrorMessage("Please define Peptide input file path", "Missing Value");
                txtPeptideInputFilePath.Focus();
                return false;
            }
            else
            {
                return true;
            }
        }

        private void CreateSummaryDataTable(string strResultsFilePath)
        {
            StreamReader srInFile;
            long bytesRead = 0;

            int intLineCount;
            int intIndex;

            string strLineIn;
            string[] strSplitLine;

            bool blnProteinDescriptionPresent;

            DataRow objNewRow;
            try
            {
                if (strResultsFilePath == null || strResultsFilePath.Length == 0)
                {
                    // Output file not available
                    return;
                }

                if (!File.Exists(strResultsFilePath))
                {
                    ShowErrorMessage("Results file not found: " + strResultsFilePath);
                }

                // Clear the data source to prevent the data grid from updating
                dgResults.DataSource = null;

                // Clear the dataset
                mDSCoverageResults.Tables[COVERAGE_RESULTS_DATA_TABLE].Clear();

                // Open the file and read in the lines
                srInFile = new StreamReader(strResultsFilePath);
                intLineCount = 1;
                blnProteinDescriptionPresent = false;

                while (srInFile.Peek() != -1)
                {
                    strLineIn = srInFile.ReadLine();
                    bytesRead += strLineIn.Length + 2;           // Add 2 for CrLf

                    if (intLineCount == 1)
                    {
                        // do nothing, skip the first line
                    }
                    else
                    {
                        strSplitLine = strLineIn.Split('\t');

                        objNewRow = mDSCoverageResults.Tables[COVERAGE_RESULTS_DATA_TABLE].NewRow();
                        for (intIndex = 0; intIndex <= strSplitLine.Length - 1; intIndex++)
                        {
                            if (intIndex > clsProteinCoverageSummarizer.OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER - 1)
                                break;

                            try
                            {
                                switch (Type.GetTypeCode(objNewRow[intIndex].GetType()))
                                {
                                    case TypeCode.String:
                                        objNewRow[intIndex] = strSplitLine[intIndex];
                                        break;
                                    case TypeCode.Double:
                                        objNewRow[intIndex] = Convert.ToDouble(strSplitLine[intIndex]);
                                        break;
                                    case TypeCode.Single:
                                        objNewRow[intIndex] = Convert.ToSingle(strSplitLine[intIndex]);
                                        break;
                                    case TypeCode.Byte:
                                    case TypeCode.Int16:
                                    case TypeCode.Int32:
                                    case TypeCode.Int64:
                                    case TypeCode.UInt16:
                                    case TypeCode.UInt32:
                                    case TypeCode.UInt64:
                                        objNewRow[intIndex] = Convert.ToInt32(strSplitLine[intIndex]);
                                        break;
                                    case TypeCode.Boolean:
                                        objNewRow[intIndex] = Convert.ToBoolean(strSplitLine[intIndex]);
                                        break;
                                    default:
                                        objNewRow[intIndex] = strSplitLine[intIndex];
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Ignore errors while populating the table
                            }
                        }

                        if (strSplitLine.Length >= clsProteinCoverageSummarizer.OUTPUT_FILE_PROTEIN_DESCRIPTION_COLUMN_NUMBER)
                        {
                            if (strSplitLine[clsProteinCoverageSummarizer.OUTPUT_FILE_PROTEIN_DESCRIPTION_COLUMN_NUMBER - 1].Length > 0)
                            {
                                blnProteinDescriptionPresent = true;
                            }
                        }

                        // Add the row to the Customers table.
                        mDSCoverageResults.Tables[COVERAGE_RESULTS_DATA_TABLE].Rows.Add(objNewRow);
                    }

                    intLineCount += 1;
                    if (intLineCount % 25 == 0)
                    {
                        lblProgress.Text = "Loading results: " + (bytesRead / (double)srInFile.BaseStream.Length * 100).ToString("0.0") + "% complete";
                    }
                }

                srInFile.Close();

                // Re-define the data source
                // Bind the DataSet to the DataGrid
                dgResults.DataSource = mDSCoverageResults;
                dgResults.DataMember = COVERAGE_RESULTS_DATA_TABLE;

                if (blnProteinDescriptionPresent != mProteinDescriptionColVisible)
                {
                    mProteinDescriptionColVisible = blnProteinDescriptionPresent;
                    UpdateDataGridTableStyle();
                }

                // Display the sequence for the first protein
                if (mDSCoverageResults.Tables[COVERAGE_RESULTS_DATA_TABLE].Rows.Count > 0)
                {
                    dgResults.CurrentRowIndex = 0;
                    ShowRichTextStart(eSequenceDisplayConstants.UseDataGrid);
                }
                else
                {
                    ShowRichText("", rtfRichTextBox);
                }

                lblProgress.Text = "Results loaded";
            }
            catch (Exception ex)
            {
            }
        }

        private void DefineOutputFolderPath(string strPeptideInputFilePath)
        {
            try
            {
                if (strPeptideInputFilePath.Length > 0)
                {
                    txtOutputFolderPath.Text = Path.GetDirectoryName(strPeptideInputFilePath);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error defining default output folder path: " + ex.Message, "Error");
            }
        }

        private void EnableDisableControls()
        {
            bool blnFastaFile = clsProteinFileDataCache.IsFastaFile(txtProteinInputFilePath.Text);

            cboProteinInputFileColumnOrdering.Enabled = !blnFastaFile;
            cboProteinInputFileColumnDelimiter.Enabled = !blnFastaFile;
            txtProteinInputFileColumnDelimiter.Enabled = !blnFastaFile;

            chkSearchAllProteinsSkipCoverageComputationSteps.Enabled = chkSearchAllProteinsForPeptideSequence.Checked;
        }

        private string GetSettingsFilePath()
        {
            return ProcessFilesOrDirectoriesBase.GetSettingsFilePathLocal("ProteinCoverageSummarizer", XML_SETTINGS_FILE_NAME);
        }

        private void IniFileLoadOptions(bool blnUpdateIOPaths)
        {
            // Prompts the user to select a file to load the options from

            string strFilePath;

            var objOpenFile = new OpenFileDialog();

            strFilePath = mXmlSettingsFilePath;

            objOpenFile.AddExtension = true;
            objOpenFile.CheckFileExists = true;
            objOpenFile.CheckPathExists = true;
            objOpenFile.DefaultExt = ".xml";
            objOpenFile.DereferenceLinks = true;
            objOpenFile.Multiselect = false;
            objOpenFile.ValidateNames = true;

            objOpenFile.Filter = "Settings files (*.xml)|*.xml|All files (*.*)|*.*";

            objOpenFile.FilterIndex = 1;

            if (strFilePath.Length > 0)
            {
                try
                {
                    objOpenFile.InitialDirectory = Directory.GetParent(strFilePath).ToString();
                }
                catch
                {
                    objOpenFile.InitialDirectory = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
                }
            }
            else
            {
                objOpenFile.InitialDirectory = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
            }

            if (File.Exists(strFilePath))
            {
                objOpenFile.FileName = Path.GetFileName(strFilePath);
            }

            objOpenFile.Title = "Specify file to load options from";

            objOpenFile.ShowDialog();
            if (objOpenFile.FileName.Length > 0)
            {
                mXmlSettingsFilePath = objOpenFile.FileName;

                IniFileLoadOptions(mXmlSettingsFilePath, blnUpdateIOPaths);
            }
        }

        private void IniFileLoadOptions(string strFilePath, bool blnUpdateIOPaths)
        {
            XmlSettingsFileAccessor objSettingsFile;

            var objProteinCoverageSummarizer = new clsProteinCoverageSummarizerRunner();
            DelimitedFileReader.eDelimitedFileFormatCode eColumnOrdering;

            try
            {
                if (strFilePath == null || strFilePath.Length == 0)
                {
                    // No parameter file specified; nothing to load
                    return;
                }

                if (!File.Exists(strFilePath))
                {
                    ShowErrorMessage("Parameter file not Found: " + strFilePath);
                    return;
                }

                objSettingsFile = new XmlSettingsFileAccessor();

                if (objSettingsFile.LoadSettings(strFilePath))
                {
                    // Read the GUI-specific options from the XML file
                    if (!objSettingsFile.SectionPresent(XML_SECTION_GUI_OPTIONS))
                    {
                        ShowErrorMessage("The node '<section name=\"" + XML_SECTION_GUI_OPTIONS + "\"> was not found in the parameter file: " + strFilePath, "Invalid File");
                        mSaveFullSettingsFileOnExit = true;
                    }
                    else
                    {
                        if (blnUpdateIOPaths)
                        {
                            txtProteinInputFilePath.Text = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFilePath", txtProteinInputFilePath.Text);
                            txtPeptideInputFilePath.Text = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFilePath", txtPeptideInputFilePath.Text);
                            txtOutputFolderPath.Text = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "OutputFolderPath", txtOutputFolderPath.Text);
                        }

                        cboProteinInputFileColumnDelimiter.SelectedIndex = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFileColumnDelimiterIndex", cboProteinInputFileColumnDelimiter.SelectedIndex);
                        txtProteinInputFileColumnDelimiter.Text = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFileColumnDelimiter", txtProteinInputFileColumnDelimiter.Text);

                        cboPeptideInputFileColumnDelimiter.SelectedIndex = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFileColumnDelimiterIndex", cboPeptideInputFileColumnDelimiter.SelectedIndex);
                        txtPeptideInputFileColumnDelimiter.Text = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFileColumnDelimiter", txtPeptideInputFileColumnDelimiter.Text);

                        cboCharactersPerLine.SelectedIndex = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "ProteinSequenceCharactersPerLine", cboCharactersPerLine.SelectedIndex);
                        chkAddSpace.Checked = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "ProteinSequenceAddSpace", chkAddSpace.Checked);

                        if (!objSettingsFile.SectionPresent(clsProteinCoverageSummarizer.XML_SECTION_PROCESSING_OPTIONS))
                        {
                            this.ShowErrorMessage("The node '<section name=\"" + clsProteinCoverageSummarizer.XML_SECTION_PROCESSING_OPTIONS + "\"> was not found in the parameter file: ", "Invalid File");
                            mSaveFullSettingsFileOnExit = true;
                        }
                        else
                        {
                            try
                            {
                                eColumnOrdering = (DelimitedFileReader.eDelimitedFileFormatCode)objSettingsFile.GetParam(clsProteinCoverageSummarizer.XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileFormatCode", cboProteinInputFileColumnOrdering.SelectedIndex + PROTEIN_INPUT_FILE_INDEX_OFFSET);
                            }
                            catch (Exception ex)
                            {
                                eColumnOrdering = DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence;
                            }

                            try
                            {
                                cboProteinInputFileColumnOrdering.SelectedIndex = (int)eColumnOrdering - PROTEIN_INPUT_FILE_INDEX_OFFSET;
                            }
                            catch (Exception ex)
                            {
                                if (cboProteinInputFileColumnOrdering.Items.Count > 0)
                                {
                                    cboProteinInputFileColumnOrdering.SelectedIndex = 0;
                                }
                            }

                            try
                            {
                                eColumnOrdering = (DelimitedFileReader.eDelimitedFileFormatCode)objSettingsFile.GetParam(clsProteinCoverageSummarizer.XML_SECTION_PROCESSING_OPTIONS, "PeptideFileFormatCode", cboPeptideInputFileColumnOrdering.SelectedIndex);
                            }
                            catch (Exception ex)
                            {
                                eColumnOrdering = DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence;
                            }

                            try
                            {
                                cboPeptideInputFileColumnOrdering.SelectedIndex = (int)eColumnOrdering;
                            }
                            catch (Exception ex)
                            {
                                if (cboPeptideInputFileColumnOrdering.Items.Count > 0)
                                {
                                    cboPeptideInputFileColumnOrdering.SelectedIndex = 0;
                                }
                            }

                            // Note: The following settings are read using LoadProcessingClassOptions()
                            // chkPeptideFileSkipFirstLine
                            // chkProteinFileSkipFirstLine

                            // chkOutputProteinSequence
                            // chkSearchAllProteinsForPeptideSequence
                            // chkSearchAllProteinsSaveDetails
                            // chkSearchAllProteinsSkipCoverageComputationSteps
                            // chkTrackPeptideCounts
                            // chkRemoveSymbolCharacters
                            // chkMatchPeptidePrefixAndSuffixToProtein
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error in IniFileLoadOptions: " + ex.Message);
            }

            try
            {
                objProteinCoverageSummarizer.LoadParameterFileSettings(strFilePath);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error calling LoadParameterFileSettings: " + ex.ToString());
            }

            try
            {
                LoadProcessingClassOptions(ref objProteinCoverageSummarizer);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error calling LoadProcessingClassOptions: " + ex.ToString());
            }
        }

        private void IniFileSaveOptions(string strSettingsFilePath, bool blnSaveExtendedOptions = false)
        {
            var objSettingsFile = new XmlSettingsFileAccessor();

            const string XML_SECTION_PROCESSING_OPTIONS = "ProcessingOptions";

            try
            {
                var fiSettingsFile = new FileInfo(strSettingsFilePath);
                if (!fiSettingsFile.Exists)
                {
                    blnSaveExtendedOptions = true;
                }
            }
            catch
            {
                // Ignore errors here
            }

            // Pass True to .LoadSettings() to turn off case sensitive matching
            try
            {
                objSettingsFile.LoadSettings(strSettingsFilePath, true);
            }
            catch (Exception ex)
            {
                return;
            }

            try
            {
                objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFilePath", txtProteinInputFilePath.Text);
                objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFilePath", txtPeptideInputFilePath.Text);
                objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "OutputFolderPath", txtOutputFolderPath.Text);

                if (blnSaveExtendedOptions)
                {
                    objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFileColumnDelimiterIndex", cboProteinInputFileColumnDelimiter.SelectedIndex);
                    objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFileColumnDelimiter", txtProteinInputFileColumnDelimiter.Text);

                    objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFileColumnDelimiterIndex", cboPeptideInputFileColumnDelimiter.SelectedIndex);
                    objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFileColumnDelimiter", txtPeptideInputFileColumnDelimiter.Text);

                    objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "ProteinSequenceCharactersPerLine", cboCharactersPerLine.SelectedIndex);
                    objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "ProteinSequenceAddSpace", chkAddSpace.Checked);

                    objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "OutputProteinSequence", chkOutputProteinSequence.Checked);
                    objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "SearchAllProteinsForPeptideSequence", chkSearchAllProteinsForPeptideSequence.Checked);

                    objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "SaveProteinToPeptideMappingFile", chkSaveProteinToPeptideMappingFile.Checked);
                    objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "SearchAllProteinsSkipCoverageComputationSteps", chkSearchAllProteinsSkipCoverageComputationSteps.Checked);

                    objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "TrackPeptideCounts", chkTrackPeptideCounts.Checked);
                    objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "RemoveSymbolCharacters", chkRemoveSymbolCharacters.Checked);
                    objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "MatchPeptidePrefixAndSuffixToProtein", chkMatchPeptidePrefixAndSuffixToProtein.Checked);
                    objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "IgnoreILDifferences", chkIgnoreILDifferences.Checked);

                    objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideInputFileDelimiter", LookupColumnDelimiter(cboPeptideInputFileColumnDelimiter, txtPeptideInputFileColumnDelimiter, '\t'));
                    objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideFileFormatCode", cboPeptideInputFileColumnOrdering.SelectedIndex);
                    objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideFileSkipFirstLine", chkPeptideFileSkipFirstLine.Checked);

                    objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileDelimiter", LookupColumnDelimiter(cboPeptideInputFileColumnDelimiter, txtPeptideInputFileColumnDelimiter, '\t'));
                    objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileFormatCode", cboProteinInputFileColumnOrdering.SelectedIndex + PROTEIN_INPUT_FILE_INDEX_OFFSET);
                    objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "ProteinFileSkipFirstLine", chkProteinFileSkipFirstLine.Checked);
                }

                objSettingsFile.SaveSettings();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error storing parameter in settings file: " + Path.GetFileName(strSettingsFilePath), "Error");
            }
        }

        private void InitializeControls()
        {
            cmdAbort.Visible = false;
            cmdStart.Visible = true;
            txtRTFCode.Visible = false;

            mLastFolderUsed = Application.StartupPath;

            lblProgress.Text = string.Empty;
            lblStatus.Text = string.Empty;

            PopulateComboBoxes();
            InitializeDataGrid();

            ResetToDefaults();

            try
            {
                // Try loading from the default xml file
                IniFileLoadOptions(mXmlSettingsFilePath, true);
            }
            catch (Exception ex)
            {
                // Ignore any errors here
                ShowErrorMessage("Error loading settings from " + mXmlSettingsFilePath + ": " + ex.Message);
            }
        }

        private void InitializeDataGrid()
        {
            try
            {

                // Make the Peak Matching Thresholds data table
                var dtCoverageResults = new DataTable(COVERAGE_RESULTS_DATA_TABLE);

                // Add the columns to the data table
                PRISMDatabaseUtils.DataTableUtils.AppendColumnStringToTable(dtCoverageResults, COL_NAME_PROTEIN_NAME, string.Empty);
                PRISMDatabaseUtils.DataTableUtils.AppendColumnFloatToTable(dtCoverageResults, COL_NAME_PROTEIN_COVERAGE);
                PRISMDatabaseUtils.DataTableUtils.AppendColumnStringToTable(dtCoverageResults, COL_NAME_PROTEIN_DESCRIPTION, string.Empty);
                PRISMDatabaseUtils.DataTableUtils.AppendColumnIntegerToTable(dtCoverageResults, COL_NAME_NON_UNIQUE_PEPTIDE_COUNT);
                PRISMDatabaseUtils.DataTableUtils.AppendColumnIntegerToTable(dtCoverageResults, COL_NAME_UNIQUE_PEPTIDE_COUNT);
                PRISMDatabaseUtils.DataTableUtils.AppendColumnIntegerToTable(dtCoverageResults, COL_NAME_PROTEIN_RESIDUE_COUNT);
                PRISMDatabaseUtils.DataTableUtils.AppendColumnStringToTable(dtCoverageResults, COL_NAME_PROTEIN_SEQUENCE, string.Empty);

                // Note that Protein Sequence should be at ColIndex 6 = clsProteinCoverageSummarizer.OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER-1
                mProteinSequenceColIndex = clsProteinCoverageSummarizer.OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER - 1;

                // Could define a primary key if we wanted
                // dtCoverageResults.PrimaryKey = dtCoverageResults.Columns(COL_NAME_PROTEIN_NAME);

                // Instantiate the dataset
                mDSCoverageResults = new DataSet(COVERAGE_RESULTS_DATA_TABLE);

                // Add the new DataTable to the DataSet.
                mDSCoverageResults.Tables.Add(dtCoverageResults);

                // Bind the DataSet to the DataGrid
                dgResults.DataSource = mDSCoverageResults;
                dgResults.DataMember = COVERAGE_RESULTS_DATA_TABLE;

                mProteinDescriptionColVisible = false;

                // Update the grid's table style
                UpdateDataGridTableStyle();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error in InitializeDataGrid: " + ex.ToString());
            }
        }

        private void LoadProcessingClassOptions(ref clsProteinCoverageSummarizerRunner objProteinCoverageSummarizer)
        {
            try
            {
                chkPeptideFileSkipFirstLine.Checked = objProteinCoverageSummarizer.PeptideFileSkipFirstLine;
                chkProteinFileSkipFirstLine.Checked = objProteinCoverageSummarizer.ProteinDataDelimitedFileSkipFirstLine;

                chkOutputProteinSequence.Checked = objProteinCoverageSummarizer.OutputProteinSequence;
                chkSearchAllProteinsForPeptideSequence.Checked = objProteinCoverageSummarizer.SearchAllProteinsForPeptideSequence;

                chkSaveProteinToPeptideMappingFile.Checked = objProteinCoverageSummarizer.SaveProteinToPeptideMappingFile;
                chkSearchAllProteinsSkipCoverageComputationSteps.Checked = objProteinCoverageSummarizer.SearchAllProteinsSkipCoverageComputationSteps;

                chkTrackPeptideCounts.Checked = objProteinCoverageSummarizer.TrackPeptideCounts;
                chkRemoveSymbolCharacters.Checked = objProteinCoverageSummarizer.RemoveSymbolCharacters;
                chkMatchPeptidePrefixAndSuffixToProtein.Checked = objProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein;
                chkIgnoreILDifferences.Checked = objProteinCoverageSummarizer.IgnoreILDifferences;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error in LoadProcessingClassOptions: " + ex.Message);
            }
        }

        private char LookupColumnDelimiter(ListControl delimiterCombobox, Control delimiterTextBox, char defaultDelimiter)
        {
            try
            {
                return LookupColumnDelimiterChar(delimiterCombobox.SelectedIndex, delimiterTextBox.Text, defaultDelimiter);
            }
            catch (Exception ex)
            {
                return '\t';
            }
        }

        private char LookupColumnDelimiterChar(int delimiterIndex, string customDelimiter, char defaultDelimiter)
        {
            string delimiter;

            switch (delimiterIndex)
            {
                case (int)DelimiterCharConstants.Space:
                    delimiter = " ";
                    break;
                case (int)DelimiterCharConstants.Tab:
                    delimiter = Convert.ToString('\t');
                    break;
                case (int)DelimiterCharConstants.Comma:
                    delimiter = ",";
                    break;
                default:
                    // Includes DelimiterCharConstants.Other
                    delimiter = string.Copy(customDelimiter);
                    break;
            }

            if (delimiter == null || delimiter.Length == 0)
            {
                delimiter = string.Copy(Convert.ToString(defaultDelimiter));
            }

            try
            {
                return delimiter[0];
            }
            catch (Exception ex)
            {
                return '\t';
            }
        }

        private void PopulateComboBoxes()
        {
            cboProteinInputFileColumnDelimiter.Items.Clear();
            cboProteinInputFileColumnDelimiter.Items.Insert(0, "Space");
            cboProteinInputFileColumnDelimiter.Items.Insert(1, "Tab");
            cboProteinInputFileColumnDelimiter.Items.Insert(2, "Comma");
            cboProteinInputFileColumnDelimiter.Items.Insert(3, "Other");

            cboProteinInputFileColumnDelimiter.SelectedIndex = 1;

            cboPeptideInputFileColumnDelimiter.Items.Insert(0, "Space");
            cboPeptideInputFileColumnDelimiter.Items.Insert(1, "Tab");
            cboPeptideInputFileColumnDelimiter.Items.Insert(2, "Comma");
            cboPeptideInputFileColumnDelimiter.Items.Insert(3, "Other");

            cboPeptideInputFileColumnDelimiter.SelectedIndex = 1;

            cboCharactersPerLine.Items.Clear();
            cboCharactersPerLine.Items.Insert(0, " 40 Characters per line");
            cboCharactersPerLine.Items.Insert(1, " 50 Characters per line");
            cboCharactersPerLine.Items.Insert(2, " 60 Characters per line");

            cboCharactersPerLine.SelectedIndex = 0;

            cboProteinInputFileColumnOrdering.Items.Clear();
            // Note: Skipping ProteinFileReader.(int)DelimitedFileReader.eDelimitedFileFormatCode.SequenceOnly since a Protein Sequence Only file is inappropriate for this program
            cboProteinInputFileColumnOrdering.Items.Insert((int)DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence - PROTEIN_INPUT_FILE_INDEX_OFFSET, "ProteinName and Sequence");
            cboProteinInputFileColumnOrdering.Items.Insert((int)DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Description_Sequence - PROTEIN_INPUT_FILE_INDEX_OFFSET, "ProteinName, Description, Seq");
            cboProteinInputFileColumnOrdering.Items.Insert((int)DelimitedFileReader.eDelimitedFileFormatCode.UniqueID_Sequence - PROTEIN_INPUT_FILE_INDEX_OFFSET, "UniqueID and Seq");
            cboProteinInputFileColumnOrdering.Items.Insert((int)DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_PeptideSequence_UniqueID - PROTEIN_INPUT_FILE_INDEX_OFFSET, "ProteinName, Seq, UniqueID");
            cboProteinInputFileColumnOrdering.Items.Insert((int)DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_PeptideSequence_UniqueID_Mass_NET - PROTEIN_INPUT_FILE_INDEX_OFFSET, "ProteinName, Seq, UniqueID, Mass, Time");
            cboProteinInputFileColumnOrdering.Items.Insert((int)DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_PeptideSequence_UniqueID_Mass_NET_NETStDev_DiscriminantScore - PROTEIN_INPUT_FILE_INDEX_OFFSET, "ProteinName, Seq, UniqueID, Mass, Time, TimeStDev, DiscriminantScore");
            cboProteinInputFileColumnOrdering.Items.Insert((int)DelimitedFileReader.eDelimitedFileFormatCode.UniqueID_Sequence_Mass_NET - PROTEIN_INPUT_FILE_INDEX_OFFSET, "UniqueID, Seq, Mass, Time");

            cboProteinInputFileColumnOrdering.SelectedIndex = (int)DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Description_Sequence - PROTEIN_INPUT_FILE_INDEX_OFFSET;

            cboPeptideInputFileColumnOrdering.Items.Clear();
            cboPeptideInputFileColumnOrdering.Items.Insert((int)DelimitedFileReader.eDelimitedFileFormatCode.SequenceOnly, "Sequence Only");
            cboPeptideInputFileColumnOrdering.Items.Insert((int)DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence, "ProteinName and Sequence");

            cboPeptideInputFileColumnOrdering.SelectedIndex = (int)DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence;
        }

        private void ResetToDefaults()
        {
            cboProteinInputFileColumnOrdering.SelectedIndex = (int)DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence - PROTEIN_INPUT_FILE_INDEX_OFFSET;
            cboProteinInputFileColumnDelimiter.SelectedIndex = 1;
            txtProteinInputFileColumnDelimiter.Text = ";";
            chkProteinFileSkipFirstLine.Checked = false;

            cboPeptideInputFileColumnOrdering.SelectedIndex = (int)DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence;
            cboPeptideInputFileColumnDelimiter.SelectedIndex = 1;
            txtPeptideInputFileColumnDelimiter.Text = ";";
            chkPeptideFileSkipFirstLine.Checked = false;

            chkOutputProteinSequence.Checked = true;
            chkSearchAllProteinsForPeptideSequence.Checked = false;

            chkSaveProteinToPeptideMappingFile.Checked = true;
            chkSearchAllProteinsSkipCoverageComputationSteps.Checked = false;

            chkTrackPeptideCounts.Checked = true;
            chkRemoveSymbolCharacters.Checked = true;
            chkMatchPeptidePrefixAndSuffixToProtein.Checked = false;

            cboCharactersPerLine.SelectedIndex = 0;
            chkAddSpace.Checked = true;

            mXmlSettingsFilePath = GetSettingsFilePath();
            ProcessFilesOrDirectoriesBase.CreateSettingsFileIfMissing(mXmlSettingsFilePath);
        }

        private void SelectOutputFolder()
        {
            var folderBrowserDialog = new VistaFolderBrowserDialog();

            if (txtOutputFolderPath.TextLength > 0)
            {
                folderBrowserDialog.SelectedPath = txtOutputFolderPath.Text;
            }
            else
            {
                folderBrowserDialog.SelectedPath = mLastFolderUsed;
            }

            var result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtOutputFolderPath.Text = folderBrowserDialog.SelectedPath;
                mLastFolderUsed = folderBrowserDialog.SelectedPath;
            }
        }

        private void SelectProteinInputFile()
        {
            DialogResult eResult;
            var dlgOpenFileDialog = new OpenFileDialog()
            {
                Filter = "Fasta Files (*.fasta)|*.fasta|Text Files(*.txt)|*.txt|All Files (*.*)|*.*",
                FilterIndex = 3
            };

            eResult = dlgOpenFileDialog.ShowDialog();
            if (eResult == DialogResult.OK)
            {
                txtProteinInputFilePath.Text = dlgOpenFileDialog.FileName;
                mLastFolderUsed = Path.GetDirectoryName(dlgOpenFileDialog.FileName);
            }
        }

        private void SelectPeptideInputFile()
        {
            DialogResult eResult;
            OpenFileDialog dlgOpenFileDialog;

            dlgOpenFileDialog = new OpenFileDialog()
            {
                InitialDirectory = mLastFolderUsed,
                Filter = "Text Files(*.txt)|*.txt|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            eResult = dlgOpenFileDialog.ShowDialog();
            if (eResult == DialogResult.OK)
            {
                txtPeptideInputFilePath.Text = dlgOpenFileDialog.FileName;
                mLastFolderUsed = Path.GetDirectoryName(dlgOpenFileDialog.FileName);
            }
        }

        private bool SetOptionsFromGUI(clsProteinCoverageSummarizerRunner objProteinCoverageSummarizer)
        {
            try
            {
                objProteinCoverageSummarizer.ProteinInputFilePath = txtProteinInputFilePath.Text;

                objProteinCoverageSummarizer.ProteinDataDelimitedFileFormatCode = (DelimitedFileReader.eDelimitedFileFormatCode)(cboProteinInputFileColumnOrdering.SelectedIndex + PROTEIN_INPUT_FILE_INDEX_OFFSET);
                objProteinCoverageSummarizer.ProteinDataDelimitedFileDelimiter = LookupColumnDelimiter(cboProteinInputFileColumnDelimiter, txtProteinInputFileColumnDelimiter, '\t');
                objProteinCoverageSummarizer.ProteinDataDelimitedFileSkipFirstLine = chkProteinFileSkipFirstLine.Checked;
                objProteinCoverageSummarizer.ProteinDataRemoveSymbolCharacters = chkRemoveSymbolCharacters.Checked;
                objProteinCoverageSummarizer.ProteinDataIgnoreILDifferences = chkIgnoreILDifferences.Checked;

                // peptide file options
                objProteinCoverageSummarizer.PeptideFileFormatCode = (clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode)Convert.ToInt32(cboPeptideInputFileColumnOrdering.SelectedIndex);
                objProteinCoverageSummarizer.PeptideInputFileDelimiter = LookupColumnDelimiter(cboPeptideInputFileColumnDelimiter, txtPeptideInputFileColumnDelimiter, '\t');
                objProteinCoverageSummarizer.PeptideFileSkipFirstLine = chkPeptideFileSkipFirstLine.Checked;

                // processing options
                objProteinCoverageSummarizer.OutputProteinSequence = chkOutputProteinSequence.Checked;
                objProteinCoverageSummarizer.SearchAllProteinsForPeptideSequence = chkSearchAllProteinsForPeptideSequence.Checked;

                objProteinCoverageSummarizer.SaveProteinToPeptideMappingFile = chkSaveProteinToPeptideMappingFile.Checked;

                if (chkSaveProteinToPeptideMappingFile.Checked)
                {
                    objProteinCoverageSummarizer.SearchAllProteinsSkipCoverageComputationSteps = chkSearchAllProteinsSkipCoverageComputationSteps.Checked;
                }
                else
                {
                    objProteinCoverageSummarizer.SearchAllProteinsSkipCoverageComputationSteps = false;
                }

                objProteinCoverageSummarizer.TrackPeptideCounts = chkTrackPeptideCounts.Checked;
                objProteinCoverageSummarizer.RemoveSymbolCharacters = chkRemoveSymbolCharacters.Checked;
                objProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein = chkMatchPeptidePrefixAndSuffixToProtein.Checked;
                objProteinCoverageSummarizer.IgnoreILDifferences = chkIgnoreILDifferences.Checked;
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        private void ShowAboutBox()
        {
            var message = new StringBuilder();

            message.AppendLine("This program reads in a .fasta or .txt file containing protein names and sequences (and optionally descriptions).");
            message.AppendLine("The program also reads in a .txt file containing peptide sequences and protein names (though protein name is optional) then uses this information to compute the sequence coverage percent for each protein.");
            message.AppendLine();
            message.AppendLine("Program written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA) in 2005");
            message.AppendLine();
            message.AppendLine("This is version " + Application.ProductVersion + " (" + Program.PROGRAM_DATE + ")");
            message.AppendLine();
            message.AppendLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
            message.AppendLine("Website: https://omics.pnl.gov or https://panomics.pnl.gov/");
            message.AppendLine();
            message.AppendLine("Licensed under the 2-Clause BSD License; https://opensource.org/licenses/BSD-2-Clause");
            message.AppendLine("Copyright 2018 Battelle Memorial Institute");

            MessageBox.Show(message.ToString(), "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowRichTextStart()
        {
            ShowRichTextStart(eSequenceDisplayConstants.UsePrevious);
        }

        private bool lastSequenceWasDataGrid = false;

        private void ShowRichTextStart(eSequenceDisplayConstants eSequenceDisplayMode)
        {
            bool useDataGrid;

            switch (eSequenceDisplayMode)
            {
                case eSequenceDisplayConstants.UseDataGrid:
                    useDataGrid = true;
                    break;
                case eSequenceDisplayConstants.UseCustom:
                    useDataGrid = false;
                    break;
                default:
                    // Includes Use Previous
                    useDataGrid = lastSequenceWasDataGrid;
                    break;
            }

            lastSequenceWasDataGrid = useDataGrid;
            if (useDataGrid)
            {
                try
                {
                    if (dgResults.CurrentRowIndex >= 0)
                    {
                        if (dgResults[dgResults.CurrentRowIndex, mProteinSequenceColIndex] is object)
                        {
                            ShowRichText(Convert.ToString(dgResults[dgResults.CurrentRowIndex, mProteinSequenceColIndex]), rtfRichTextBox);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Ignore errors here
                }
            }
            else
            {
                ShowRichText(txtCustomProteinSequence.Text, rtfRichTextBox);
            }
        }

        protected void ShowErrorMessage(string strMessage)
        {
            ShowErrorMessage(strMessage, "Error");
        }

        protected void ShowErrorMessage(string strMessage, string strCaption)
        {
            MessageBox.Show(strMessage, strCaption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void ShowRichText(string strSequenceToShow, RichTextBox objRichTextBox)
        {
            int intIndex;
            int intModValue;

            int intCharCount;
            int intUppercaseCount;
            float sngCoveragePercent;

            string strRtf;
            Regex reReplaceSymbols;

            // Define a RegEx to remove whitespace characters
            reReplaceSymbols = new Regex(@"[ \t\r\n]", RegexOptions.Compiled);

            bool blnInUpperRegion;

            try
            {
                // Lookup the number of characters per line
                var switchExpr = cboCharactersPerLine.SelectedIndex;
                switch (switchExpr)
                {
                    case 0:
                        intModValue = 40;
                        break;
                    case 1:
                        intModValue = 50;
                        break;
                    case 2:
                        intModValue = 60;
                        break;
                    default:
                        intModValue = 40;
                        break;
                }

                // Remove any spaces, tabs, CR, or LF characters in strSequenceToShow
                strSequenceToShow = reReplaceSymbols.Replace(strSequenceToShow, string.Empty);

                // Define the base RTF text
                // ReSharper disable StringLiteralTypo
                strRtf = @"{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Courier New;}}" +
                    @"{\colortbl\red0\green0\blue0;\red255\green0\blue0;}" +
                    @"\viewkind4\uc1\pard\f0\fs20 ";
                // ReSharper restore StringLiteralTypo

                blnInUpperRegion = false;
                intCharCount = 0;
                intUppercaseCount = 0;
                if (strSequenceToShow == null)
                    strSequenceToShow = string.Empty;

                for (intIndex = 0; intIndex <= strSequenceToShow.Length - 1; intIndex++)
                {
                    if (intIndex > 0)
                    {
                        if (intIndex % intModValue == 0)
                        {
                            // Add a new line
                            strRtf += @"\par ";
                        }
                        else if (chkAddSpace.Checked == true && intIndex % 10 == 0)
                        {
                            // Add a space every 10 residues
                            strRtf += " ";
                        }
                    }

                    if (char.IsUpper(strSequenceToShow[intIndex]))
                    {
                        intCharCount += 1;
                        intUppercaseCount += 1;
                        if (!blnInUpperRegion)
                        {
                            strRtf += @"{\cf1 {\b ";
                            blnInUpperRegion = true;
                        }
                    }
                    else
                    {
                        if (char.IsLower(strSequenceToShow[intIndex]))
                        {
                            intCharCount += 1;
                        }

                        if (blnInUpperRegion)
                        {
                            strRtf += "}}";
                            blnInUpperRegion = false;
                        }
                    }

                    strRtf += Convert.ToString(strSequenceToShow[intIndex]);
                }

                // Add a final paragraph mark
                strRtf += @"\par}";

                objRichTextBox.Rtf = strRtf;

                txtRTFCode.Text = objRichTextBox.Rtf;

                if (intCharCount > 0)
                {
                    sngCoveragePercent = Convert.ToSingle(intUppercaseCount / (double)intCharCount * 100);
                }
                else
                {
                    sngCoveragePercent = 0;
                }

                txtCoverage.Text = "Coverage: " + Math.Round(sngCoveragePercent, 3) + "%  (" + intUppercaseCount + " / " + intCharCount + ")";
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error in ShowRichText: " + ex.Message);
            }
        }

        private void Start()
        {
            bool blnSuccess;
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                cmdAbort.Visible = true;
                cmdStart.Visible = false;

                mProteinCoverageSummarizer = new clsProteinCoverageSummarizerRunner()
                {
                    CallingAppHandlesEvents = true,
                    KeepDB = KeepDB
                };

                this.mProteinCoverageSummarizer.StatusEvent += ProteinCoverageSummarizer_StatusEvent;
                this.mProteinCoverageSummarizer.ErrorEvent += ProteinCoverageSummarizer_ErrorEvent;
                this.mProteinCoverageSummarizer.WarningEvent += ProteinCoverageSummarizer_WarningEvent;

                this.mProteinCoverageSummarizer.ProgressUpdate += ProteinCoverageSummarizer_ProgressChanged;
                this.mProteinCoverageSummarizer.ProgressReset += ProteinCoverageSummarizer_ProgressReset;

                blnSuccess = SetOptionsFromGUI(mProteinCoverageSummarizer);
                if (blnSuccess)
                {
                    blnSuccess = mProteinCoverageSummarizer.ProcessFile(txtPeptideInputFilePath.Text, txtOutputFolderPath.Text);

                    if (blnSuccess & !(mProteinCoverageSummarizer.SearchAllProteinsForPeptideSequence & mProteinCoverageSummarizer.SearchAllProteinsSkipCoverageComputationSteps))
                    {
                        CreateSummaryDataTable(mProteinCoverageSummarizer.ResultsFilePath);
                    }

                    if (lblStatus.Text.StartsWith("Done (9"))
                    {
                        lblStatus.Text = "Done";
                    }
                }
                else
                {
                    ShowErrorMessage("Error initializing Protein File Parser General Options.");
                }

                Cursor.Current = Cursors.Default;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error in Start: " + ex.Message);
            }
            finally
            {
                cmdAbort.Visible = false;
                cmdStart.Visible = true;
            }
        }

        private void ToggleRTFCodeVisible()
        {
            mnuEditShowRTF.Checked = !mnuEditShowRTF.Checked;
            txtRTFCode.Visible = mnuEditShowRTF.Checked;
        }

        private void UpdateDataGridTableStyle()
        {

            // Define the coverage results table style
            var tsResults = new DataGridTableStyle();

            // Setting the MappingName of the table style to COVERAGE_RESULTS_DATA_TABLE will cause this style to be used with that table
            tsResults.MappingName = COVERAGE_RESULTS_DATA_TABLE;
            tsResults.AllowSorting = true;
            tsResults.ColumnHeadersVisible = true;
            tsResults.RowHeadersVisible = true;
            tsResults.ReadOnly = true;
            DataGridUtils.AppendColumnToTableStyle(tsResults, COL_NAME_PROTEIN_NAME, COL_NAME_PROTEIN_NAME, 100);
            DataGridUtils.AppendColumnToTableStyle(tsResults, COL_NAME_PROTEIN_COVERAGE, COL_NAME_PROTEIN_COVERAGE, 95);
            if (mProteinDescriptionColVisible)
            {
                DataGridUtils.AppendColumnToTableStyle(tsResults, COL_NAME_PROTEIN_DESCRIPTION, COL_NAME_PROTEIN_DESCRIPTION, 100);
            }
            else
            {
                DataGridUtils.AppendColumnToTableStyle(tsResults, COL_NAME_PROTEIN_DESCRIPTION, COL_NAME_PROTEIN_DESCRIPTION, 0);
            }

            DataGridUtils.AppendColumnToTableStyle(tsResults, COL_NAME_NON_UNIQUE_PEPTIDE_COUNT, COL_NAME_NON_UNIQUE_PEPTIDE_COUNT, 90);
            DataGridUtils.AppendColumnToTableStyle(tsResults, COL_NAME_UNIQUE_PEPTIDE_COUNT, COL_NAME_UNIQUE_PEPTIDE_COUNT, 65);
            DataGridUtils.AppendColumnToTableStyle(tsResults, COL_NAME_PROTEIN_RESIDUE_COUNT, COL_NAME_PROTEIN_RESIDUE_COUNT, 90);
            DataGridUtils.AppendColumnToTableStyle(tsResults, COL_NAME_PROTEIN_SEQUENCE, COL_NAME_PROTEIN_SEQUENCE, 0);

            // Add the DataGridTableStyle to the data grid's TableStyles collection
            var withBlock = dgResults;
            dgResults.TableStyles.Clear();

            if (!dgResults.TableStyles.Contains(tsResults))
            {
                dgResults.TableStyles.Add(tsResults);
            }

            dgResults.ReadOnly = true;
            dgResults.Refresh();
        }

        #region "Command Handlers"

        private void chkSearchAllProteinsForPeptideSequence_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }

        private void cmdAbort_Click(object sender, EventArgs e)
        {
            mProteinCoverageSummarizer.AbortProcessing = true;
        }

        private void cmdExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cmdSelectOutputFolder_Click(object sender, EventArgs e)
        {
            SelectOutputFolder();
        }

        private void cmdPeptideSelectFile_Click(object sender, EventArgs e)
        {
            SelectPeptideInputFile();
        }

        private void cmdProteinSelectFile_Click(object sender, EventArgs e)
        {
            SelectProteinInputFile();
        }

        private void cmdStart_Click(object sender, EventArgs e)
        {
            if (ConfirmInputFilePaths())
            {
                Start();
            }
        }

        private void chkAddSpace_CheckStateChanged(object sender, EventArgs e)
        {
            ShowRichTextStart();
        }

        private void cboCharactersPerLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowRichTextStart();
        }

        private void dgResults_CurrentCellChanged(object sender, EventArgs e)
        {
            ShowRichTextStart(eSequenceDisplayConstants.UseDataGrid);
        }

        #endregion

        #region "Textbox handlers"
        private void txtCoverage_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtCoverage, e, false, false, false, false, false, false, false, false, false, false, true);
        }

        private void txtCustomProteinSequence_Click(object sender, EventArgs e)
        {
            if (txtCustomProteinSequence.TextLength > 0)
                ShowRichTextStart(eSequenceDisplayConstants.UseCustom);
        }

        private void txtCustomProteinSequence_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtCustomProteinSequence, e, false, false, false, true, false, false, false, false, true, true, true);
        }

        private void txtCustomProteinSequence_TextChanged(object sender, EventArgs e)
        {
            ShowRichTextStart(eSequenceDisplayConstants.UseCustom);
        }

        private void txtOutputFolderPath_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandlerCheckControlChars(txtOutputFolderPath, e);
        }

        private void txtPeptideInputFilePath_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandlerCheckControlChars(txtPeptideInputFilePath, e);
        }

        private void txtPeptideInputFilePath_TextChanged(object sender, EventArgs e)
        {
            // Auto-define the output file path
            DefineOutputFolderPath(txtPeptideInputFilePath.Text);
        }

        private void txtProteinInputFilePath_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandlerCheckControlChars(txtProteinInputFilePath, e);
        }

        private void txtProteinInputFilePath_TextChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }

        #endregion

        #region "Menu Handlers"
        private void mnuFileExit_Click(object sender, EventArgs e)
        {
            CloseProgram();
        }

        private void mnuFileSelectInputFile_Click(object sender, EventArgs e)
        {
            SelectProteinInputFile();
        }

        private void mnuFileLoadOptions_Click(object sender, EventArgs e)
        {
            IniFileLoadOptions(false);
        }

        private void mnuPeptideInputFile_Click(object sender, EventArgs e)
        {
            SelectPeptideInputFile();
        }

        private void mnuFileSelectOutputFile_Click(object sender, EventArgs e)
        {
            SelectPeptideInputFile();
        }

        private void mnuEditShowRTF_Click(object sender, EventArgs e)
        {
            ToggleRTFCodeVisible();
        }

        private void mnuHelpAbout_Click(object sender, EventArgs e)
        {
            ShowAboutBox();
        }

        private void mnuEditResetOptions_Click(object sender, EventArgs e)
        {
            ResetToDefaults();
        }

        private void mnuFileSaveDefaultOptions_Click(object sender, EventArgs e)
        {
            IniFileSaveOptions(GetSettingsFilePath(), true);
        }
        #endregion

        private void GUI_Closing(object sender, CancelEventArgs e)
        {
            IniFileSaveOptions(GetSettingsFilePath(), mSaveFullSettingsFileOnExit);
        }

        private void chkSearchAllProteinsSaveDetails_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableControls();
        }

        private void cboPeptideInputFileColumnOrdering_SelectedIndexChanged(object sender, EventArgs e)
        {
            AutoDefineSearchAllProteins();
        }

        private void ProteinCoverageSummarizer_StatusEvent(string message)
        {
            Console.WriteLine(message);
            if (lblProgress.Text.StartsWith(message))
            {
                lblStatus.Text = "";
            }
            else
            {
                lblStatus.Text = message;
            }
        }

        private void ProteinCoverageSummarizer_WarningEvent(string message)
        {
            ConsoleMsgUtils.ShowWarning(message);
            lblStatus.Text = message;
        }

        private void ProteinCoverageSummarizer_ErrorEvent(string message, Exception ex)
        {
            ConsoleMsgUtils.ShowError(message, ex);
            lblStatus.Text = message;
        }

        private void ProteinCoverageSummarizer_ProgressChanged(string taskDescription, float percentComplete)
        {
            lblProgress.Text = taskDescription;
            if (percentComplete > 0)
                lblProgress.Text += Environment.NewLine + percentComplete.ToString("0.0") + "% complete";
            Application.DoEvents();
        }

        private void ProteinCoverageSummarizer_ProgressReset()
        {
            lblProgress.Text = mProteinCoverageSummarizer.ProgressStepDescription;
            Application.DoEvents();
        }
    }
}