Option Strict On

' -------------------------------------------------------------------------------
' Written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA)
' Program started June 14, 2005
'
' E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
' Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/
' -------------------------------------------------------------------------------
'
' Licensed under the 2-Clause BSD License; you may not use this file except
' in compliance with the License.  You may obtain a copy of the License at
' https://opensource.org/licenses/BSD-2-Clause
'
' Copyright 2018 Battelle Memorial Institute

Imports System.ComponentModel
Imports System.Text.RegularExpressions
Imports Ookii.Dialogs
Imports PRISM
Imports ProteinCoverageSummarizer
Imports ProteinFileReader
Imports SharedVBNetRoutines

''' <summary>
''' This program uses clsProteinCoverageSummarizer to read in a file with protein sequences along with
''' an accompanying file with peptide sequences and compute the percent coverage of each of the proteins
''' </summary>
Public Class GUI
    Inherits Form

#Region " Windows Form Designer generated code "

    Public Sub New()
        MyBase.New()

        'This call is required by the Windows Form Designer.
        InitializeComponent()

        'Add any initialization after the InitializeComponent() call
        InitializeControls()

    End Sub

    'Form overrides dispose to clean up the component list.
    Protected Overloads Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            If Not (components Is Nothing) Then
                components.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer
#Region "Designer generated code "

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    Friend WithEvents MainMenuControl As System.Windows.Forms.MainMenu
    Friend WithEvents mnuFile As System.Windows.Forms.MenuItem
    Friend WithEvents mnuFileSelectInputFile As System.Windows.Forms.MenuItem
    Friend WithEvents mnuFileSelectOutputFile As System.Windows.Forms.MenuItem
    Friend WithEvents mnuFileSep1 As System.Windows.Forms.MenuItem
    Friend WithEvents mnuFileSaveDefaultOptions As System.Windows.Forms.MenuItem
    Friend WithEvents mnuFileSep2 As System.Windows.Forms.MenuItem
    Friend WithEvents mnuFileExit As System.Windows.Forms.MenuItem
    Friend WithEvents mnuEdit As System.Windows.Forms.MenuItem
    Friend WithEvents mnuEditResetOptions As System.Windows.Forms.MenuItem
    Friend WithEvents mnuHelp As System.Windows.Forms.MenuItem
    Friend WithEvents mnuHelpAbout As System.Windows.Forms.MenuItem
    Friend WithEvents fraProteinInputFilePath As System.Windows.Forms.GroupBox
    Friend WithEvents cmdProteinSelectFile As System.Windows.Forms.Button
    Friend WithEvents fraPeptideInputFilePath As System.Windows.Forms.GroupBox
    Friend WithEvents cmdPeptideSelectFile As System.Windows.Forms.Button
    Friend WithEvents txtPeptideInputFilePath As System.Windows.Forms.TextBox
    Friend WithEvents fraProcessingOptions As System.Windows.Forms.GroupBox
    Friend WithEvents fraMassCalculationOptions As System.Windows.Forms.GroupBox
    Friend WithEvents fraDigestionOptions As System.Windows.Forms.GroupBox
    Friend WithEvents txtMinimumSLiCScore As System.Windows.Forms.TextBox
    Friend WithEvents fraPeakMatchingOptions As System.Windows.Forms.GroupBox
    Friend WithEvents fraSqlServerOptions As System.Windows.Forms.GroupBox
    Friend WithEvents fraUniquenessBinningOptions As System.Windows.Forms.GroupBox
    Friend WithEvents cmdPastePMThresholdsList As System.Windows.Forms.Button
    Friend WithEvents cboPMPredefinedThresholds As System.Windows.Forms.ComboBox
    Friend WithEvents cmdPMThresholdsAutoPopulate As System.Windows.Forms.Button
    Friend WithEvents cmdClearPMThresholdsList As System.Windows.Forms.Button
    Friend WithEvents cboMassTolType As System.Windows.Forms.ComboBox
    Friend WithEvents tbsOptions As System.Windows.Forms.TabControl
    Friend WithEvents TabPageFileFormatOptions As System.Windows.Forms.TabPage
    Friend WithEvents TabPagePeakMatchingThresholds As System.Windows.Forms.TabPage
    Friend WithEvents fraProteinDelimitedFileOptions As System.Windows.Forms.GroupBox
    Friend WithEvents cboProteinInputFileColumnOrdering As System.Windows.Forms.ComboBox
    Friend WithEvents lblProteinInputFileColumnOrdering As System.Windows.Forms.Label
    Friend WithEvents lblProteinInputFileColumnDelimiter As System.Windows.Forms.Label
    Friend WithEvents cboProteinInputFileColumnDelimiter As System.Windows.Forms.ComboBox
    Friend WithEvents fraPeptideDelimitedFileOptions As System.Windows.Forms.GroupBox
    Friend WithEvents txtPeptideInputFileColumnDelimiter As System.Windows.Forms.TextBox
    Friend WithEvents lblPeptideInputFileColumnDelimiter As System.Windows.Forms.Label
    Friend WithEvents cboPeptideInputFileColumnDelimiter As System.Windows.Forms.ComboBox
    Friend WithEvents fraOptions As System.Windows.Forms.GroupBox
    Friend WithEvents chkSearchAllProteinsForPeptideSequence As System.Windows.Forms.CheckBox
    Friend WithEvents chkOutputProteinSequence As System.Windows.Forms.CheckBox
    Friend WithEvents chkTrackPeptideCounts As System.Windows.Forms.CheckBox
    Friend WithEvents chkRemoveSymbolCharacters As System.Windows.Forms.CheckBox
    Friend WithEvents cmdStart As System.Windows.Forms.Button
    Friend WithEvents mnuPeptideInputFile As System.Windows.Forms.MenuItem
    Friend WithEvents dgResults As System.Windows.Forms.DataGrid
    Friend WithEvents lblProgress As System.Windows.Forms.Label
    Friend WithEvents chkAddSpace As System.Windows.Forms.CheckBox
    Friend WithEvents cboCharactersPerLine As System.Windows.Forms.ComboBox
    Friend WithEvents rtfRichTextBox As System.Windows.Forms.RichTextBox
    Friend WithEvents chkPeptideFileSkipFirstLine As System.Windows.Forms.CheckBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents chkProteinFileSkipFirstLine As System.Windows.Forms.CheckBox
    Friend WithEvents cmdAbort As System.Windows.Forms.Button
    Friend WithEvents cmdExit As System.Windows.Forms.Button
    Friend WithEvents cboPeptideInputFileColumnOrdering As System.Windows.Forms.ComboBox
    Friend WithEvents txtProteinInputFileColumnDelimiter As System.Windows.Forms.TextBox
    Friend WithEvents mnuFileLoadOptions As System.Windows.Forms.MenuItem
    Friend WithEvents txtCustomProteinSequence As System.Windows.Forms.TextBox
    Friend WithEvents lblCustomProteinSequence As System.Windows.Forms.Label
    Friend WithEvents txtRTFCode As System.Windows.Forms.TextBox
    Friend WithEvents mnuEditShowRTF As System.Windows.Forms.MenuItem
    Friend WithEvents txtCoverage As System.Windows.Forms.TextBox
    Friend WithEvents fraOutputFolderPath As System.Windows.Forms.GroupBox
    Friend WithEvents txtOutputFolderPath As System.Windows.Forms.TextBox
    Friend WithEvents cmdSelectOutputFolder As System.Windows.Forms.Button
    Friend WithEvents chkSearchAllProteinsSkipCoverageComputationSteps As System.Windows.Forms.CheckBox
    Friend WithEvents lblInputFileNotes As System.Windows.Forms.Label
    Friend WithEvents chkSaveProteinToPeptideMappingFile As System.Windows.Forms.CheckBox
    Friend WithEvents chkMatchPeptidePrefixAndSuffixToProtein As System.Windows.Forms.CheckBox
    Friend WithEvents txtProteinInputFilePath As System.Windows.Forms.TextBox
    Friend WithEvents lblStatus As Label
    Friend WithEvents chkIgnoreILDifferences As System.Windows.Forms.CheckBox
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.fraProteinInputFilePath = New System.Windows.Forms.GroupBox()
        Me.cmdProteinSelectFile = New System.Windows.Forms.Button()
        Me.txtProteinInputFilePath = New System.Windows.Forms.TextBox()
        Me.MainMenuControl = New System.Windows.Forms.MainMenu(Me.components)
        Me.mnuFile = New System.Windows.Forms.MenuItem()
        Me.mnuFileSelectInputFile = New System.Windows.Forms.MenuItem()
        Me.mnuPeptideInputFile = New System.Windows.Forms.MenuItem()
        Me.mnuFileSelectOutputFile = New System.Windows.Forms.MenuItem()
        Me.mnuFileSep1 = New System.Windows.Forms.MenuItem()
        Me.mnuFileLoadOptions = New System.Windows.Forms.MenuItem()
        Me.mnuFileSaveDefaultOptions = New System.Windows.Forms.MenuItem()
        Me.mnuFileSep2 = New System.Windows.Forms.MenuItem()
        Me.mnuFileExit = New System.Windows.Forms.MenuItem()
        Me.mnuEdit = New System.Windows.Forms.MenuItem()
        Me.mnuEditShowRTF = New System.Windows.Forms.MenuItem()
        Me.mnuEditResetOptions = New System.Windows.Forms.MenuItem()
        Me.mnuHelp = New System.Windows.Forms.MenuItem()
        Me.mnuHelpAbout = New System.Windows.Forms.MenuItem()
        Me.fraPeptideInputFilePath = New System.Windows.Forms.GroupBox()
        Me.cmdPeptideSelectFile = New System.Windows.Forms.Button()
        Me.txtPeptideInputFilePath = New System.Windows.Forms.TextBox()
        Me.fraProcessingOptions = New System.Windows.Forms.GroupBox()
        Me.fraMassCalculationOptions = New System.Windows.Forms.GroupBox()
        Me.fraDigestionOptions = New System.Windows.Forms.GroupBox()
        Me.txtMinimumSLiCScore = New System.Windows.Forms.TextBox()
        Me.fraPeakMatchingOptions = New System.Windows.Forms.GroupBox()
        Me.fraSqlServerOptions = New System.Windows.Forms.GroupBox()
        Me.fraUniquenessBinningOptions = New System.Windows.Forms.GroupBox()
        Me.cmdPastePMThresholdsList = New System.Windows.Forms.Button()
        Me.cboPMPredefinedThresholds = New System.Windows.Forms.ComboBox()
        Me.cmdPMThresholdsAutoPopulate = New System.Windows.Forms.Button()
        Me.cmdClearPMThresholdsList = New System.Windows.Forms.Button()
        Me.cboMassTolType = New System.Windows.Forms.ComboBox()
        Me.tbsOptions = New System.Windows.Forms.TabControl()
        Me.TabPageFileFormatOptions = New System.Windows.Forms.TabPage()
        Me.cmdExit = New System.Windows.Forms.Button()
        Me.cmdStart = New System.Windows.Forms.Button()
        Me.cmdAbort = New System.Windows.Forms.Button()
        Me.lblProgress = New System.Windows.Forms.Label()
        Me.fraOptions = New System.Windows.Forms.GroupBox()
        Me.chkIgnoreILDifferences = New System.Windows.Forms.CheckBox()
        Me.chkMatchPeptidePrefixAndSuffixToProtein = New System.Windows.Forms.CheckBox()
        Me.chkSearchAllProteinsSkipCoverageComputationSteps = New System.Windows.Forms.CheckBox()
        Me.chkSaveProteinToPeptideMappingFile = New System.Windows.Forms.CheckBox()
        Me.chkSearchAllProteinsForPeptideSequence = New System.Windows.Forms.CheckBox()
        Me.chkOutputProteinSequence = New System.Windows.Forms.CheckBox()
        Me.chkTrackPeptideCounts = New System.Windows.Forms.CheckBox()
        Me.chkRemoveSymbolCharacters = New System.Windows.Forms.CheckBox()
        Me.fraPeptideDelimitedFileOptions = New System.Windows.Forms.GroupBox()
        Me.cboPeptideInputFileColumnOrdering = New System.Windows.Forms.ComboBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.chkPeptideFileSkipFirstLine = New System.Windows.Forms.CheckBox()
        Me.txtPeptideInputFileColumnDelimiter = New System.Windows.Forms.TextBox()
        Me.lblPeptideInputFileColumnDelimiter = New System.Windows.Forms.Label()
        Me.cboPeptideInputFileColumnDelimiter = New System.Windows.Forms.ComboBox()
        Me.lblInputFileNotes = New System.Windows.Forms.Label()
        Me.fraProteinDelimitedFileOptions = New System.Windows.Forms.GroupBox()
        Me.chkProteinFileSkipFirstLine = New System.Windows.Forms.CheckBox()
        Me.cboProteinInputFileColumnOrdering = New System.Windows.Forms.ComboBox()
        Me.lblProteinInputFileColumnOrdering = New System.Windows.Forms.Label()
        Me.txtProteinInputFileColumnDelimiter = New System.Windows.Forms.TextBox()
        Me.lblProteinInputFileColumnDelimiter = New System.Windows.Forms.Label()
        Me.cboProteinInputFileColumnDelimiter = New System.Windows.Forms.ComboBox()
        Me.TabPagePeakMatchingThresholds = New System.Windows.Forms.TabPage()
        Me.txtCoverage = New System.Windows.Forms.TextBox()
        Me.txtRTFCode = New System.Windows.Forms.TextBox()
        Me.txtCustomProteinSequence = New System.Windows.Forms.TextBox()
        Me.lblCustomProteinSequence = New System.Windows.Forms.Label()
        Me.chkAddSpace = New System.Windows.Forms.CheckBox()
        Me.cboCharactersPerLine = New System.Windows.Forms.ComboBox()
        Me.rtfRichTextBox = New System.Windows.Forms.RichTextBox()
        Me.dgResults = New System.Windows.Forms.DataGrid()
        Me.fraOutputFolderPath = New System.Windows.Forms.GroupBox()
        Me.cmdSelectOutputFolder = New System.Windows.Forms.Button()
        Me.txtOutputFolderPath = New System.Windows.Forms.TextBox()
        Me.lblStatus = New System.Windows.Forms.Label()
        Me.fraProteinInputFilePath.SuspendLayout()
        Me.fraPeptideInputFilePath.SuspendLayout()
        Me.tbsOptions.SuspendLayout()
        Me.TabPageFileFormatOptions.SuspendLayout()
        Me.fraOptions.SuspendLayout()
        Me.fraPeptideDelimitedFileOptions.SuspendLayout()
        Me.fraProteinDelimitedFileOptions.SuspendLayout()
        Me.TabPagePeakMatchingThresholds.SuspendLayout()
        CType(Me.dgResults, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.fraOutputFolderPath.SuspendLayout()
        Me.SuspendLayout()
        '
        'fraProteinInputFilePath
        '
        Me.fraProteinInputFilePath.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.fraProteinInputFilePath.Controls.Add(Me.cmdProteinSelectFile)
        Me.fraProteinInputFilePath.Controls.Add(Me.txtProteinInputFilePath)
        Me.fraProteinInputFilePath.Location = New System.Drawing.Point(10, 18)
        Me.fraProteinInputFilePath.Name = "fraProteinInputFilePath"
        Me.fraProteinInputFilePath.Size = New System.Drawing.Size(885, 56)
        Me.fraProteinInputFilePath.TabIndex = 0
        Me.fraProteinInputFilePath.TabStop = False
        Me.fraProteinInputFilePath.Text = "Protein Input File Path (Fasta or Tab-delimited)"
        '
        'cmdProteinSelectFile
        '
        Me.cmdProteinSelectFile.Location = New System.Drawing.Point(10, 18)
        Me.cmdProteinSelectFile.Name = "cmdProteinSelectFile"
        Me.cmdProteinSelectFile.Size = New System.Drawing.Size(96, 28)
        Me.cmdProteinSelectFile.TabIndex = 0
        Me.cmdProteinSelectFile.Text = "Select file"
        '
        'txtProteinInputFilePath
        '
        Me.txtProteinInputFilePath.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtProteinInputFilePath.Location = New System.Drawing.Point(125, 21)
        Me.txtProteinInputFilePath.Name = "txtProteinInputFilePath"
        Me.txtProteinInputFilePath.Size = New System.Drawing.Size(741, 22)
        Me.txtProteinInputFilePath.TabIndex = 1
        '
        'MainMenuControl
        '
        Me.MainMenuControl.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.mnuFile, Me.mnuEdit, Me.mnuHelp})
        '
        'mnuFile
        '
        Me.mnuFile.Index = 0
        Me.mnuFile.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.mnuFileSelectInputFile, Me.mnuPeptideInputFile, Me.mnuFileSelectOutputFile, Me.mnuFileSep1, Me.mnuFileLoadOptions, Me.mnuFileSaveDefaultOptions, Me.mnuFileSep2, Me.mnuFileExit})
        Me.mnuFile.Text = "&File"
        '
        'mnuFileSelectInputFile
        '
        Me.mnuFileSelectInputFile.Index = 0
        Me.mnuFileSelectInputFile.Text = "Select Protein &Input File..."
        '
        'mnuPeptideInputFile
        '
        Me.mnuPeptideInputFile.Index = 1
        Me.mnuPeptideInputFile.Text = "Select Peptide I&nput File..."
        '
        'mnuFileSelectOutputFile
        '
        Me.mnuFileSelectOutputFile.Index = 2
        Me.mnuFileSelectOutputFile.Text = "Select &Output File..."
        '
        'mnuFileSep1
        '
        Me.mnuFileSep1.Index = 3
        Me.mnuFileSep1.Text = "-"
        '
        'mnuFileLoadOptions
        '
        Me.mnuFileLoadOptions.Index = 4
        Me.mnuFileLoadOptions.Text = "Load Options ..."
        '
        'mnuFileSaveDefaultOptions
        '
        Me.mnuFileSaveDefaultOptions.Index = 5
        Me.mnuFileSaveDefaultOptions.Text = "Save &Default Options"
        '
        'mnuFileSep2
        '
        Me.mnuFileSep2.Index = 6
        Me.mnuFileSep2.Text = "-"
        '
        'mnuFileExit
        '
        Me.mnuFileExit.Index = 7
        Me.mnuFileExit.Text = "E&xit"
        '
        'mnuEdit
        '
        Me.mnuEdit.Index = 1
        Me.mnuEdit.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.mnuEditShowRTF, Me.mnuEditResetOptions})
        Me.mnuEdit.Text = "&Edit"
        '
        'mnuEditShowRTF
        '
        Me.mnuEditShowRTF.Index = 0
        Me.mnuEditShowRTF.Text = "Show RTF Code"
        '
        'mnuEditResetOptions
        '
        Me.mnuEditResetOptions.Index = 1
        Me.mnuEditResetOptions.Text = "&Reset options to Defaults"
        '
        'mnuHelp
        '
        Me.mnuHelp.Index = 2
        Me.mnuHelp.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.mnuHelpAbout})
        Me.mnuHelp.Text = "&Help"
        '
        'mnuHelpAbout
        '
        Me.mnuHelpAbout.Index = 0
        Me.mnuHelpAbout.Text = "&About"
        '
        'fraPeptideInputFilePath
        '
        Me.fraPeptideInputFilePath.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.fraPeptideInputFilePath.Controls.Add(Me.cmdPeptideSelectFile)
        Me.fraPeptideInputFilePath.Controls.Add(Me.txtPeptideInputFilePath)
        Me.fraPeptideInputFilePath.Location = New System.Drawing.Point(10, 83)
        Me.fraPeptideInputFilePath.Name = "fraPeptideInputFilePath"
        Me.fraPeptideInputFilePath.Size = New System.Drawing.Size(885, 55)
        Me.fraPeptideInputFilePath.TabIndex = 1
        Me.fraPeptideInputFilePath.TabStop = False
        Me.fraPeptideInputFilePath.Text = "Peptide Input File Path (Tab-delimited)"
        '
        'cmdPeptideSelectFile
        '
        Me.cmdPeptideSelectFile.Location = New System.Drawing.Point(10, 18)
        Me.cmdPeptideSelectFile.Name = "cmdPeptideSelectFile"
        Me.cmdPeptideSelectFile.Size = New System.Drawing.Size(96, 28)
        Me.cmdPeptideSelectFile.TabIndex = 0
        Me.cmdPeptideSelectFile.Text = "Select file"
        '
        'txtPeptideInputFilePath
        '
        Me.txtPeptideInputFilePath.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtPeptideInputFilePath.Location = New System.Drawing.Point(125, 21)
        Me.txtPeptideInputFilePath.Name = "txtPeptideInputFilePath"
        Me.txtPeptideInputFilePath.Size = New System.Drawing.Size(741, 22)
        Me.txtPeptideInputFilePath.TabIndex = 1
        '
        'fraProcessingOptions
        '
        Me.fraProcessingOptions.Location = New System.Drawing.Point(8, 8)
        Me.fraProcessingOptions.Name = "fraProcessingOptions"
        Me.fraProcessingOptions.Size = New System.Drawing.Size(360, 152)
        Me.fraProcessingOptions.TabIndex = 0
        Me.fraProcessingOptions.TabStop = False
        Me.fraProcessingOptions.Text = "Processing Options"
        '
        'fraMassCalculationOptions
        '
        Me.fraMassCalculationOptions.Location = New System.Drawing.Point(376, 80)
        Me.fraMassCalculationOptions.Name = "fraMassCalculationOptions"
        Me.fraMassCalculationOptions.Size = New System.Drawing.Size(248, 80)
        Me.fraMassCalculationOptions.TabIndex = 1
        Me.fraMassCalculationOptions.TabStop = False
        Me.fraMassCalculationOptions.Text = "Mass Calculation Options"
        '
        'fraDigestionOptions
        '
        Me.fraDigestionOptions.Location = New System.Drawing.Point(8, 168)
        Me.fraDigestionOptions.Name = "fraDigestionOptions"
        Me.fraDigestionOptions.Size = New System.Drawing.Size(616, 112)
        Me.fraDigestionOptions.TabIndex = 2
        Me.fraDigestionOptions.TabStop = False
        Me.fraDigestionOptions.Text = "Digestion Options"
        '
        'txtMinimumSLiCScore
        '
        Me.txtMinimumSLiCScore.Location = New System.Drawing.Point(144, 104)
        Me.txtMinimumSLiCScore.Name = "txtMinimumSLiCScore"
        Me.txtMinimumSLiCScore.Size = New System.Drawing.Size(40, 22)
        Me.txtMinimumSLiCScore.TabIndex = 5
        '
        'fraPeakMatchingOptions
        '
        Me.fraPeakMatchingOptions.Location = New System.Drawing.Point(232, 48)
        Me.fraPeakMatchingOptions.Name = "fraPeakMatchingOptions"
        Me.fraPeakMatchingOptions.Size = New System.Drawing.Size(392, 136)
        Me.fraPeakMatchingOptions.TabIndex = 2
        Me.fraPeakMatchingOptions.TabStop = False
        '
        'fraSqlServerOptions
        '
        Me.fraSqlServerOptions.Location = New System.Drawing.Point(576, 192)
        Me.fraSqlServerOptions.Name = "fraSqlServerOptions"
        Me.fraSqlServerOptions.Size = New System.Drawing.Size(376, 112)
        Me.fraSqlServerOptions.TabIndex = 4
        Me.fraSqlServerOptions.TabStop = False
        Me.fraSqlServerOptions.Visible = False
        '
        'fraUniquenessBinningOptions
        '
        Me.fraUniquenessBinningOptions.Location = New System.Drawing.Point(8, 144)
        Me.fraUniquenessBinningOptions.Name = "fraUniquenessBinningOptions"
        Me.fraUniquenessBinningOptions.Size = New System.Drawing.Size(208, 136)
        Me.fraUniquenessBinningOptions.TabIndex = 3
        Me.fraUniquenessBinningOptions.TabStop = False
        '
        'cmdPastePMThresholdsList
        '
        Me.cmdPastePMThresholdsList.Location = New System.Drawing.Point(456, 96)
        Me.cmdPastePMThresholdsList.Name = "cmdPastePMThresholdsList"
        Me.cmdPastePMThresholdsList.Size = New System.Drawing.Size(104, 24)
        Me.cmdPastePMThresholdsList.TabIndex = 6
        Me.cmdPastePMThresholdsList.Text = "Paste Values"
        '
        'cboPMPredefinedThresholds
        '
        Me.cboPMPredefinedThresholds.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboPMPredefinedThresholds.Location = New System.Drawing.Point(336, 256)
        Me.cboPMPredefinedThresholds.Name = "cboPMPredefinedThresholds"
        Me.cboPMPredefinedThresholds.Size = New System.Drawing.Size(264, 25)
        Me.cboPMPredefinedThresholds.TabIndex = 5
        '
        'cmdPMThresholdsAutoPopulate
        '
        Me.cmdPMThresholdsAutoPopulate.Location = New System.Drawing.Point(336, 224)
        Me.cmdPMThresholdsAutoPopulate.Name = "cmdPMThresholdsAutoPopulate"
        Me.cmdPMThresholdsAutoPopulate.Size = New System.Drawing.Size(104, 24)
        Me.cmdPMThresholdsAutoPopulate.TabIndex = 4
        Me.cmdPMThresholdsAutoPopulate.Text = "Auto-Populate"
        '
        'cmdClearPMThresholdsList
        '
        Me.cmdClearPMThresholdsList.Location = New System.Drawing.Point(456, 128)
        Me.cmdClearPMThresholdsList.Name = "cmdClearPMThresholdsList"
        Me.cmdClearPMThresholdsList.Size = New System.Drawing.Size(104, 24)
        Me.cmdClearPMThresholdsList.TabIndex = 7
        Me.cmdClearPMThresholdsList.Text = "Clear List"
        '
        'cboMassTolType
        '
        Me.cboMassTolType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboMassTolType.Location = New System.Drawing.Point(144, 224)
        Me.cboMassTolType.Name = "cboMassTolType"
        Me.cboMassTolType.Size = New System.Drawing.Size(136, 25)
        Me.cboMassTolType.TabIndex = 2
        '
        'tbsOptions
        '
        Me.tbsOptions.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.tbsOptions.Controls.Add(Me.TabPageFileFormatOptions)
        Me.tbsOptions.Controls.Add(Me.TabPagePeakMatchingThresholds)
        Me.tbsOptions.Location = New System.Drawing.Point(10, 222)
        Me.tbsOptions.Name = "tbsOptions"
        Me.tbsOptions.SelectedIndex = 0
        Me.tbsOptions.Size = New System.Drawing.Size(885, 369)
        Me.tbsOptions.TabIndex = 3
        '
        'TabPageFileFormatOptions
        '
        Me.TabPageFileFormatOptions.Controls.Add(Me.lblStatus)
        Me.TabPageFileFormatOptions.Controls.Add(Me.cmdExit)
        Me.TabPageFileFormatOptions.Controls.Add(Me.cmdStart)
        Me.TabPageFileFormatOptions.Controls.Add(Me.cmdAbort)
        Me.TabPageFileFormatOptions.Controls.Add(Me.lblProgress)
        Me.TabPageFileFormatOptions.Controls.Add(Me.fraOptions)
        Me.TabPageFileFormatOptions.Controls.Add(Me.fraPeptideDelimitedFileOptions)
        Me.TabPageFileFormatOptions.Controls.Add(Me.fraProteinDelimitedFileOptions)
        Me.TabPageFileFormatOptions.Location = New System.Drawing.Point(4, 25)
        Me.TabPageFileFormatOptions.Name = "TabPageFileFormatOptions"
        Me.TabPageFileFormatOptions.Size = New System.Drawing.Size(877, 340)
        Me.TabPageFileFormatOptions.TabIndex = 2
        Me.TabPageFileFormatOptions.Text = "File Format Options"
        '
        'cmdExit
        '
        Me.cmdExit.Location = New System.Drawing.Point(662, 204)
        Me.cmdExit.Name = "cmdExit"
        Me.cmdExit.Size = New System.Drawing.Size(116, 37)
        Me.cmdExit.TabIndex = 5
        Me.cmdExit.Text = "E&xit"
        '
        'cmdStart
        '
        Me.cmdStart.Location = New System.Drawing.Point(662, 148)
        Me.cmdStart.Name = "cmdStart"
        Me.cmdStart.Size = New System.Drawing.Size(116, 37)
        Me.cmdStart.TabIndex = 4
        Me.cmdStart.Text = "&Start"
        '
        'cmdAbort
        '
        Me.cmdAbort.Location = New System.Drawing.Point(662, 148)
        Me.cmdAbort.Name = "cmdAbort"
        Me.cmdAbort.Size = New System.Drawing.Size(116, 37)
        Me.cmdAbort.TabIndex = 4
        Me.cmdAbort.Text = "Abort"
        '
        'lblProgress
        '
        Me.lblProgress.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblProgress.Location = New System.Drawing.Point(657, 15)
        Me.lblProgress.Name = "lblProgress"
        Me.lblProgress.Size = New System.Drawing.Size(192, 51)
        Me.lblProgress.TabIndex = 3
        Me.lblProgress.Text = "Progress ..."
        '
        'fraOptions
        '
        Me.fraOptions.Controls.Add(Me.chkIgnoreILDifferences)
        Me.fraOptions.Controls.Add(Me.chkMatchPeptidePrefixAndSuffixToProtein)
        Me.fraOptions.Controls.Add(Me.chkSearchAllProteinsSkipCoverageComputationSteps)
        Me.fraOptions.Controls.Add(Me.chkSaveProteinToPeptideMappingFile)
        Me.fraOptions.Controls.Add(Me.chkSearchAllProteinsForPeptideSequence)
        Me.fraOptions.Controls.Add(Me.chkOutputProteinSequence)
        Me.fraOptions.Controls.Add(Me.chkTrackPeptideCounts)
        Me.fraOptions.Controls.Add(Me.chkRemoveSymbolCharacters)
        Me.fraOptions.Location = New System.Drawing.Point(10, 252)
        Me.fraOptions.Name = "fraOptions"
        Me.fraOptions.Size = New System.Drawing.Size(777, 163)
        Me.fraOptions.TabIndex = 2
        Me.fraOptions.TabStop = False
        Me.fraOptions.Text = "Options"
        '
        'chkIgnoreILDifferences
        '
        Me.chkIgnoreILDifferences.Location = New System.Drawing.Point(480, 111)
        Me.chkIgnoreILDifferences.Name = "chkIgnoreILDifferences"
        Me.chkIgnoreILDifferences.Size = New System.Drawing.Size(269, 18)
        Me.chkIgnoreILDifferences.TabIndex = 7
        Me.chkIgnoreILDifferences.Text = "Ignore I/L Differences"
        '
        'chkMatchPeptidePrefixAndSuffixToProtein
        '
        Me.chkMatchPeptidePrefixAndSuffixToProtein.Location = New System.Drawing.Point(19, 138)
        Me.chkMatchPeptidePrefixAndSuffixToProtein.Name = "chkMatchPeptidePrefixAndSuffixToProtein"
        Me.chkMatchPeptidePrefixAndSuffixToProtein.Size = New System.Drawing.Size(394, 19)
        Me.chkMatchPeptidePrefixAndSuffixToProtein.TabIndex = 6
        Me.chkMatchPeptidePrefixAndSuffixToProtein.Text = "Match peptide prefix and suffix letters to protein sequence"
        '
        'chkSearchAllProteinsSkipCoverageComputationSteps
        '
        Me.chkSearchAllProteinsSkipCoverageComputationSteps.Location = New System.Drawing.Point(480, 65)
        Me.chkSearchAllProteinsSkipCoverageComputationSteps.Name = "chkSearchAllProteinsSkipCoverageComputationSteps"
        Me.chkSearchAllProteinsSkipCoverageComputationSteps.Size = New System.Drawing.Size(269, 18)
        Me.chkSearchAllProteinsSkipCoverageComputationSteps.TabIndex = 3
        Me.chkSearchAllProteinsSkipCoverageComputationSteps.Text = "Skip coverage computation (faster)"
        '
        'chkSaveProteinToPeptideMappingFile
        '
        Me.chkSaveProteinToPeptideMappingFile.Location = New System.Drawing.Point(480, 46)
        Me.chkSaveProteinToPeptideMappingFile.Name = "chkSaveProteinToPeptideMappingFile"
        Me.chkSaveProteinToPeptideMappingFile.Size = New System.Drawing.Size(269, 19)
        Me.chkSaveProteinToPeptideMappingFile.TabIndex = 2
        Me.chkSaveProteinToPeptideMappingFile.Text = "Save protein to peptide mapping details"
        '
        'chkSearchAllProteinsForPeptideSequence
        '
        Me.chkSearchAllProteinsForPeptideSequence.Location = New System.Drawing.Point(19, 46)
        Me.chkSearchAllProteinsForPeptideSequence.Name = "chkSearchAllProteinsForPeptideSequence"
        Me.chkSearchAllProteinsForPeptideSequence.Size = New System.Drawing.Size(288, 28)
        Me.chkSearchAllProteinsForPeptideSequence.TabIndex = 1
        Me.chkSearchAllProteinsForPeptideSequence.Text = "Search All Proteins For Peptide Sequence"
        '
        'chkOutputProteinSequence
        '
        Me.chkOutputProteinSequence.Location = New System.Drawing.Point(19, 18)
        Me.chkOutputProteinSequence.Name = "chkOutputProteinSequence"
        Me.chkOutputProteinSequence.Size = New System.Drawing.Size(211, 28)
        Me.chkOutputProteinSequence.TabIndex = 0
        Me.chkOutputProteinSequence.Text = "Output Protein Sequence"
        '
        'chkTrackPeptideCounts
        '
        Me.chkTrackPeptideCounts.Location = New System.Drawing.Point(19, 83)
        Me.chkTrackPeptideCounts.Name = "chkTrackPeptideCounts"
        Me.chkTrackPeptideCounts.Size = New System.Drawing.Size(317, 19)
        Me.chkTrackPeptideCounts.TabIndex = 4
        Me.chkTrackPeptideCounts.Text = "Track Unique And Non-Unique Peptide Counts"
        '
        'chkRemoveSymbolCharacters
        '
        Me.chkRemoveSymbolCharacters.Location = New System.Drawing.Point(19, 111)
        Me.chkRemoveSymbolCharacters.Name = "chkRemoveSymbolCharacters"
        Me.chkRemoveSymbolCharacters.Size = New System.Drawing.Size(442, 18)
        Me.chkRemoveSymbolCharacters.TabIndex = 5
        Me.chkRemoveSymbolCharacters.Text = "Remove non-letter characters from protein and peptide sequences"
        '
        'fraPeptideDelimitedFileOptions
        '
        Me.fraPeptideDelimitedFileOptions.Controls.Add(Me.cboPeptideInputFileColumnOrdering)
        Me.fraPeptideDelimitedFileOptions.Controls.Add(Me.Label1)
        Me.fraPeptideDelimitedFileOptions.Controls.Add(Me.chkPeptideFileSkipFirstLine)
        Me.fraPeptideDelimitedFileOptions.Controls.Add(Me.txtPeptideInputFileColumnDelimiter)
        Me.fraPeptideDelimitedFileOptions.Controls.Add(Me.lblPeptideInputFileColumnDelimiter)
        Me.fraPeptideDelimitedFileOptions.Controls.Add(Me.cboPeptideInputFileColumnDelimiter)
        Me.fraPeptideDelimitedFileOptions.Controls.Add(Me.lblInputFileNotes)
        Me.fraPeptideDelimitedFileOptions.Location = New System.Drawing.Point(10, 129)
        Me.fraPeptideDelimitedFileOptions.Name = "fraPeptideDelimitedFileOptions"
        Me.fraPeptideDelimitedFileOptions.Size = New System.Drawing.Size(643, 120)
        Me.fraPeptideDelimitedFileOptions.TabIndex = 1
        Me.fraPeptideDelimitedFileOptions.TabStop = False
        Me.fraPeptideDelimitedFileOptions.Text = "Peptide Delimited Input File Options"
        '
        'cboPeptideInputFileColumnOrdering
        '
        Me.cboPeptideInputFileColumnOrdering.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboPeptideInputFileColumnOrdering.DropDownWidth = 70
        Me.cboPeptideInputFileColumnOrdering.Location = New System.Drawing.Point(106, 28)
        Me.cboPeptideInputFileColumnOrdering.Name = "cboPeptideInputFileColumnOrdering"
        Me.cboPeptideInputFileColumnOrdering.Size = New System.Drawing.Size(316, 24)
        Me.cboPeptideInputFileColumnOrdering.TabIndex = 1
        '
        'Label1
        '
        Me.Label1.Location = New System.Drawing.Point(10, 28)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(96, 18)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Column Order"
        '
        'chkPeptideFileSkipFirstLine
        '
        Me.chkPeptideFileSkipFirstLine.Location = New System.Drawing.Point(317, 65)
        Me.chkPeptideFileSkipFirstLine.Name = "chkPeptideFileSkipFirstLine"
        Me.chkPeptideFileSkipFirstLine.Size = New System.Drawing.Size(288, 27)
        Me.chkPeptideFileSkipFirstLine.TabIndex = 5
        Me.chkPeptideFileSkipFirstLine.Text = "Skip first line in peptide input file"
        '
        'txtPeptideInputFileColumnDelimiter
        '
        Me.txtPeptideInputFileColumnDelimiter.Location = New System.Drawing.Point(230, 65)
        Me.txtPeptideInputFileColumnDelimiter.MaxLength = 1
        Me.txtPeptideInputFileColumnDelimiter.Name = "txtPeptideInputFileColumnDelimiter"
        Me.txtPeptideInputFileColumnDelimiter.Size = New System.Drawing.Size(39, 22)
        Me.txtPeptideInputFileColumnDelimiter.TabIndex = 4
        Me.txtPeptideInputFileColumnDelimiter.Text = ";"
        '
        'lblPeptideInputFileColumnDelimiter
        '
        Me.lblPeptideInputFileColumnDelimiter.Location = New System.Drawing.Point(10, 65)
        Me.lblPeptideInputFileColumnDelimiter.Name = "lblPeptideInputFileColumnDelimiter"
        Me.lblPeptideInputFileColumnDelimiter.Size = New System.Drawing.Size(115, 18)
        Me.lblPeptideInputFileColumnDelimiter.TabIndex = 2
        Me.lblPeptideInputFileColumnDelimiter.Text = "Column Delimiter"
        '
        'cboPeptideInputFileColumnDelimiter
        '
        Me.cboPeptideInputFileColumnDelimiter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboPeptideInputFileColumnDelimiter.DropDownWidth = 70
        Me.cboPeptideInputFileColumnDelimiter.Location = New System.Drawing.Point(134, 65)
        Me.cboPeptideInputFileColumnDelimiter.Name = "cboPeptideInputFileColumnDelimiter"
        Me.cboPeptideInputFileColumnDelimiter.Size = New System.Drawing.Size(84, 24)
        Me.cboPeptideInputFileColumnDelimiter.TabIndex = 3
        '
        'lblInputFileNotes
        '
        Me.lblInputFileNotes.Location = New System.Drawing.Point(10, 95)
        Me.lblInputFileNotes.Name = "lblInputFileNotes"
        Me.lblInputFileNotes.Size = New System.Drawing.Size(585, 18)
        Me.lblInputFileNotes.TabIndex = 6
        Me.lblInputFileNotes.Text = "Note: prefix and suffix residues will be automatically removed from the input pep" &
    "tides"
        '
        'fraProteinDelimitedFileOptions
        '
        Me.fraProteinDelimitedFileOptions.Controls.Add(Me.chkProteinFileSkipFirstLine)
        Me.fraProteinDelimitedFileOptions.Controls.Add(Me.cboProteinInputFileColumnOrdering)
        Me.fraProteinDelimitedFileOptions.Controls.Add(Me.lblProteinInputFileColumnOrdering)
        Me.fraProteinDelimitedFileOptions.Controls.Add(Me.txtProteinInputFileColumnDelimiter)
        Me.fraProteinDelimitedFileOptions.Controls.Add(Me.lblProteinInputFileColumnDelimiter)
        Me.fraProteinDelimitedFileOptions.Controls.Add(Me.cboProteinInputFileColumnDelimiter)
        Me.fraProteinDelimitedFileOptions.Location = New System.Drawing.Point(10, 18)
        Me.fraProteinDelimitedFileOptions.Name = "fraProteinDelimitedFileOptions"
        Me.fraProteinDelimitedFileOptions.Size = New System.Drawing.Size(604, 102)
        Me.fraProteinDelimitedFileOptions.TabIndex = 0
        Me.fraProteinDelimitedFileOptions.TabStop = False
        Me.fraProteinDelimitedFileOptions.Text = "Protein Delimited Input File Options"
        '
        'chkProteinFileSkipFirstLine
        '
        Me.chkProteinFileSkipFirstLine.Location = New System.Drawing.Point(317, 65)
        Me.chkProteinFileSkipFirstLine.Name = "chkProteinFileSkipFirstLine"
        Me.chkProteinFileSkipFirstLine.Size = New System.Drawing.Size(259, 27)
        Me.chkProteinFileSkipFirstLine.TabIndex = 5
        Me.chkProteinFileSkipFirstLine.Text = "Skip first line in protein input file"
        '
        'cboProteinInputFileColumnOrdering
        '
        Me.cboProteinInputFileColumnOrdering.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboProteinInputFileColumnOrdering.DropDownWidth = 70
        Me.cboProteinInputFileColumnOrdering.Location = New System.Drawing.Point(106, 28)
        Me.cboProteinInputFileColumnOrdering.Name = "cboProteinInputFileColumnOrdering"
        Me.cboProteinInputFileColumnOrdering.Size = New System.Drawing.Size(470, 24)
        Me.cboProteinInputFileColumnOrdering.TabIndex = 1
        '
        'lblProteinInputFileColumnOrdering
        '
        Me.lblProteinInputFileColumnOrdering.Location = New System.Drawing.Point(10, 30)
        Me.lblProteinInputFileColumnOrdering.Name = "lblProteinInputFileColumnOrdering"
        Me.lblProteinInputFileColumnOrdering.Size = New System.Drawing.Size(96, 18)
        Me.lblProteinInputFileColumnOrdering.TabIndex = 0
        Me.lblProteinInputFileColumnOrdering.Text = "Column Order"
        '
        'txtProteinInputFileColumnDelimiter
        '
        Me.txtProteinInputFileColumnDelimiter.Location = New System.Drawing.Point(230, 65)
        Me.txtProteinInputFileColumnDelimiter.MaxLength = 1
        Me.txtProteinInputFileColumnDelimiter.Name = "txtProteinInputFileColumnDelimiter"
        Me.txtProteinInputFileColumnDelimiter.Size = New System.Drawing.Size(39, 22)
        Me.txtProteinInputFileColumnDelimiter.TabIndex = 4
        Me.txtProteinInputFileColumnDelimiter.Text = ";"
        '
        'lblProteinInputFileColumnDelimiter
        '
        Me.lblProteinInputFileColumnDelimiter.Location = New System.Drawing.Point(10, 67)
        Me.lblProteinInputFileColumnDelimiter.Name = "lblProteinInputFileColumnDelimiter"
        Me.lblProteinInputFileColumnDelimiter.Size = New System.Drawing.Size(115, 18)
        Me.lblProteinInputFileColumnDelimiter.TabIndex = 2
        Me.lblProteinInputFileColumnDelimiter.Text = "Column Delimiter"
        '
        'cboProteinInputFileColumnDelimiter
        '
        Me.cboProteinInputFileColumnDelimiter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboProteinInputFileColumnDelimiter.DropDownWidth = 70
        Me.cboProteinInputFileColumnDelimiter.Location = New System.Drawing.Point(134, 65)
        Me.cboProteinInputFileColumnDelimiter.Name = "cboProteinInputFileColumnDelimiter"
        Me.cboProteinInputFileColumnDelimiter.Size = New System.Drawing.Size(84, 24)
        Me.cboProteinInputFileColumnDelimiter.TabIndex = 3
        '
        'TabPagePeakMatchingThresholds
        '
        Me.TabPagePeakMatchingThresholds.Controls.Add(Me.txtCoverage)
        Me.TabPagePeakMatchingThresholds.Controls.Add(Me.txtRTFCode)
        Me.TabPagePeakMatchingThresholds.Controls.Add(Me.txtCustomProteinSequence)
        Me.TabPagePeakMatchingThresholds.Controls.Add(Me.lblCustomProteinSequence)
        Me.TabPagePeakMatchingThresholds.Controls.Add(Me.chkAddSpace)
        Me.TabPagePeakMatchingThresholds.Controls.Add(Me.cboCharactersPerLine)
        Me.TabPagePeakMatchingThresholds.Controls.Add(Me.rtfRichTextBox)
        Me.TabPagePeakMatchingThresholds.Controls.Add(Me.dgResults)
        Me.TabPagePeakMatchingThresholds.Location = New System.Drawing.Point(4, 25)
        Me.TabPagePeakMatchingThresholds.Name = "TabPagePeakMatchingThresholds"
        Me.TabPagePeakMatchingThresholds.Size = New System.Drawing.Size(859, 340)
        Me.TabPagePeakMatchingThresholds.TabIndex = 3
        Me.TabPagePeakMatchingThresholds.Text = "Results Browser"
        Me.TabPagePeakMatchingThresholds.Visible = False
        '
        'txtCoverage
        '
        Me.txtCoverage.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.txtCoverage.Location = New System.Drawing.Point(614, 282)
        Me.txtCoverage.Name = "txtCoverage"
        Me.txtCoverage.ReadOnly = True
        Me.txtCoverage.Size = New System.Drawing.Size(260, 22)
        Me.txtCoverage.TabIndex = 7
        Me.txtCoverage.Text = "Coverage: 0%  (0 / 0)"
        '
        'txtRTFCode
        '
        Me.txtRTFCode.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.txtRTFCode.Location = New System.Drawing.Point(86, 18)
        Me.txtRTFCode.Multiline = True
        Me.txtRTFCode.Name = "txtRTFCode"
        Me.txtRTFCode.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.txtRTFCode.Size = New System.Drawing.Size(519, 172)
        Me.txtRTFCode.TabIndex = 1
        Me.txtRTFCode.WordWrap = False
        '
        'txtCustomProteinSequence
        '
        Me.txtCustomProteinSequence.AcceptsReturn = True
        Me.txtCustomProteinSequence.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.txtCustomProteinSequence.Location = New System.Drawing.Point(106, 283)
        Me.txtCustomProteinSequence.Multiline = True
        Me.txtCustomProteinSequence.Name = "txtCustomProteinSequence"
        Me.txtCustomProteinSequence.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtCustomProteinSequence.Size = New System.Drawing.Size(499, 45)
        Me.txtCustomProteinSequence.TabIndex = 6
        '
        'lblCustomProteinSequence
        '
        Me.lblCustomProteinSequence.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lblCustomProteinSequence.Location = New System.Drawing.Point(5, 283)
        Me.lblCustomProteinSequence.Name = "lblCustomProteinSequence"
        Me.lblCustomProteinSequence.Size = New System.Drawing.Size(105, 37)
        Me.lblCustomProteinSequence.TabIndex = 5
        Me.lblCustomProteinSequence.Text = "Custom Protein Sequence"
        '
        'chkAddSpace
        '
        Me.chkAddSpace.Location = New System.Drawing.Point(883, 7)
        Me.chkAddSpace.Name = "chkAddSpace"
        Me.chkAddSpace.Size = New System.Drawing.Size(144, 29)
        Me.chkAddSpace.TabIndex = 3
        Me.chkAddSpace.Text = "Add space every 10 residues"
        '
        'cboCharactersPerLine
        '
        Me.cboCharactersPerLine.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboCharactersPerLine.Location = New System.Drawing.Point(614, 12)
        Me.cboCharactersPerLine.Name = "cboCharactersPerLine"
        Me.cboCharactersPerLine.Size = New System.Drawing.Size(260, 24)
        Me.cboCharactersPerLine.TabIndex = 2
        '
        'rtfRichTextBox
        '
        Me.rtfRichTextBox.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.rtfRichTextBox.Location = New System.Drawing.Point(614, 46)
        Me.rtfRichTextBox.Name = "rtfRichTextBox"
        Me.rtfRichTextBox.Size = New System.Drawing.Size(234, 227)
        Me.rtfRichTextBox.TabIndex = 4
        Me.rtfRichTextBox.Text = ""
        Me.rtfRichTextBox.WordWrap = False
        '
        'dgResults
        '
        Me.dgResults.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.dgResults.CaptionText = "Results"
        Me.dgResults.DataMember = ""
        Me.dgResults.HeaderForeColor = System.Drawing.SystemColors.ControlText
        Me.dgResults.Location = New System.Drawing.Point(2, 18)
        Me.dgResults.Name = "dgResults"
        Me.dgResults.PreferredColumnWidth = 80
        Me.dgResults.Size = New System.Drawing.Size(605, 256)
        Me.dgResults.TabIndex = 0
        '
        'fraOutputFolderPath
        '
        Me.fraOutputFolderPath.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.fraOutputFolderPath.Controls.Add(Me.cmdSelectOutputFolder)
        Me.fraOutputFolderPath.Controls.Add(Me.txtOutputFolderPath)
        Me.fraOutputFolderPath.Location = New System.Drawing.Point(10, 148)
        Me.fraOutputFolderPath.Name = "fraOutputFolderPath"
        Me.fraOutputFolderPath.Size = New System.Drawing.Size(885, 64)
        Me.fraOutputFolderPath.TabIndex = 2
        Me.fraOutputFolderPath.TabStop = False
        Me.fraOutputFolderPath.Text = "Output folder path"
        '
        'cmdSelectOutputFolder
        '
        Me.cmdSelectOutputFolder.Location = New System.Drawing.Point(10, 18)
        Me.cmdSelectOutputFolder.Name = "cmdSelectOutputFolder"
        Me.cmdSelectOutputFolder.Size = New System.Drawing.Size(96, 37)
        Me.cmdSelectOutputFolder.TabIndex = 0
        Me.cmdSelectOutputFolder.Text = "Select folder"
        '
        'txtOutputFolderPath
        '
        Me.txtOutputFolderPath.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtOutputFolderPath.Location = New System.Drawing.Point(125, 21)
        Me.txtOutputFolderPath.Name = "txtOutputFolderPath"
        Me.txtOutputFolderPath.Size = New System.Drawing.Size(741, 22)
        Me.txtOutputFolderPath.TabIndex = 1
        '
        'lblStatus
        '
        Me.lblStatus.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblStatus.Location = New System.Drawing.Point(657, 69)
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(192, 51)
        Me.lblStatus.TabIndex = 7
        Me.lblStatus.Text = "Status ..."
        '
        'GUI
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(6, 15)
        Me.ClientSize = New System.Drawing.Size(914, 601)
        Me.Controls.Add(Me.fraOutputFolderPath)
        Me.Controls.Add(Me.tbsOptions)
        Me.Controls.Add(Me.fraPeptideInputFilePath)
        Me.Controls.Add(Me.fraProteinInputFilePath)
        Me.Menu = Me.MainMenuControl
        Me.Name = "GUI"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Protein Coverage Summarizer"
        Me.fraProteinInputFilePath.ResumeLayout(False)
        Me.fraProteinInputFilePath.PerformLayout()
        Me.fraPeptideInputFilePath.ResumeLayout(False)
        Me.fraPeptideInputFilePath.PerformLayout()
        Me.tbsOptions.ResumeLayout(False)
        Me.TabPageFileFormatOptions.ResumeLayout(False)
        Me.fraOptions.ResumeLayout(False)
        Me.fraPeptideDelimitedFileOptions.ResumeLayout(False)
        Me.fraPeptideDelimitedFileOptions.PerformLayout()
        Me.fraProteinDelimitedFileOptions.ResumeLayout(False)
        Me.fraProteinDelimitedFileOptions.PerformLayout()
        Me.TabPagePeakMatchingThresholds.ResumeLayout(False)
        Me.TabPagePeakMatchingThresholds.PerformLayout()
        CType(Me.dgResults, System.ComponentModel.ISupportInitialize).EndInit()
        Me.fraOutputFolderPath.ResumeLayout(False)
        Me.fraOutputFolderPath.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
#End Region
#End Region

#Region "Constants and Enums"
    Private Const XML_SETTINGS_FILE_NAME As String = "ProteinCoverageSummarizerSettings.xml"
    Private Const XML_SECTION_GUI_OPTIONS As String = "GUIOptions"

    Private Const COVERAGE_RESULTS_DATATABLE As String = "T_Coverage_Results"
    Private Const COL_NAME_PROTEIN_NAME As String = "Protein Name"
    Private Const COL_NAME_PROTEIN_COVERAGE As String = "Percent Coverage"
    Private Const COL_NAME_PROTEIN_DESCRIPTION As String = "Protein Description"
    Private Const COL_NAME_NON_UNIQUE_PEPTIDE_COUNT As String = "Non Unique Peptide Count"
    Private Const COL_NAME_UNIQUE_PEPTIDE_COUNT As String = "Unique Peptide Count"
    Private Const COL_NAME_PROTEIN_RESIDUE_COUNT As String = "Protein Residue count"
    Private Const COL_NAME_PROTEIN_SEQUENCE As String = "Protein Sequence"

    Private Const PROTEIN_INPUT_FILE_INDEX_OFFSET As Integer = 1

    Private Enum DelimiterCharConstants
        Space = 0
        Tab = 1
        Comma = 2
        Other = 3
    End Enum

    Private Enum eSequenceDisplayConstants
        UsePrevious = 0
        UseDataGrid = 1
        UseCustom = 2
    End Enum
#End Region

#Region "Classwide variables"
    Private mDSCoverageResults As DataSet
    Private mProteinSequenceColIndex As Integer
    Private mProteinDescriptionColVisible As Boolean

    Private mProteinCoverageSummarizer As clsProteinCoverageSummarizerRunner

    Private mXmlSettingsFilePath As String
    Private mSaveFullSettingsFileOnExit As Boolean
    Private mLastFolderUsed As String

#End Region

#Region "Properties"
    Public Property KeepDB As Boolean

#End Region

    Private Sub CloseProgram()
        Me.Close()
    End Sub

    Private Sub AutoDefineSearchAllProteins()
        If cboPeptideInputFileColumnOrdering.SelectedIndex = DelimitedFileReader.eDelimitedFileFormatCode.SequenceOnly Then
            chkSearchAllProteinsForPeptideSequence.Checked = True
        Else
            chkSearchAllProteinsForPeptideSequence.Checked = False
        End If
    End Sub

    Private Function ConfirmInputFilePaths() As Boolean
        If txtProteinInputFilePath.Text.Length = 0 And txtPeptideInputFilePath.Text.Length = 0 Then
            ShowErrorMessage("Please define the input file paths", "Missing Value")
            txtProteinInputFilePath.Focus()
            Return False
        ElseIf txtProteinInputFilePath.Text.Length = 0 Then
            ShowErrorMessage("Please define Protein input file path", "Missing Value")
            txtProteinInputFilePath.Focus()
            Return False
        ElseIf txtPeptideInputFilePath.Text.Length = 0 Then
            ShowErrorMessage("Please define Peptide input file path", "Missing Value")
            txtPeptideInputFilePath.Focus()
            Return False
        Else
            Return True
        End If
    End Function

    Private Sub CreateSummaryDataTable(strResultsFilePath As String)

        Dim srInFile As StreamReader
        Dim bytesRead As Long = 0

        Dim intLineCount As Integer
        Dim intIndex As Integer

        Dim strLineIn As String
        Dim strSplitLine As String()

        Dim blnProteinDescriptionPresent As Boolean

        Dim objNewRow As DataRow

        Try
            If strResultsFilePath Is Nothing OrElse strResultsFilePath.Length = 0 Then
                ' Output file not available
                Exit Sub
            End If

            If Not File.Exists(strResultsFilePath) Then
                ShowErrorMessage("Results file not found: " & strResultsFilePath)
            End If

            ' Clear the data source to prevent the data grid from updating
            dgResults.DataSource = Nothing

            ' Clear the dataset
            mDSCoverageResults.Tables(COVERAGE_RESULTS_DATATABLE).Clear()

            ' Open the file and read in the lines
            srInFile = New StreamReader(strResultsFilePath)
            intLineCount = 1
            blnProteinDescriptionPresent = False

            Do While srInFile.Peek <> -1
                strLineIn = srInFile.ReadLine
                bytesRead += strLineIn.Length + 2           ' Add 2 for CrLf

                If intLineCount = 1 Then
                    ' do nothing, skip the first line
                Else
                    strSplitLine = strLineIn.Split(ControlChars.Tab)

                    objNewRow = mDSCoverageResults.Tables(COVERAGE_RESULTS_DATATABLE).NewRow()
                    For intIndex = 0 To strSplitLine.Length - 1
                        If intIndex > clsProteinCoverageSummarizer.OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER - 1 Then Exit For

                        Try
                            Select Case Type.GetTypeCode(objNewRow(intIndex).GetType)
                                Case TypeCode.String
                                    objNewRow(intIndex) = strSplitLine(intIndex)
                                Case TypeCode.Double
                                    objNewRow(intIndex) = CDbl(strSplitLine(intIndex))
                                Case TypeCode.Single
                                    objNewRow(intIndex) = CSng(strSplitLine(intIndex))
                                Case TypeCode.Byte, TypeCode.Int16, TypeCode.Int32, TypeCode.Int64, TypeCode.UInt16, TypeCode.UInt32, TypeCode.UInt64
                                    objNewRow(intIndex) = CInt(strSplitLine(intIndex))
                                Case TypeCode.Boolean
                                    objNewRow(intIndex) = CBool(strSplitLine(intIndex))
                                Case Else
                                    objNewRow(intIndex) = strSplitLine(intIndex)
                            End Select
                        Catch ex As Exception
                            ' Ignore errors while populating the table
                        End Try
                    Next intIndex

                    If strSplitLine.Length >= clsProteinCoverageSummarizer.OUTPUT_FILE_PROTEIN_DESCRIPTION_COLUMN_NUMBER Then
                        If strSplitLine(clsProteinCoverageSummarizer.OUTPUT_FILE_PROTEIN_DESCRIPTION_COLUMN_NUMBER - 1).Length > 0 Then
                            blnProteinDescriptionPresent = True
                        End If
                    End If

                    ' Add the row to the Customers table.
                    mDSCoverageResults.Tables(COVERAGE_RESULTS_DATATABLE).Rows.Add(objNewRow)

                End If
                intLineCount += 1

                If intLineCount Mod 25 = 0 Then
                    lblProgress.Text = "Loading results: " & (bytesRead / srInFile.BaseStream.Length * 100).ToString("0.0") & "% complete"
                End If

            Loop

            srInFile.Close()

            ' Re-define the data source
            ' Bind the DataSet to the DataGrid
            With dgResults
                .DataSource = mDSCoverageResults
                .DataMember = COVERAGE_RESULTS_DATATABLE
            End With

            If blnProteinDescriptionPresent <> mProteinDescriptionColVisible Then
                mProteinDescriptionColVisible = blnProteinDescriptionPresent
                UpdateDataGridTableStyle()
            End If

            ' Display the sequence for the first protein
            If mDSCoverageResults.Tables(COVERAGE_RESULTS_DATATABLE).Rows.Count > 0 Then
                dgResults.CurrentRowIndex = 0
                ShowRichTextStart(eSequenceDisplayConstants.UseDataGrid)
            Else
                ShowRichText("", rtfRichTextBox)
            End If

            lblProgress.Text = "Results loaded"

        Catch ex As Exception

        End Try

    End Sub

    Private Sub DefineOutputFolderPath(strPeptideInputFilePath As String)

        Try
            If strPeptideInputFilePath.Length > 0 Then
                txtOutputFolderPath.Text = Path.GetDirectoryName(strPeptideInputFilePath)
            End If
        Catch ex As Exception
            ShowErrorMessage("Error defining default output folder path: " & ex.Message, "Error")
        End Try

    End Sub

    Private Sub EnableDisableControls()

        Dim blnFastaFile = clsProteinFileDataCache.IsFastaFile(txtProteinInputFilePath.Text)

        cboProteinInputFileColumnOrdering.Enabled = Not blnFastaFile
        cboProteinInputFileColumnDelimiter.Enabled = Not blnFastaFile
        txtProteinInputFileColumnDelimiter.Enabled = Not blnFastaFile

        chkSearchAllProteinsSkipCoverageComputationSteps.Enabled = chkSearchAllProteinsForPeptideSequence.Checked

    End Sub

    Private Function GetSettingsFilePath() As String
        Return FileProcessor.ProcessFilesOrDirectoriesBase.GetSettingsFilePathLocal("ProteinCoverageSummarizer", XML_SETTINGS_FILE_NAME)
    End Function

    Private Sub IniFileLoadOptions(blnUpdateIOPaths As Boolean)
        ' Prompts the user to select a file to load the options from

        Dim strFilePath As String

        Dim objOpenFile As New OpenFileDialog()

        strFilePath = mXmlSettingsFilePath

        With objOpenFile
            .AddExtension = True
            .CheckFileExists = True
            .CheckPathExists = True
            .DefaultExt = ".xml"
            .DereferenceLinks = True
            .Multiselect = False
            .ValidateNames = True

            .Filter = "Settings files (*.xml)|*.xml|All files (*.*)|*.*"

            .FilterIndex = 1

            If strFilePath.Length > 0 Then
                Try
                    .InitialDirectory = Directory.GetParent(strFilePath).ToString
                Catch
                    .InitialDirectory = FileProcessor.ProcessFilesOrDirectoriesBase.GetAppDirectoryPath()
                End Try
            Else
                .InitialDirectory = FileProcessor.ProcessFilesOrDirectoriesBase.GetAppDirectoryPath()
            End If

            If File.Exists(strFilePath) Then
                .FileName = Path.GetFileName(strFilePath)
            End If

            .Title = "Specify file to load options from"

            .ShowDialog()
            If .FileName.Length > 0 Then
                mXmlSettingsFilePath = .FileName

                IniFileLoadOptions(mXmlSettingsFilePath, blnUpdateIOPaths)
            End If
        End With

    End Sub

    Private Sub IniFileLoadOptions(strFilePath As String, blnUpdateIOPaths As Boolean)

        Dim objSettingsFile As XmlSettingsFileAccessor

        Dim objProteinCoverageSummarizer As New clsProteinCoverageSummarizerRunner()
        Dim eColumnOrdering As DelimitedFileReader.eDelimitedFileFormatCode

        Try

            If strFilePath Is Nothing OrElse strFilePath.Length = 0 Then
                ' No parameter file specified; nothing to load
                Exit Sub
            End If

            If Not File.Exists(strFilePath) Then
                ShowErrorMessage("Parameter file not Found: " & strFilePath)
                Exit Sub
            End If

            objSettingsFile = New XmlSettingsFileAccessor

            If objSettingsFile.LoadSettings(strFilePath) Then

                ' Read the GUI-specific options from the XML file
                If Not objSettingsFile.SectionPresent(XML_SECTION_GUI_OPTIONS) Then
                    ShowErrorMessage("The node '<section name=""" & XML_SECTION_GUI_OPTIONS & """> was not found in the parameter file: " & strFilePath, "Invalid File")
                    mSaveFullSettingsFileOnExit = True
                Else
                    If blnUpdateIOPaths Then
                        txtProteinInputFilePath.Text = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFilePath", txtProteinInputFilePath.Text)
                        txtPeptideInputFilePath.Text = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFilePath", txtPeptideInputFilePath.Text)
                        txtOutputFolderPath.Text = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "OutputFolderPath", txtOutputFolderPath.Text)
                    End If

                    cboProteinInputFileColumnDelimiter.SelectedIndex = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFileColumnDelimiterIndex", cboProteinInputFileColumnDelimiter.SelectedIndex)
                    txtProteinInputFileColumnDelimiter.Text = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFileColumnDelimiter", txtProteinInputFileColumnDelimiter.Text)

                    cboPeptideInputFileColumnDelimiter.SelectedIndex = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFileColumnDelimiterIndex", cboPeptideInputFileColumnDelimiter.SelectedIndex)
                    txtPeptideInputFileColumnDelimiter.Text = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFileColumnDelimiter", txtPeptideInputFileColumnDelimiter.Text)

                    cboCharactersPerLine.SelectedIndex = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "ProteinSequenceCharactersPerLine", cboCharactersPerLine.SelectedIndex)
                    chkAddSpace.Checked = objSettingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "ProteinSequenceAddSpace", chkAddSpace.Checked)

                    If Not objSettingsFile.SectionPresent(clsProteinCoverageSummarizer.XML_SECTION_PROCESSING_OPTIONS) Then
                        ShowErrorMessage("The node '<section name=""" & clsProteinCoverageSummarizer.XML_SECTION_PROCESSING_OPTIONS & """> was not found in the parameter file: ", "Invalid File")
                        mSaveFullSettingsFileOnExit = True
                    Else
                        Try
                            eColumnOrdering = CType(objSettingsFile.GetParam(clsProteinCoverageSummarizer.XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileFormatCode", cboProteinInputFileColumnOrdering.SelectedIndex + PROTEIN_INPUT_FILE_INDEX_OFFSET), DelimitedFileReader.eDelimitedFileFormatCode)
                        Catch ex As Exception
                            eColumnOrdering = DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence
                        End Try

                        Try
                            cboProteinInputFileColumnOrdering.SelectedIndex = eColumnOrdering - PROTEIN_INPUT_FILE_INDEX_OFFSET
                        Catch ex As Exception
                            If cboProteinInputFileColumnOrdering.Items.Count > 0 Then
                                cboProteinInputFileColumnOrdering.SelectedIndex = 0
                            End If
                        End Try

                        Try
                            eColumnOrdering = CType(objSettingsFile.GetParam(clsProteinCoverageSummarizer.XML_SECTION_PROCESSING_OPTIONS, "PeptideFileFormatCode", cboPeptideInputFileColumnOrdering.SelectedIndex), DelimitedFileReader.eDelimitedFileFormatCode)
                        Catch ex As Exception
                            eColumnOrdering = DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence
                        End Try

                        Try
                            cboPeptideInputFileColumnOrdering.SelectedIndex = eColumnOrdering
                        Catch ex As Exception
                            If cboPeptideInputFileColumnOrdering.Items.Count > 0 Then
                                cboPeptideInputFileColumnOrdering.SelectedIndex = 0
                            End If
                        End Try

                        ' Note: The following settings are read using LoadProcessingClassOptions()
                        'chkPeptideFileSkipFirstLine
                        'chkProteinFileSkipFirstLine

                        'chkOutputProteinSequence
                        'chkSearchAllProteinsForPeptideSequence
                        'chkSearchAllProteinsSaveDetails
                        'chkSearchAllProteinsSkipCoverageComputationSteps
                        'chkTrackPeptideCounts
                        'chkRemoveSymbolCharacters
                        'chkMatchPeptidePrefixAndSuffixToProtein
                    End If

                End If
            End If

        Catch ex As Exception
            ShowErrorMessage("Error in IniFileLoadOptions: " & ex.Message)
        End Try

        Try
            objProteinCoverageSummarizer.LoadParameterFileSettings(strFilePath)
        Catch ex As Exception
            ShowErrorMessage("Error calling LoadParameterFileSettings: " & ex.ToString)
        End Try

        Try
            LoadProcessingClassOptions(objProteinCoverageSummarizer)
        Catch ex As Exception
            ShowErrorMessage("Error calling LoadProcessingClassOptions: " & ex.ToString)
        End Try


    End Sub

    Private Sub IniFileSaveOptions(strSettingsFilePath As String, Optional blnSaveExtendedOptions As Boolean = False)
        Dim objSettingsFile As New XmlSettingsFileAccessor()

        Const XML_SECTION_PROCESSING_OPTIONS = "ProcessingOptions"

        Try
            Dim fiSettingsFile = New FileInfo(strSettingsFilePath)
            If Not fiSettingsFile.Exists Then
                blnSaveExtendedOptions = True
            End If
        Catch
            'Ignore errors here
        End Try

        ' Pass True to .LoadSettings() to turn off case sensitive matching
        Try
            objSettingsFile.LoadSettings(strSettingsFilePath, True)
        Catch ex As Exception
            Exit Sub
        End Try

        Try

            objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFilePath", txtProteinInputFilePath.Text)
            objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFilePath", txtPeptideInputFilePath.Text)
            objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "OutputFolderPath", txtOutputFolderPath.Text)

            If blnSaveExtendedOptions Then
                objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFileColumnDelimiterIndex", cboProteinInputFileColumnDelimiter.SelectedIndex)
                objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFileColumnDelimiter", txtProteinInputFileColumnDelimiter.Text)

                objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFileColumnDelimiterIndex", cboPeptideInputFileColumnDelimiter.SelectedIndex)
                objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFileColumnDelimiter", txtPeptideInputFileColumnDelimiter.Text)

                objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "ProteinSequenceCharactersPerLine", cboCharactersPerLine.SelectedIndex)
                objSettingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "ProteinSequenceAddSpace", chkAddSpace.Checked)

                objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "OutputProteinSequence", chkOutputProteinSequence.Checked)
                objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "SearchAllProteinsForPeptideSequence", chkSearchAllProteinsForPeptideSequence.Checked)

                objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "SaveProteinToPeptideMappingFile", chkSaveProteinToPeptideMappingFile.Checked)
                objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "SearchAllProteinsSkipCoverageComputationSteps", chkSearchAllProteinsSkipCoverageComputationSteps.Checked)

                objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "TrackPeptideCounts", chkTrackPeptideCounts.Checked)
                objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "RemoveSymbolCharacters", chkRemoveSymbolCharacters.Checked)
                objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "MatchPeptidePrefixAndSuffixToProtein", chkMatchPeptidePrefixAndSuffixToProtein.Checked)
                objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "IgnoreILDifferences", chkIgnoreILDifferences.Checked)

                objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideInputFileDelimiter", LookupColumnDelimiter(cboPeptideInputFileColumnDelimiter, txtPeptideInputFileColumnDelimiter, ControlChars.Tab))
                objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideFileFormatCode", cboPeptideInputFileColumnOrdering.SelectedIndex)
                objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideFileSkipFirstLine", chkPeptideFileSkipFirstLine.Checked)

                objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileDelimiter", LookupColumnDelimiter(cboPeptideInputFileColumnDelimiter, txtPeptideInputFileColumnDelimiter, ControlChars.Tab))
                objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileFormatCode", cboProteinInputFileColumnOrdering.SelectedIndex + PROTEIN_INPUT_FILE_INDEX_OFFSET)
                objSettingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "ProteinFileSkipFirstLine", chkProteinFileSkipFirstLine.Checked)
            End If

            objSettingsFile.SaveSettings()
        Catch ex As Exception
            ShowErrorMessage("Error storing parameter in settings file: " & Path.GetFileName(strSettingsFilePath), "Error")
        End Try


    End Sub

    Private Sub InitializeControls()

        cmdAbort.Visible = False
        cmdStart.Visible = True
        txtRTFCode.Visible = False

        mLastFolderUsed = Application.StartupPath

        lblProgress.Text = String.Empty
        lblStatus.Text = String.Empty

        PopulateComboBoxes()
        InitializeDataGrid()

        ResetToDefaults()

        Try
            ' Try loading from the default xml file
            IniFileLoadOptions(mXmlSettingsFilePath, True)
        Catch ex As Exception
            ' Ignore any errors here
            ShowErrorMessage("Error loading settings from " & mXmlSettingsFilePath & ": " & ex.Message)
        End Try

    End Sub

    Private Sub InitializeDataGrid()

        Try

            ' Make the Peak Matching Thresholds data table
            Dim dtCoverageResults = New DataTable(COVERAGE_RESULTS_DATATABLE)

            ' Add the columns to the data table
            ADONetRoutines.AppendColumnStringToTable(dtCoverageResults, COL_NAME_PROTEIN_NAME, String.Empty)
            ADONetRoutines.AppendColumnSingleToTable(dtCoverageResults, COL_NAME_PROTEIN_COVERAGE)
            ADONetRoutines.AppendColumnStringToTable(dtCoverageResults, COL_NAME_PROTEIN_DESCRIPTION, String.Empty)
            ADONetRoutines.AppendColumnIntegerToTable(dtCoverageResults, COL_NAME_NON_UNIQUE_PEPTIDE_COUNT)
            ADONetRoutines.AppendColumnIntegerToTable(dtCoverageResults, COL_NAME_UNIQUE_PEPTIDE_COUNT)
            ADONetRoutines.AppendColumnIntegerToTable(dtCoverageResults, COL_NAME_PROTEIN_RESIDUE_COUNT)
            ADONetRoutines.AppendColumnStringToTable(dtCoverageResults, COL_NAME_PROTEIN_SEQUENCE, String.Empty)

            ' Note that Protein Sequence should be at ColIndex 6 = clsProteinCoverageSummarizer.OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER-1
            mProteinSequenceColIndex = clsProteinCoverageSummarizer.OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER - 1

            ' Could define a primary key if we wanted
            'With dtCoverageResults
            '    Dim PrimaryKeyColumn As System.Data.DataColumn() = New DataColumn() {.Columns(COL_NAME_PROTEIN_NAME)}
            '    .PrimaryKey = PrimaryKeyColumn
            'End With

            ' Instantiate the dataset
            mDSCoverageResults = New DataSet(COVERAGE_RESULTS_DATATABLE)

            ' Add the new DataTable to the DataSet.
            mDSCoverageResults.Tables.Add(dtCoverageResults)

            ' Bind the DataSet to the DataGrid
            With dgResults
                .DataSource = mDSCoverageResults
                .DataMember = COVERAGE_RESULTS_DATATABLE
            End With

            mProteinDescriptionColVisible = False

            ' Update the grid's table style
            UpdateDataGridTableStyle()

        Catch ex As Exception
            ShowErrorMessage("Error in InitializeDataGrid: " & ex.ToString)
        End Try

    End Sub

    Private Sub LoadProcessingClassOptions(ByRef objProteinCoverageSummarizer As clsProteinCoverageSummarizerRunner)

        Try
            With objProteinCoverageSummarizer
                chkPeptideFileSkipFirstLine.Checked = .PeptideFileSkipFirstLine
                chkProteinFileSkipFirstLine.Checked = .ProteinDataDelimitedFileSkipFirstLine

                chkOutputProteinSequence.Checked = .OutputProteinSequence
                chkSearchAllProteinsForPeptideSequence.Checked = .SearchAllProteinsForPeptideSequence

                chkSaveProteinToPeptideMappingFile.Checked = .SaveProteinToPeptideMappingFile
                chkSearchAllProteinsSkipCoverageComputationSteps.Checked = .SearchAllProteinsSkipCoverageComputationSteps

                chkTrackPeptideCounts.Checked = .TrackPeptideCounts
                chkRemoveSymbolCharacters.Checked = .RemoveSymbolCharacters
                chkMatchPeptidePrefixAndSuffixToProtein.Checked = .MatchPeptidePrefixAndSuffixToProtein
                chkIgnoreILDifferences.Checked = .IgnoreILDifferences
            End With

        Catch ex As Exception
            ShowErrorMessage("Error in LoadProcessingClassOptions: " & ex.Message)
        End Try

    End Sub

    Private Function LookupColumnDelimiter(delimiterCombobox As ListControl, delimiterTextBox As Control, defaultDelimiter As Char) As Char
        Try
            Return LookupColumnDelimiterChar(delimiterCombobox.SelectedIndex, delimiterTextBox.Text, defaultDelimiter)
        Catch ex As Exception
            Return ControlChars.Tab
        End Try
    End Function

    Private Function LookupColumnDelimiterChar(delimiterIndex As Integer, customDelimiter As String, defaultDelimiter As Char) As Char

        Dim delimiter As String

        Select Case delimiterIndex
            Case DelimiterCharConstants.Space
                delimiter = " "
            Case DelimiterCharConstants.Tab
                delimiter = ControlChars.Tab
            Case DelimiterCharConstants.Comma
                delimiter = ","
            Case Else
                ' Includes DelimiterCharConstants.Other
                delimiter = String.Copy(customDelimiter)
        End Select

        If delimiter Is Nothing OrElse delimiter.Length = 0 Then
            delimiter = String.Copy(defaultDelimiter)
        End If

        Try
            Return delimiter.Chars(0)
        Catch ex As Exception
            Return ControlChars.Tab
        End Try

    End Function

    Private Sub PopulateComboBoxes()
        With cboProteinInputFileColumnDelimiter
            With .Items
                .Clear()
                .Insert(0, "Space")
                .Insert(1, "Tab")
                .Insert(2, "Comma")
                .Insert(3, "Other")
            End With
            .SelectedIndex = 1
        End With

        With cboPeptideInputFileColumnDelimiter
            With .Items
                .Insert(0, "Space")
                .Insert(1, "Tab")
                .Insert(2, "Comma")
                .Insert(3, "Other")
            End With
            .SelectedIndex = 1
        End With

        With cboCharactersPerLine
            With .Items
                .Clear()
                .Insert(0, " 40 Characters per line")
                .Insert(1, " 50 Characters per line")
                .Insert(2, " 60 Characters per line")
            End With
            .SelectedIndex = 0
        End With

        With cboProteinInputFileColumnOrdering
            With .Items
                .Clear()
                ' Note: Skipping ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode.SequenceOnly since a Protein Sequence Only file is inappropriate for this program
                .Insert(DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence - PROTEIN_INPUT_FILE_INDEX_OFFSET, "ProteinName and Sequence")
                .Insert(DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Description_Sequence - PROTEIN_INPUT_FILE_INDEX_OFFSET, "ProteinName, Description, Seq")
                .Insert(DelimitedFileReader.eDelimitedFileFormatCode.UniqueID_Sequence - PROTEIN_INPUT_FILE_INDEX_OFFSET, "UniqueID and Seq")
                .Insert(DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_PeptideSequence_UniqueID - PROTEIN_INPUT_FILE_INDEX_OFFSET, "ProteinName, Seq, UniqueID")
                .Insert(DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_PeptideSequence_UniqueID_Mass_NET - PROTEIN_INPUT_FILE_INDEX_OFFSET, "ProteinName, Seq, UniqueID, Mass, Time")
                .Insert(DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_PeptideSequence_UniqueID_Mass_NET_NETStDev_DiscriminantScore - PROTEIN_INPUT_FILE_INDEX_OFFSET, "ProteinName, Seq, UniqueID, Mass, Time, TimeStDev, DiscriminantScore")
                .Insert(DelimitedFileReader.eDelimitedFileFormatCode.UniqueID_Sequence_Mass_NET - PROTEIN_INPUT_FILE_INDEX_OFFSET, "UniqueID, Seq, Mass, Time")
            End With
            .SelectedIndex = DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Description_Sequence - PROTEIN_INPUT_FILE_INDEX_OFFSET
        End With


        With cboPeptideInputFileColumnOrdering
            With .Items
                .Clear()
                .Insert(DelimitedFileReader.eDelimitedFileFormatCode.SequenceOnly, "Sequence Only")
                .Insert(DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence, "ProteinName and Sequence")
            End With
            .SelectedIndex = DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence
        End With
    End Sub

    Private Sub ResetToDefaults()

        cboProteinInputFileColumnOrdering.SelectedIndex = DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence - PROTEIN_INPUT_FILE_INDEX_OFFSET
        cboProteinInputFileColumnDelimiter.SelectedIndex = 1
        txtProteinInputFileColumnDelimiter.Text = ";"
        chkProteinFileSkipFirstLine.Checked = False

        cboPeptideInputFileColumnOrdering.SelectedIndex = DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence
        cboPeptideInputFileColumnDelimiter.SelectedIndex = 1
        txtPeptideInputFileColumnDelimiter.Text = ";"
        chkPeptideFileSkipFirstLine.Checked = False

        chkOutputProteinSequence.Checked = True
        chkSearchAllProteinsForPeptideSequence.Checked = False

        chkSaveProteinToPeptideMappingFile.Checked = True
        chkSearchAllProteinsSkipCoverageComputationSteps.Checked = False

        chkTrackPeptideCounts.Checked = True
        chkRemoveSymbolCharacters.Checked = True
        chkMatchPeptidePrefixAndSuffixToProtein.Checked = False

        cboCharactersPerLine.SelectedIndex = 0
        chkAddSpace.Checked = True

        mXmlSettingsFilePath = GetSettingsFilePath()
        FileProcessor.ProcessFilesOrDirectoriesBase.CreateSettingsFileIfMissing(mXmlSettingsFilePath)

    End Sub

    Private Sub SelectOutputFolder()

        Dim folderBrowserDialog As New VistaFolderBrowserDialog()

        If txtOutputFolderPath.TextLength > 0 Then
            folderBrowserDialog.SelectedPath = txtOutputFolderPath.Text
        Else
            folderBrowserDialog.SelectedPath = mLastFolderUsed
        End If

        Dim result = folderBrowserDialog.ShowDialog()
        If result = DialogResult.OK Then
            txtOutputFolderPath.Text = folderBrowserDialog.SelectedPath
            mLastFolderUsed = folderBrowserDialog.SelectedPath
        End If

    End Sub

    Private Sub SelectProteinInputFile()
        Dim eResult As DialogResult
        Dim dlgOpenFileDialog = New OpenFileDialog() With {
            .Filter = "Fasta Files (*.fasta)|*.fasta|Text Files(*.txt)|*.txt|All Files (*.*)|*.*",
            .FilterIndex = 3
        }

        eResult = dlgOpenFileDialog.ShowDialog()
        If eResult = DialogResult.OK Then
            txtProteinInputFilePath.Text = dlgOpenFileDialog.FileName
            mLastFolderUsed = Path.GetDirectoryName(dlgOpenFileDialog.FileName)
        End If

    End Sub

    Private Sub SelectPeptideInputFile()
        Dim eResult As DialogResult
        Dim dlgOpenFileDialog As OpenFileDialog

        dlgOpenFileDialog = New OpenFileDialog() With {
            .InitialDirectory = mLastFolderUsed,
            .Filter = "Text Files(*.txt)|*.txt|All Files (*.*)|*.*",
            .FilterIndex = 1
        }

        eResult = dlgOpenFileDialog.ShowDialog()
        If eResult = DialogResult.OK Then
            txtPeptideInputFilePath.Text = dlgOpenFileDialog.FileName
            mLastFolderUsed = Path.GetDirectoryName(dlgOpenFileDialog.FileName)
        End If

    End Sub

    Private Function SetOptionsFromGUI(objProteinCoverageSummarizer As clsProteinCoverageSummarizerRunner) As Boolean
        Try
            With objProteinCoverageSummarizer

                .ProteinInputFilePath = txtProteinInputFilePath.Text

                .ProteinDataDelimitedFileFormatCode = CType(cboProteinInputFileColumnOrdering.SelectedIndex + PROTEIN_INPUT_FILE_INDEX_OFFSET, DelimitedFileReader.eDelimitedFileFormatCode)
                .ProteinDataDelimitedFileDelimiter = LookupColumnDelimiter(cboProteinInputFileColumnDelimiter, txtProteinInputFileColumnDelimiter, ControlChars.Tab)
                .ProteinDataDelimitedFileSkipFirstLine = chkProteinFileSkipFirstLine.Checked
                .ProteinDataRemoveSymbolCharacters = chkRemoveSymbolCharacters.Checked
                .ProteinDataIgnoreILDifferences = chkIgnoreILDifferences.Checked

                'peptide file options
                .PeptideFileFormatCode = CType(cboPeptideInputFileColumnOrdering.SelectedIndex, clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode)
                .PeptideInputFileDelimiter = LookupColumnDelimiter(cboPeptideInputFileColumnDelimiter, txtPeptideInputFileColumnDelimiter, ControlChars.Tab)
                .PeptideFileSkipFirstLine = chkPeptideFileSkipFirstLine.Checked

                'processing options
                .OutputProteinSequence = chkOutputProteinSequence.Checked
                .SearchAllProteinsForPeptideSequence = chkSearchAllProteinsForPeptideSequence.Checked

                .SaveProteinToPeptideMappingFile = chkSaveProteinToPeptideMappingFile.Checked

                If chkSaveProteinToPeptideMappingFile.Checked Then
                    .SearchAllProteinsSkipCoverageComputationSteps = chkSearchAllProteinsSkipCoverageComputationSteps.Checked
                Else
                    .SearchAllProteinsSkipCoverageComputationSteps = False
                End If

                .TrackPeptideCounts = chkTrackPeptideCounts.Checked
                .RemoveSymbolCharacters = chkRemoveSymbolCharacters.Checked
                .MatchPeptidePrefixAndSuffixToProtein = chkMatchPeptidePrefixAndSuffixToProtein.Checked
                .IgnoreILDifferences = chkIgnoreILDifferences.Checked

            End With
        Catch ex As Exception
            Return False
        End Try

        Return True

    End Function

    Private Sub ShowAboutBox()
        Dim strMessage As String

        strMessage = "This program reads in a .fasta or .txt file containing protein names and sequences (and optionally descriptions)." & ControlChars.NewLine
        strMessage &= "The program also reads in a .txt file containing peptide sequences and protein names (though protein name is optional) then uses this information to compute the sequence coverage percent for each protein." & ControlChars.NewLine & ControlChars.NewLine

        strMessage &= "Program written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA) in 2005" & ControlChars.NewLine & ControlChars.NewLine

        strMessage &= "This is version " & Application.ProductVersion & " (" & PROGRAM_DATE & ")" & ControlChars.NewLine & ControlChars.NewLine

        strMessage &= "E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov" & ControlChars.NewLine
        strMessage &= "Website: https://omics.pnl.gov or https://panomics.pnl.gov/" & ControlChars.NewLine & ControlChars.NewLine

        strMessage &= "Licensed under the 2-Clause BSD License; https://opensource.org/licenses/BSD-2-Clause" & ControlChars.NewLine
        strMessage &= "Copyright 2018 Battelle Memorial Institute" & ControlChars.NewLine & ControlChars.NewLine

        MessageBox.Show(strMessage, "About", MessageBoxButtons.OK, MessageBoxIcon.Information)

    End Sub

    Private Sub ShowRichTextStart()
        ShowRichTextStart(eSequenceDisplayConstants.UsePrevious)
    End Sub

    Private Sub ShowRichTextStart(eSequenceDisplayMode As eSequenceDisplayConstants)
        Static lastSequenceWasDataGrid As Boolean
        Dim useDataGrid As Boolean

        Select Case eSequenceDisplayMode
            Case eSequenceDisplayConstants.UseDataGrid
                useDataGrid = True
            Case eSequenceDisplayConstants.UseCustom
                useDataGrid = False
            Case Else
                ' Includes Use Previous
                useDataGrid = lastSequenceWasDataGrid
        End Select

        lastSequenceWasDataGrid = useDataGrid
        If useDataGrid Then
            Try
                If dgResults.CurrentRowIndex >= 0 Then
                    If Not dgResults.Item(dgResults.CurrentRowIndex, mProteinSequenceColIndex) Is Nothing Then
                        ShowRichText(CStr(dgResults.Item(dgResults.CurrentRowIndex, mProteinSequenceColIndex)), rtfRichTextBox)
                    End If
                End If
            Catch ex As Exception
                ' Ignore errors here
            End Try
        Else
            ShowRichText(txtCustomProteinSequence.Text, rtfRichTextBox)
        End If

    End Sub

    Protected Sub ShowErrorMessage(strMessage As String)
        ShowErrorMessage(strMessage, "Error")
    End Sub

    Protected Sub ShowErrorMessage(strMessage As String, strCaption As String)
        MessageBox.Show(strMessage, strCaption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
    End Sub

    Private Sub ShowRichText(strSequenceToShow As String, objRichTextBox As RichTextBox)

        Dim intIndex As Integer
        Dim intModValue As Integer

        Dim intCharCount As Integer
        Dim intUppercaseCount As Integer
        Dim sngCoveragePercent As Single

        Dim strRtf As String
        Dim reReplaceSymbols As Regex

        ' Define a RegEx to remove whitespace characters
        reReplaceSymbols = New Regex("[ \t\r\n]", RegexOptions.Compiled)

        Dim blnInUpperRegion As Boolean

        Try
            ' Lookup the number of characters per line
            Select Case cboCharactersPerLine.SelectedIndex
                Case 0
                    intModValue = 40
                Case 1
                    intModValue = 50
                Case 2
                    intModValue = 60
                Case Else
                    intModValue = 40
            End Select

            ' Remove any spaces, tabs, CR, or LF characters in strSequenceToShow
            strSequenceToShow = reReplaceSymbols.Replace(strSequenceToShow, String.Empty)

            ' Define the base RTF text
            ' ReSharper disable StringLiteralTypo
            strRtf = "{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Courier New;}}" &
               "{\colortbl\red0\green0\blue0;\red255\green0\blue0;}" &
               "\viewkind4\uc1\pard\f0\fs20 "
            ' ReSharper restore StringLiteralTypo

            blnInUpperRegion = False
            intCharCount = 0
            intUppercaseCount = 0
            If strSequenceToShow Is Nothing Then strSequenceToShow = String.Empty

            For intIndex = 0 To strSequenceToShow.Length - 1

                If intIndex > 0 Then
                    If intIndex Mod intModValue = 0 Then
                        ' Add a new line
                        strRtf &= "\par "
                    Else
                        If chkAddSpace.Checked = True AndAlso intIndex Mod 10 = 0 Then
                            ' Add a space every 10 residues
                            strRtf &= " "
                        End If
                    End If
                End If

                If Char.IsUpper(strSequenceToShow.Chars(intIndex)) Then
                    intCharCount += 1
                    intUppercaseCount += 1
                    If Not blnInUpperRegion Then
                        strRtf &= "{\cf1 {\b "
                        blnInUpperRegion = True
                    End If
                Else
                    If Char.IsLower(strSequenceToShow.Chars(intIndex)) Then
                        intCharCount += 1
                    End If

                    If blnInUpperRegion Then
                        strRtf &= "}}"
                        blnInUpperRegion = False
                    End If
                End If

                strRtf &= strSequenceToShow.Chars(intIndex)

            Next intIndex

            ' Add a final paragraph mark
            strRtf &= "\par}"

            objRichTextBox.Rtf = strRtf

            txtRTFCode.Text = objRichTextBox.Rtf

            If intCharCount > 0 Then
                sngCoveragePercent = CSng(intUppercaseCount / intCharCount * 100)
            Else
                sngCoveragePercent = 0
            End If
            txtCoverage.Text = "Coverage: " & Math.Round(sngCoveragePercent, 3) & "%  (" & intUppercaseCount & " / " & intCharCount & ")"

        Catch ex As Exception
            ShowErrorMessage("Error in ShowRichText: " & ex.Message)
        End Try

    End Sub

    Private Sub Start()
        Dim blnSuccess As Boolean

        Try
            Cursor.Current = Cursors.WaitCursor

            cmdAbort.Visible = True
            cmdStart.Visible = False

            mProteinCoverageSummarizer = New clsProteinCoverageSummarizerRunner() With {
                .CallingAppHandlesEvents = True,
                .KeepDB = KeepDB
            }

            AddHandler mProteinCoverageSummarizer.StatusEvent, AddressOf ProteinCoverageSummarizer_StatusEvent
            AddHandler mProteinCoverageSummarizer.ErrorEvent, AddressOf ProteinCoverageSummarizer_ErrorEvent
            AddHandler mProteinCoverageSummarizer.WarningEvent, AddressOf ProteinCoverageSummarizer_WarningEvent

            AddHandler mProteinCoverageSummarizer.ProgressUpdate, AddressOf ProteinCoverageSummarizer_ProgressChanged
            AddHandler mProteinCoverageSummarizer.ProgressReset, AddressOf ProteinCoverageSummarizer_ProgressReset

            blnSuccess = SetOptionsFromGUI(mProteinCoverageSummarizer)
            If blnSuccess Then
                blnSuccess = mProteinCoverageSummarizer.ProcessFile(txtPeptideInputFilePath.Text, txtOutputFolderPath.Text)

                If blnSuccess And Not (mProteinCoverageSummarizer.SearchAllProteinsForPeptideSequence And mProteinCoverageSummarizer.SearchAllProteinsSkipCoverageComputationSteps) Then
                    CreateSummaryDataTable(mProteinCoverageSummarizer.ResultsFilePath)
                End If

                If lblStatus.Text.StartsWith("Done (9") Then
                    lblStatus.Text = "Done"
                End If
            Else
                ShowErrorMessage("Error initializing Protein File Parser General Options.")
            End If

            Cursor.Current = Cursors.Default

        Catch ex As Exception
            ShowErrorMessage("Error in Start: " & ex.Message)
        Finally
            cmdAbort.Visible = False
            cmdStart.Visible = True
        End Try

    End Sub

    Private Sub ToggleRTFCodeVisible()
        mnuEditShowRTF.Checked = Not mnuEditShowRTF.Checked
        txtRTFCode.Visible = mnuEditShowRTF.Checked
    End Sub

    Private Sub UpdateDataGridTableStyle()

        ' Define the coverage results table style
        Dim tsResults As New DataGridTableStyle

        ' Setting the MappingName of the table style to COVERAGE_RESULTS_DATATABLE will cause this style to be used with that table
        With tsResults
            .MappingName = COVERAGE_RESULTS_DATATABLE
            .AllowSorting = True
            .ColumnHeadersVisible = True
            .RowHeadersVisible = True
            .ReadOnly = True
        End With

        ADONetRoutines.AppendColumnToTableStyle(tsResults, COL_NAME_PROTEIN_NAME, COL_NAME_PROTEIN_NAME, 100)
        ADONetRoutines.AppendColumnToTableStyle(tsResults, COL_NAME_PROTEIN_COVERAGE, COL_NAME_PROTEIN_COVERAGE, 95)

        If mProteinDescriptionColVisible Then
            ADONetRoutines.AppendColumnToTableStyle(tsResults, COL_NAME_PROTEIN_DESCRIPTION, COL_NAME_PROTEIN_DESCRIPTION, 100)
        Else
            ADONetRoutines.AppendColumnToTableStyle(tsResults, COL_NAME_PROTEIN_DESCRIPTION, COL_NAME_PROTEIN_DESCRIPTION, 0)
        End If

        ADONetRoutines.AppendColumnToTableStyle(tsResults, COL_NAME_NON_UNIQUE_PEPTIDE_COUNT, COL_NAME_NON_UNIQUE_PEPTIDE_COUNT, 90)
        ADONetRoutines.AppendColumnToTableStyle(tsResults, COL_NAME_UNIQUE_PEPTIDE_COUNT, COL_NAME_UNIQUE_PEPTIDE_COUNT, 65)
        ADONetRoutines.AppendColumnToTableStyle(tsResults, COL_NAME_PROTEIN_RESIDUE_COUNT, COL_NAME_PROTEIN_RESIDUE_COUNT, 90)
        ADONetRoutines.AppendColumnToTableStyle(tsResults, COL_NAME_PROTEIN_SEQUENCE, COL_NAME_PROTEIN_SEQUENCE, 0)

        ' Add the DataGridTableStyle to the data grid's TableStyles collection
        With dgResults
            .TableStyles.Clear()

            If Not .TableStyles.Contains(tsResults) Then
                .TableStyles.Add(tsResults)
            End If
            .ReadOnly = True

            .Refresh()
        End With

    End Sub

#Region "Command Handlers"

    Private Sub chkSearchAllProteinsForPeptideSequence_CheckedChanged(sender As Object, e As EventArgs) Handles chkSearchAllProteinsForPeptideSequence.CheckedChanged
        EnableDisableControls()
    End Sub

    Private Sub cmdAbort_Click(sender As Object, e As EventArgs) Handles cmdAbort.Click
        mProteinCoverageSummarizer.AbortProcessing = True
    End Sub

    Private Sub cmdExit_Click(sender As Object, e As EventArgs) Handles cmdExit.Click
        Me.Close()
    End Sub

    Private Sub cmdSelectOutputFolder_Click(sender As Object, e As EventArgs) Handles cmdSelectOutputFolder.Click
        SelectOutputFolder()
    End Sub

    Private Sub cmdPeptideSelectFile_Click(sender As Object, e As EventArgs) Handles cmdPeptideSelectFile.Click
        SelectPeptideInputFile()
    End Sub

    Private Sub cmdProteinSelectFile_Click(sender As Object, e As EventArgs) Handles cmdProteinSelectFile.Click
        SelectProteinInputFile()
    End Sub

    Private Sub cmdStart_Click(sender As Object, e As EventArgs) Handles cmdStart.Click
        If ConfirmInputFilePaths() Then
            Start()
        End If
    End Sub

    Private Sub chkAddSpace_CheckStateChanged(sender As Object, e As EventArgs) Handles chkAddSpace.CheckedChanged
        ShowRichTextStart()
    End Sub

    Private Sub cboCharactersPerLine_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboCharactersPerLine.SelectedIndexChanged
        ShowRichTextStart()
    End Sub

    Private Sub dgResults_CurrentCellChanged(sender As Object, e As EventArgs) Handles dgResults.CurrentCellChanged
        ShowRichTextStart(eSequenceDisplayConstants.UseDataGrid)
    End Sub

#End Region

#Region "Textbox handlers"
    Private Sub txtCoverage_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtCoverage.KeyPress
        VBNetRoutines.TextBoxKeyPressHandler(txtCoverage, e, False, False, False, False, False, False, False, False, False, False, True)
    End Sub

    Private Sub txtCustomProteinSequence_Click(sender As Object, e As EventArgs) Handles txtCustomProteinSequence.Click
        If txtCustomProteinSequence.TextLength > 0 Then ShowRichTextStart(eSequenceDisplayConstants.UseCustom)
    End Sub

    Private Sub txtCustomProteinSequence_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtCustomProteinSequence.KeyPress
        VBNetRoutines.TextBoxKeyPressHandler(txtCustomProteinSequence, e, False, False, False, True, False, False, False, False, True, True, True)
    End Sub

    Private Sub txtCustomProteinSequence_TextChanged(sender As Object, e As EventArgs) Handles txtCustomProteinSequence.TextChanged
        ShowRichTextStart(eSequenceDisplayConstants.UseCustom)
    End Sub

    Private Sub txtOutputFolderPath_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtOutputFolderPath.KeyPress
        VBNetRoutines.TextBoxKeyPressHandlerCheckControlChars(txtOutputFolderPath, e)
    End Sub

    Private Sub txtPeptideInputFilePath_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtPeptideInputFilePath.KeyPress
        VBNetRoutines.TextBoxKeyPressHandlerCheckControlChars(txtPeptideInputFilePath, e)
    End Sub

    Private Sub txtPeptideInputFilePath_TextChanged(sender As Object, e As EventArgs) Handles txtPeptideInputFilePath.TextChanged
        ' Auto-define the output file path
        DefineOutputFolderPath(txtPeptideInputFilePath.Text)
    End Sub

    Private Sub txtProteinInputFilePath_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtProteinInputFilePath.KeyPress
        VBNetRoutines.TextBoxKeyPressHandlerCheckControlChars(txtProteinInputFilePath, e)
    End Sub

    Private Sub txtProteinInputFilePath_TextChanged(sender As Object, e As EventArgs) Handles txtProteinInputFilePath.TextChanged
        EnableDisableControls()
    End Sub

#End Region

#Region "Menu Handlers"
    Private Sub mnuFileExit_Click(sender As Object, e As EventArgs) Handles mnuFileExit.Click
        CloseProgram()
    End Sub

    Private Sub mnuFileSelectInputFile_Click(sender As Object, e As EventArgs) Handles mnuFileSelectInputFile.Click
        SelectProteinInputFile()
    End Sub

    Private Sub mnuFileLoadOptions_Click(sender As Object, e As EventArgs) Handles mnuFileLoadOptions.Click
        IniFileLoadOptions(False)
    End Sub

    Private Sub mnuPeptideInputFile_Click(sender As Object, e As EventArgs) Handles mnuPeptideInputFile.Click
        SelectPeptideInputFile()
    End Sub

    Private Sub mnuFileSelectOutputFile_Click(sender As Object, e As EventArgs) Handles mnuFileSelectOutputFile.Click
        SelectPeptideInputFile()
    End Sub

    Private Sub mnuEditShowRTF_Click(sender As Object, e As EventArgs) Handles mnuEditShowRTF.Click
        ToggleRTFCodeVisible()
    End Sub

    Private Sub mnuHelpAbout_Click(sender As Object, e As EventArgs) Handles mnuHelpAbout.Click
        ShowAboutBox()
    End Sub

    Private Sub mnuEditResetOptions_Click(sender As Object, e As EventArgs) Handles mnuEditResetOptions.Click
        ResetToDefaults()
    End Sub

    Private Sub mnuFileSaveDefaultOptions_Click(sender As Object, e As EventArgs) Handles mnuFileSaveDefaultOptions.Click
        IniFileSaveOptions(GetSettingsFilePath(), True)
    End Sub
#End Region

    Private Sub GUI_Closing(sender As Object, e As CancelEventArgs) Handles MyBase.Closing
        IniFileSaveOptions(GetSettingsFilePath(), mSaveFullSettingsFileOnExit)
    End Sub

    Private Sub chkSearchAllProteinsSaveDetails_CheckedChanged(sender As Object, e As EventArgs) Handles chkSaveProteinToPeptideMappingFile.CheckedChanged
        EnableDisableControls()
    End Sub

    Private Sub cboPeptideInputFileColumnOrdering_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboPeptideInputFileColumnOrdering.SelectedIndexChanged
        AutoDefineSearchAllProteins()
    End Sub

    Private Sub ProteinCoverageSummarizer_StatusEvent(message As String)
        Console.WriteLine(message)
        If lblProgress.Text.StartsWith(message) Then
            lblStatus.Text = ""
        Else
            lblStatus.Text = message
        End If

    End Sub

    Private Sub ProteinCoverageSummarizer_WarningEvent(message As String)
        ConsoleMsgUtils.ShowWarning(message)
        lblStatus.Text = message
    End Sub

    Private Sub ProteinCoverageSummarizer_ErrorEvent(message As String, ex As Exception)
        ConsoleMsgUtils.ShowError(message, ex)
        lblStatus.Text = message
    End Sub

    Private Sub ProteinCoverageSummarizer_ProgressChanged(taskDescription As String, percentComplete As Single)
        lblProgress.Text = taskDescription
        If percentComplete > 0 Then lblProgress.Text &= ControlChars.NewLine & percentComplete.ToString("0.0") & "% complete"

        Application.DoEvents()
    End Sub

    Private Sub ProteinCoverageSummarizer_ProgressReset()
        lblProgress.Text = mProteinCoverageSummarizer.ProgressStepDescription
        Application.DoEvents()
    End Sub

End Class
