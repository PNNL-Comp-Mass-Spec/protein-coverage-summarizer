// -------------------------------------------------------------------------------
// Written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA)
// Program started June 14, 2005
//
// E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov
// Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics
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
using PRISMDatabaseUtils;
using PRISMWin;
using ProteinCoverageSummarizer;
using ProteinFileReader;

namespace ProteinCoverageSummarizerGUI
{
    /// <summary>
    /// This program uses clsProteinCoverageSummarizer to read in a file with protein sequences along with
    /// an accompanying file with peptide sequences and compute the percent coverage of each of the proteins
    /// </summary>
    public partial class GUI : Form
    {
        // Ignore Spelling: CrLf, chk, ini, Nikša

        /// <summary>
        /// Graphical user interface
        /// </summary>
        public GUI()
        {
            Closing += GUI_Closing;

            // This call is required by the Windows Form Designer.
            InitializeComponent();

            // Add any initialization after the InitializeComponent() call
            InitializeControls();
        }

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

        private enum SequenceDisplayConstants
        {
            UsePrevious = 0,
            UseDataGrid = 1,
            UseCustom = 2
        }

        #endregion

        #region "Class wide variables"

        private DataSet mDSCoverageResults;
        private int mProteinSequenceColIndex;
        private bool mProteinDescriptionColVisible;

        private clsProteinCoverageSummarizerRunner mProteinCoverageSummarizer;

        private string mXmlSettingsFilePath;
        private bool mSaveFullSettingsFileOnExit;
        private string mLastFolderUsed;

        #endregion

        #region "Properties"

        /// <summary>
        /// When true, do not delete the .SQLite database
        /// </summary>
        public bool KeepDB { get; set; }

        #endregion

        private void CloseProgram()
        {
            Close();
        }

        private void AutoDefineSearchAllProteins()
        {
            if (cboPeptideInputFileColumnOrdering.SelectedIndex == (int)ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.SequenceOnly)
            {
                chkSearchAllProteinsForPeptideSequence.Checked = true;
            }
        }

        private bool ConfirmInputFilePaths()
        {
            if (txtProteinInputFilePath.Text.Length == 0 && txtPeptideInputFilePath.Text.Length == 0)
            {
                ShowErrorMessage("Please define the input file paths", "Missing Value");
                txtProteinInputFilePath.Focus();
                return false;
            }

            if (txtProteinInputFilePath.Text.Length == 0)
            {
                ShowErrorMessage("Please define Protein input file path", "Missing Value");
                txtProteinInputFilePath.Focus();
                return false;
            }

            if (txtPeptideInputFilePath.Text.Length == 0)
            {
                ShowErrorMessage("Please define Peptide input file path", "Missing Value");
                txtPeptideInputFilePath.Focus();
                return false;
            }

            try
            {
                if (!File.Exists(txtProteinInputFilePath.Text))
                {
                    ShowErrorMessage("Protein input file path not found: " + txtProteinInputFilePath.Text, "Missing File");
                    txtProteinInputFilePath.Focus();
                    return false;
                }

                if (!File.Exists(txtPeptideInputFilePath.Text))
                {
                    ShowErrorMessage("Peptide input file path not found: " + txtPeptideInputFilePath.Text, "Missing File");
                    txtPeptideInputFilePath.Focus();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error confirming that the input files exist: " + ex.Message);
                txtPeptideInputFilePath.Focus();
                return false;
            }

            return true;
        }

        private void CreateSummaryDataTable(string resultsFilePath)
        {
            long bytesRead = 0;

            try
            {
                if (string.IsNullOrWhiteSpace(resultsFilePath))
                {
                    // Output file not available
                    return;
                }

                if (!File.Exists(resultsFilePath))
                {
                    ShowErrorMessage("Results file not found: " + resultsFilePath);
                }

                // Clear the data source to prevent the data grid from updating
                dgResults.DataSource = null;

                // Clear the dataset
                mDSCoverageResults.Tables[COVERAGE_RESULTS_DATA_TABLE].Clear();

                var linesRead = 0;
                var proteinDescriptionPresent = false;

                // Open the file and read in the lines
                using (var reader = new StreamReader(new FileStream(resultsFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();
                        if (string.IsNullOrEmpty(dataLine))
                            continue;

                        bytesRead += dataLine.Length + 2;           // Add 2 for CrLf

                        if (string.IsNullOrWhiteSpace(dataLine))
                            continue;

                        linesRead++;
                        if (linesRead == 1)
                        {
                            // Skip the first line (column headers)
                            continue;
                        }

                        var lineParts = dataLine.Split('\t');

                        var newRow = mDSCoverageResults.Tables[COVERAGE_RESULTS_DATA_TABLE].NewRow();
                        int index;
                        for (index = 0; index <= lineParts.Length - 1; index++)
                        {
                            if (index > clsProteinCoverageSummarizer.OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER - 1)
                                break;

                            try
                            {
                                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                                switch (Type.GetTypeCode(newRow[index].GetType()))
                                {
                                    case TypeCode.String:
                                        newRow[index] = lineParts[index];
                                        break;
                                    case TypeCode.Double:
                                        newRow[index] = Convert.ToDouble(lineParts[index]);
                                        break;
                                    case TypeCode.Single:
                                        newRow[index] = Convert.ToSingle(lineParts[index]);
                                        break;
                                    case TypeCode.Byte:
                                    case TypeCode.Int16:
                                    case TypeCode.Int32:
                                    case TypeCode.Int64:
                                    case TypeCode.UInt16:
                                    case TypeCode.UInt32:
                                    case TypeCode.UInt64:
                                        newRow[index] = Convert.ToInt32(lineParts[index]);
                                        break;
                                    case TypeCode.Boolean:
                                        newRow[index] = Convert.ToBoolean(lineParts[index]);
                                        break;
                                    default:
                                        newRow[index] = lineParts[index];
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Ignore errors while populating the table
                                ConsoleMsgUtils.ShowDebug("Error reading line {0}: {1}", linesRead, ex.Message);
                            }
                        }

                        if (lineParts.Length >= clsProteinCoverageSummarizer.OUTPUT_FILE_PROTEIN_DESCRIPTION_COLUMN_NUMBER &&
                            lineParts[clsProteinCoverageSummarizer.OUTPUT_FILE_PROTEIN_DESCRIPTION_COLUMN_NUMBER - 1].Length > 0)
                        {
                            proteinDescriptionPresent = true;
                        }

                        // Add the row to the coverage table.
                        mDSCoverageResults.Tables[COVERAGE_RESULTS_DATA_TABLE].Rows.Add(newRow);

                        if (linesRead % 25 == 1)
                        {
                            lblProgress.Text = "Loading results: " + (bytesRead / (double)reader.BaseStream.Length * 100).ToString("0.0") + "% complete";
                        }
                    }
                }

                // Re-define the data source
                // Bind the DataSet to the DataGrid
                dgResults.DataSource = mDSCoverageResults;
                dgResults.DataMember = COVERAGE_RESULTS_DATA_TABLE;

                if (proteinDescriptionPresent != mProteinDescriptionColVisible)
                {
                    mProteinDescriptionColVisible = proteinDescriptionPresent;
                    UpdateDataGridTableStyle();
                }

                // Display the sequence for the first protein
                if (mDSCoverageResults.Tables[COVERAGE_RESULTS_DATA_TABLE].Rows.Count > 0)
                {
                    dgResults.CurrentRowIndex = 0;
                    ShowRichTextStart(SequenceDisplayConstants.UseDataGrid);
                }
                else
                {
                    ShowRichText("", rtfRichTextBox);
                }

                lblProgress.Text = "Results loaded";
            }
            catch (Exception)
            {
                // Ignore errors here
            }
        }

        private void DefineOutputFolderPath(string peptideInputFilePath)
        {
            try
            {
                if (peptideInputFilePath.Length > 0)
                {
                    txtOutputFolderPath.Text = Path.GetDirectoryName(peptideInputFilePath);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error defining default output folder path: " + ex.Message);
            }
        }

        private void EnableDisableControls()
        {
            var fastaFile = clsProteinFileDataCache.IsFastaFile(txtProteinInputFilePath.Text);

            cboProteinInputFileColumnOrdering.Enabled = !fastaFile;
            cboProteinInputFileColumnDelimiter.Enabled = !fastaFile;
            txtProteinInputFileColumnDelimiter.Enabled = !fastaFile;

            chkSearchAllProteinsSkipCoverageComputationSteps.Enabled = chkSearchAllProteinsForPeptideSequence.Checked;
        }

        private string GetSettingsFilePath()
        {
            return ProcessFilesOrDirectoriesBase.GetSettingsFilePathLocal("ProteinCoverageSummarizer", XML_SETTINGS_FILE_NAME);
        }

        private void IniFileLoadOptions(bool updateIOPaths)
        {
            // Prompts the user to select a file to load the options from

            var fileDialog = new OpenFileDialog();

            var filePath = mXmlSettingsFilePath;

            fileDialog.AddExtension = true;
            fileDialog.CheckFileExists = true;
            fileDialog.CheckPathExists = true;
            fileDialog.DefaultExt = ".xml";
            fileDialog.DereferenceLinks = true;
            fileDialog.Multiselect = false;
            fileDialog.ValidateNames = true;

            fileDialog.Filter = "Settings files (*.xml)|*.xml|All files (*.*)|*.*";

            fileDialog.FilterIndex = 1;

            if (filePath.Length > 0)
            {
                try
                {
                    var parentDirectory = Directory.GetParent(filePath)?.FullName;

                    fileDialog.InitialDirectory = string.IsNullOrEmpty(parentDirectory)
                        ? ProcessFilesOrDirectoriesBase.GetAppDirectoryPath()
                        : parentDirectory;
                }
                catch
                {
                    fileDialog.InitialDirectory = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
                }
            }
            else
            {
                fileDialog.InitialDirectory = ProcessFilesOrDirectoriesBase.GetAppDirectoryPath();
            }

            if (File.Exists(filePath))
            {
                fileDialog.FileName = Path.GetFileName(filePath);
            }

            fileDialog.Title = "Specify file to load options from";

            fileDialog.ShowDialog();
            if (fileDialog.FileName.Length > 0)
            {
                mXmlSettingsFilePath = fileDialog.FileName;

                IniFileLoadOptions(mXmlSettingsFilePath, updateIOPaths);
            }
        }

        private void IniFileLoadOptions(string filePath, bool updateIOPaths)
        {
            var proteinCoverageSummarizer = new clsProteinCoverageSummarizerRunner();

            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    // No parameter file specified; nothing to load
                    return;
                }

                if (!File.Exists(filePath))
                {
                    ShowErrorMessage("Parameter file not Found: " + filePath);
                    return;
                }

                var settingsFile = new XmlSettingsFileAccessor();

                if (settingsFile.LoadSettings(filePath))
                {
                    // Read the GUI-specific options from the XML file
                    if (!settingsFile.SectionPresent(XML_SECTION_GUI_OPTIONS))
                    {
                        ShowErrorMessage("The node '<section name=\"" + XML_SECTION_GUI_OPTIONS + "\"> was not found in the parameter file: " + filePath, "Invalid File");
                        mSaveFullSettingsFileOnExit = true;
                    }
                    else
                    {
                        if (updateIOPaths)
                        {
                            txtProteinInputFilePath.Text = settingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFilePath", txtProteinInputFilePath.Text);
                            txtPeptideInputFilePath.Text = settingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFilePath", txtPeptideInputFilePath.Text);
                            txtOutputFolderPath.Text = settingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "OutputFolderPath", txtOutputFolderPath.Text);
                        }

                        cboProteinInputFileColumnDelimiter.SelectedIndex = settingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFileColumnDelimiterIndex", cboProteinInputFileColumnDelimiter.SelectedIndex);
                        txtProteinInputFileColumnDelimiter.Text = settingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFileColumnDelimiter", txtProteinInputFileColumnDelimiter.Text);

                        cboPeptideInputFileColumnDelimiter.SelectedIndex = settingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFileColumnDelimiterIndex", cboPeptideInputFileColumnDelimiter.SelectedIndex);
                        txtPeptideInputFileColumnDelimiter.Text = settingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFileColumnDelimiter", txtPeptideInputFileColumnDelimiter.Text);

                        cboCharactersPerLine.SelectedIndex = settingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "ProteinSequenceCharactersPerLine", cboCharactersPerLine.SelectedIndex);
                        chkAddSpace.Checked = settingsFile.GetParam(XML_SECTION_GUI_OPTIONS, "ProteinSequenceAddSpace", chkAddSpace.Checked);

                        if (!settingsFile.SectionPresent(clsProteinCoverageSummarizer.XML_SECTION_PROCESSING_OPTIONS))
                        {
                            ShowErrorMessage("The node '<section name=\"" + clsProteinCoverageSummarizer.XML_SECTION_PROCESSING_OPTIONS + "\"> was not found in the parameter file: ", "Invalid File");
                            mSaveFullSettingsFileOnExit = true;
                        }
                        else
                        {
                            DelimitedProteinFileReader.ProteinFileFormatCode proteinInputFileColumnOrder;
                            try
                            {
                                proteinInputFileColumnOrder = (DelimitedProteinFileReader.ProteinFileFormatCode)settingsFile
                                    .GetParam(clsProteinCoverageSummarizer.XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileFormatCode", cboProteinInputFileColumnOrdering.SelectedIndex + PROTEIN_INPUT_FILE_INDEX_OFFSET);
                            }
                            catch (Exception)
                            {
                                proteinInputFileColumnOrder = DelimitedProteinFileReader.ProteinFileFormatCode.ProteinName_Sequence;
                            }

                            try
                            {
                                cboProteinInputFileColumnOrdering.SelectedIndex = (int)proteinInputFileColumnOrder - PROTEIN_INPUT_FILE_INDEX_OFFSET;
                            }
                            catch (Exception)
                            {
                                if (cboProteinInputFileColumnOrdering.Items.Count > 0)
                                {
                                    cboProteinInputFileColumnOrdering.SelectedIndex = 0;
                                }
                            }

                            ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode peptideInputFileColumnOrder;
                            try
                            {
                                peptideInputFileColumnOrder = (ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode)settingsFile
                                    .GetParam(clsProteinCoverageSummarizer.XML_SECTION_PROCESSING_OPTIONS, "PeptideFileFormatCode", cboPeptideInputFileColumnOrdering.SelectedIndex);
                            }
                            catch (Exception)
                            {
                                peptideInputFileColumnOrder = ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.UseHeaderNames;
                            }

                            try
                            {
                                cboPeptideInputFileColumnOrdering.SelectedIndex = (int)peptideInputFileColumnOrder;
                            }
                            catch (Exception)
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
                proteinCoverageSummarizer.LoadParameterFileSettings(filePath);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error calling LoadParameterFileSettings: " + ex);
            }

            try
            {
                LoadProcessingClassOptions(ref proteinCoverageSummarizer);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error calling LoadProcessingClassOptions: " + ex);
            }
        }

        private void IniFileSaveOptions(string settingsFilePath, bool saveExtendedOptions = false)
        {
            var settingsFile = new XmlSettingsFileAccessor();

            const string XML_SECTION_PROCESSING_OPTIONS = "ProcessingOptions";

            try
            {
                var settingsFileInfo = new FileInfo(settingsFilePath);
                if (!settingsFileInfo.Exists)
                {
                    saveExtendedOptions = true;
                }
            }
            catch
            {
                // Ignore errors here
            }

            // Pass True to .LoadSettings() to turn off case sensitive matching
            try
            {
                settingsFile.LoadSettings(settingsFilePath, true);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                settingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFilePath", txtProteinInputFilePath.Text);
                settingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFilePath", txtPeptideInputFilePath.Text);
                settingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "OutputFolderPath", txtOutputFolderPath.Text);

                if (saveExtendedOptions)
                {
                    settingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFileColumnDelimiterIndex", cboProteinInputFileColumnDelimiter.SelectedIndex);
                    settingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "ProteinInputFileColumnDelimiter", txtProteinInputFileColumnDelimiter.Text);

                    settingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFileColumnDelimiterIndex", cboPeptideInputFileColumnDelimiter.SelectedIndex);
                    settingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "PeptideInputFileColumnDelimiter", txtPeptideInputFileColumnDelimiter.Text);

                    settingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "ProteinSequenceCharactersPerLine", cboCharactersPerLine.SelectedIndex);
                    settingsFile.SetParam(XML_SECTION_GUI_OPTIONS, "ProteinSequenceAddSpace", chkAddSpace.Checked);

                    settingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "OutputProteinSequence", chkOutputProteinSequence.Checked);
                    settingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "SearchAllProteinsForPeptideSequence", chkSearchAllProteinsForPeptideSequence.Checked);

                    settingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "SaveProteinToPeptideMappingFile", chkSaveProteinToPeptideMappingFile.Checked);
                    settingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "SaveSourceDataPlusProteinsFile", chkSaveSourceDataPlusProteinsFile.Checked);
                    settingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "SearchAllProteinsSkipCoverageComputationSteps", chkSearchAllProteinsSkipCoverageComputationSteps.Checked);

                    settingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "TrackPeptideCounts", chkTrackPeptideCounts.Checked);
                    settingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "RemoveSymbolCharacters", chkRemoveSymbolCharacters.Checked);
                    settingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "MatchPeptidePrefixAndSuffixToProtein", chkMatchPeptidePrefixAndSuffixToProtein.Checked);
                    settingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "IgnoreILDifferences", chkIgnoreILDifferences.Checked);

                    settingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideInputFileDelimiter", LookupColumnDelimiter(cboPeptideInputFileColumnDelimiter, txtPeptideInputFileColumnDelimiter, '\t'));
                    settingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideFileFormatCode", cboPeptideInputFileColumnOrdering.SelectedIndex);
                    settingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideFileSkipFirstLine", chkPeptideFileSkipFirstLine.Checked);

                    settingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileDelimiter", LookupColumnDelimiter(cboPeptideInputFileColumnDelimiter, txtPeptideInputFileColumnDelimiter, '\t'));
                    settingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileFormatCode", cboProteinInputFileColumnOrdering.SelectedIndex + PROTEIN_INPUT_FILE_INDEX_OFFSET);
                    settingsFile.SetParam(XML_SECTION_PROCESSING_OPTIONS, "ProteinFileSkipFirstLine", chkProteinFileSkipFirstLine.Checked);
                }

                settingsFile.SaveSettings();
            }
            catch (Exception)
            {
                ShowErrorMessage("Error storing parameter in settings file: " + Path.GetFileName(settingsFilePath));
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

            SetToolTips();

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
                var coverageResults = new DataTable(COVERAGE_RESULTS_DATA_TABLE);

                // Add the columns to the data table
                DataTableUtils.AppendColumnStringToTable(coverageResults, COL_NAME_PROTEIN_NAME, string.Empty);
                DataTableUtils.AppendColumnFloatToTable(coverageResults, COL_NAME_PROTEIN_COVERAGE);
                DataTableUtils.AppendColumnStringToTable(coverageResults, COL_NAME_PROTEIN_DESCRIPTION, string.Empty);
                DataTableUtils.AppendColumnIntegerToTable(coverageResults, COL_NAME_NON_UNIQUE_PEPTIDE_COUNT);
                DataTableUtils.AppendColumnIntegerToTable(coverageResults, COL_NAME_UNIQUE_PEPTIDE_COUNT);
                DataTableUtils.AppendColumnIntegerToTable(coverageResults, COL_NAME_PROTEIN_RESIDUE_COUNT);
                DataTableUtils.AppendColumnStringToTable(coverageResults, COL_NAME_PROTEIN_SEQUENCE, string.Empty);

                // Note that Protein Sequence should be at ColIndex 6 = clsProteinCoverageSummarizer.OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER-1
                mProteinSequenceColIndex = clsProteinCoverageSummarizer.OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER - 1;

                // Could define a primary key if we wanted
                // coverageResults.PrimaryKey = coverageResults.Columns(COL_NAME_PROTEIN_NAME);

                // Instantiate the dataset
                mDSCoverageResults = new DataSet(COVERAGE_RESULTS_DATA_TABLE);

                // Add the new DataTable to the DataSet.
                mDSCoverageResults.Tables.Add(coverageResults);

                // Bind the DataSet to the DataGrid
                dgResults.DataSource = mDSCoverageResults;
                dgResults.DataMember = COVERAGE_RESULTS_DATA_TABLE;

                mProteinDescriptionColVisible = false;

                // Update the grid's table style
                UpdateDataGridTableStyle();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error in InitializeDataGrid: " + ex);
            }
        }

        private void LoadProcessingClassOptions(ref clsProteinCoverageSummarizerRunner proteinCoverageSummarizer)
        {
            try
            {
                chkPeptideFileSkipFirstLine.Checked = proteinCoverageSummarizer.Options.PeptideFileSkipFirstLine;
                chkProteinFileSkipFirstLine.Checked = proteinCoverageSummarizer.Options.ProteinDataOptions.DelimitedFileSkipFirstLine;

                chkOutputProteinSequence.Checked = proteinCoverageSummarizer.Options.OutputProteinSequence;
                chkSearchAllProteinsForPeptideSequence.Checked = proteinCoverageSummarizer.Options.SearchAllProteinsForPeptideSequence;

                chkSaveProteinToPeptideMappingFile.Checked = proteinCoverageSummarizer.Options.SaveProteinToPeptideMappingFile;
                chkSaveSourceDataPlusProteinsFile.Checked = proteinCoverageSummarizer.Options.SaveSourceDataPlusProteinsFile;
                chkSearchAllProteinsSkipCoverageComputationSteps.Checked = proteinCoverageSummarizer.Options.SearchAllProteinsSkipCoverageComputationSteps;

                chkTrackPeptideCounts.Checked = proteinCoverageSummarizer.Options.TrackPeptideCounts;
                chkRemoveSymbolCharacters.Checked = proteinCoverageSummarizer.Options.RemoveSymbolCharacters;
                chkMatchPeptidePrefixAndSuffixToProtein.Checked = proteinCoverageSummarizer.Options.MatchPeptidePrefixAndSuffixToProtein;
                chkIgnoreILDifferences.Checked = proteinCoverageSummarizer.Options.IgnoreILDifferences;
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
            catch (Exception)
            {
                return '\t';
            }
        }

        private char LookupColumnDelimiterChar(int delimiterIndex, string customDelimiter, char defaultDelimiter)
        {
            var delimiter = delimiterIndex switch
            {
                (int)DelimiterCharConstants.Space => " ",
                (int)DelimiterCharConstants.Tab => Convert.ToString('\t'),
                (int)DelimiterCharConstants.Comma => ",",
                _ => string.Copy(customDelimiter)       // Includes DelimiterCharConstants.Other
            };
            if (string.IsNullOrEmpty(delimiter))
            {
                delimiter = string.Copy(Convert.ToString(defaultDelimiter));
            }

            try
            {
                return delimiter[0];
            }
            catch (Exception)
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
            // Note: Skipping ProteinFileReader.(int)DelimitedProteinFileReader.ProteinFileFormatCode.SequenceOnly since a Protein Sequence Only file is inappropriate for this program
            cboProteinInputFileColumnOrdering.Items.Insert((int)DelimitedProteinFileReader.ProteinFileFormatCode.ProteinName_Sequence - PROTEIN_INPUT_FILE_INDEX_OFFSET, "ProteinName and Sequence");
            cboProteinInputFileColumnOrdering.Items.Insert((int)DelimitedProteinFileReader.ProteinFileFormatCode.ProteinName_Description_Sequence - PROTEIN_INPUT_FILE_INDEX_OFFSET, "ProteinName, Description, Seq");
            cboProteinInputFileColumnOrdering.Items.Insert((int)DelimitedProteinFileReader.ProteinFileFormatCode.UniqueID_Sequence - PROTEIN_INPUT_FILE_INDEX_OFFSET, "UniqueID and Seq");
            cboProteinInputFileColumnOrdering.Items.Insert((int)DelimitedProteinFileReader.ProteinFileFormatCode.ProteinName_PeptideSequence_UniqueID - PROTEIN_INPUT_FILE_INDEX_OFFSET, "ProteinName, Seq, UniqueID");
            cboProteinInputFileColumnOrdering.Items.Insert((int)DelimitedProteinFileReader.ProteinFileFormatCode.ProteinName_PeptideSequence_UniqueID_Mass_NET - PROTEIN_INPUT_FILE_INDEX_OFFSET, "ProteinName, Seq, UniqueID, Mass, Time");
            cboProteinInputFileColumnOrdering.Items.Insert((int)DelimitedProteinFileReader.ProteinFileFormatCode.ProteinName_PeptideSequence_UniqueID_Mass_NET_NETStDev_DiscriminantScore - PROTEIN_INPUT_FILE_INDEX_OFFSET, "ProteinName, Seq, UniqueID, Mass, Time, TimeStDev, DiscriminantScore");
            cboProteinInputFileColumnOrdering.Items.Insert((int)DelimitedProteinFileReader.ProteinFileFormatCode.UniqueID_Sequence_Mass_NET - PROTEIN_INPUT_FILE_INDEX_OFFSET, "UniqueID, Seq, Mass, Time");

            cboProteinInputFileColumnOrdering.SelectedIndex = (int)DelimitedProteinFileReader.ProteinFileFormatCode.ProteinName_Description_Sequence - PROTEIN_INPUT_FILE_INDEX_OFFSET;

            cboPeptideInputFileColumnOrdering.Items.Clear();
            cboPeptideInputFileColumnOrdering.Items.Insert((int)ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.SequenceOnly, "Sequence Only");
            cboPeptideInputFileColumnOrdering.Items.Insert((int)ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.ProteinName_PeptideSequence, "ProteinName and Sequence");
            cboPeptideInputFileColumnOrdering.Items.Insert((int)ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.UseHeaderNames, "Look for headers Peptide and Protein");

            cboPeptideInputFileColumnOrdering.SelectedIndex = (int)ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.UseHeaderNames;
        }

        private void ResetToDefaults()
        {
            cboProteinInputFileColumnOrdering.SelectedIndex = (int)DelimitedProteinFileReader.ProteinFileFormatCode.ProteinName_Sequence - PROTEIN_INPUT_FILE_INDEX_OFFSET;
            cboProteinInputFileColumnDelimiter.SelectedIndex = 1;
            txtProteinInputFileColumnDelimiter.Text = ";";
            chkProteinFileSkipFirstLine.Checked = false;

            cboPeptideInputFileColumnOrdering.SelectedIndex = (int)ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode.UseHeaderNames;
            cboPeptideInputFileColumnDelimiter.SelectedIndex = 1;
            txtPeptideInputFileColumnDelimiter.Text = ";";
            chkPeptideFileSkipFirstLine.Checked = false;

            chkOutputProteinSequence.Checked = true;
            chkSearchAllProteinsForPeptideSequence.Checked = false;

            chkSaveProteinToPeptideMappingFile.Checked = true;
            chkSaveSourceDataPlusProteinsFile.Checked = false;
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
            var dlgOpenFileDialog = new OpenFileDialog
            {
                Filter = "Fasta Files (*.fasta)|*.fasta|Text Files(*.txt)|*.txt|All Files (*.*)|*.*",
                FilterIndex = 3
            };

            var result = dlgOpenFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtProteinInputFilePath.Text = dlgOpenFileDialog.FileName;
                mLastFolderUsed = Path.GetDirectoryName(dlgOpenFileDialog.FileName);
            }
        }

        private void SelectPeptideInputFile()
        {
            var dlgOpenFileDialog = new OpenFileDialog
            {
                InitialDirectory = mLastFolderUsed,
                Filter = "Text Files(*.txt)|*.txt|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            var result = dlgOpenFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtPeptideInputFilePath.Text = dlgOpenFileDialog.FileName;
                mLastFolderUsed = Path.GetDirectoryName(dlgOpenFileDialog.FileName);
            }
        }

        private bool SetOptionsFromGUI(clsProteinCoverageSummarizerRunner proteinCoverageSummarizer)
        {
            try
            {
                proteinCoverageSummarizer.Options.ProteinInputFilePath = txtProteinInputFilePath.Text;

                proteinCoverageSummarizer.Options.ProteinDataOptions.DelimitedFileFormatCode = (DelimitedProteinFileReader.ProteinFileFormatCode)(cboProteinInputFileColumnOrdering.SelectedIndex + PROTEIN_INPUT_FILE_INDEX_OFFSET);
                proteinCoverageSummarizer.Options.ProteinDataOptions.DelimitedInputFileDelimiter = LookupColumnDelimiter(cboProteinInputFileColumnDelimiter, txtProteinInputFileColumnDelimiter, '\t');
                proteinCoverageSummarizer.Options.ProteinDataOptions.DelimitedFileSkipFirstLine = chkProteinFileSkipFirstLine.Checked;
                proteinCoverageSummarizer.Options.ProteinDataOptions.RemoveSymbolCharacters = chkRemoveSymbolCharacters.Checked;
                proteinCoverageSummarizer.Options.ProteinDataOptions.IgnoreILDifferences = chkIgnoreILDifferences.Checked;

                // peptide file options
                proteinCoverageSummarizer.Options.PeptideFileFormatCode = (ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode)Convert.ToInt32(cboPeptideInputFileColumnOrdering.SelectedIndex);
                proteinCoverageSummarizer.Options.PeptideInputFileDelimiter = LookupColumnDelimiter(cboPeptideInputFileColumnDelimiter, txtPeptideInputFileColumnDelimiter, '\t');
                proteinCoverageSummarizer.Options.PeptideFileSkipFirstLine = chkPeptideFileSkipFirstLine.Checked;

                // processing options
                proteinCoverageSummarizer.Options.OutputProteinSequence = chkOutputProteinSequence.Checked;
                proteinCoverageSummarizer.Options.SearchAllProteinsForPeptideSequence = chkSearchAllProteinsForPeptideSequence.Checked;

                proteinCoverageSummarizer.Options.SaveProteinToPeptideMappingFile = chkSaveProteinToPeptideMappingFile.Checked;
                proteinCoverageSummarizer.Options.SaveSourceDataPlusProteinsFile = chkSaveSourceDataPlusProteinsFile.Checked;

                if (chkSaveProteinToPeptideMappingFile.Checked)
                {
                    proteinCoverageSummarizer.Options.SearchAllProteinsSkipCoverageComputationSteps = chkSearchAllProteinsSkipCoverageComputationSteps.Checked;
                }
                else
                {
                    proteinCoverageSummarizer.Options.SearchAllProteinsSkipCoverageComputationSteps = false;
                }

                proteinCoverageSummarizer.Options.TrackPeptideCounts = chkTrackPeptideCounts.Checked;
                proteinCoverageSummarizer.Options.RemoveSymbolCharacters = chkRemoveSymbolCharacters.Checked;
                proteinCoverageSummarizer.Options.MatchPeptidePrefixAndSuffixToProtein = chkMatchPeptidePrefixAndSuffixToProtein.Checked;
                proteinCoverageSummarizer.Options.IgnoreILDifferences = chkIgnoreILDifferences.Checked;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void SetToolTips()
        {
            var toolTipControl = new ToolTip();

            toolTipControl.SetToolTip(chkSaveProteinToPeptideMappingFile,
                "The filename is auto-defined as the input file name, but with suffix _ProteinToPeptideMapping.txt");

            toolTipControl.SetToolTip(chkSaveSourceDataPlusProteinsFile,
                "The filename is auto-defined as the input file name, but with suffix _AllProteins.txt");
        }

        private void ShowAboutBox()
        {
            var message = new StringBuilder();

            message.AppendLine("This program reads in a .fasta or .txt file containing protein names and sequences (and optionally descriptions).");
            message.AppendLine("The program also reads in a .txt file containing peptide sequences and protein names (though protein name is optional) then uses this information to compute the sequence coverage percent for each protein.");
            message.AppendLine();
            message.AppendLine("Program written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA)");
            message.AppendLine();
            message.AppendFormat("This is version {0} ({1})\n", Application.ProductVersion, Program.PROGRAM_DATE);
            message.AppendLine();
            message.AppendLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
            message.AppendLine("Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics");
            message.AppendLine();
            message.AppendLine("Licensed under the 2-Clause BSD License; https://opensource.org/licenses/BSD-2-Clause");
            message.AppendLine("Copyright 2018 Battelle Memorial Institute");

            MessageBox.Show(message.ToString(), "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool lastSequenceWasDataGrid;

        private void ShowRichTextStart(SequenceDisplayConstants sequenceDisplayMode = SequenceDisplayConstants.UsePrevious)
        {
            var useDataGrid = sequenceDisplayMode switch
            {
                SequenceDisplayConstants.UseDataGrid => true,
                SequenceDisplayConstants.UseCustom => false,
                _ => lastSequenceWasDataGrid             // Includes Use Previous
            };

            lastSequenceWasDataGrid = useDataGrid;
            if (useDataGrid)
            {
                try
                {
                    if (dgResults.CurrentRowIndex >= 0)
                    {
                        if (dgResults[dgResults.CurrentRowIndex, mProteinSequenceColIndex] != null)
                        {
                            ShowRichText(Convert.ToString(dgResults[dgResults.CurrentRowIndex, mProteinSequenceColIndex]), rtfRichTextBox);
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore errors here
                }
            }
            else
            {
                ShowRichText(txtCustomProteinSequence.Text, rtfRichTextBox);
            }
        }

        private void ShowErrorMessage(string message, string caption = "Error")
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void ShowRichText(string sequenceToShow, RichTextBox targetRichTextBox)
        {
            // Define a RegEx to remove whitespace characters
            var reReplaceSymbols = new Regex(@"[ \t\r\n]", RegexOptions.Compiled);

            try
            {
                // Lookup the number of characters per line

                var modValue = cboCharactersPerLine.SelectedIndex switch
                {
                    0 => 40,
                    1 => 50,
                    2 => 60,
                    _ => 40
                };

                // Remove any spaces, tabs, CR, or LF characters in sequenceToShow
                sequenceToShow = reReplaceSymbols.Replace(sequenceToShow, string.Empty);

                // Define the base RTF text
                // ReSharper disable StringLiteralTypo

                var rtfText = new StringBuilder();
                rtfText.Append(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Courier New;}}");
                rtfText.Append(@"{\colortbl\red0\green0\blue0;\red255\green0\blue0;}");
                rtfText.Append(@"\viewkind4\uc1\pard\f0\fs20 ");

                // ReSharper restore StringLiteralTypo

                var inUpperRegion = false;
                var characterCount = 0;
                var upperCaseCount = 0;

                int index;
                for (index = 0; index <= sequenceToShow.Length - 1; index++)
                {
                    if (index > 0)
                    {
                        if (index % modValue == 0)
                        {
                            // Add a new line
                            rtfText.Append(@"\par ");
                        }
                        else if (chkAddSpace.Checked && index % 10 == 0)
                        {
                            // Add a space every 10 residues
                            rtfText.Append(" ");
                        }
                    }

                    if (char.IsUpper(sequenceToShow[index]))
                    {
                        characterCount++;
                        upperCaseCount++;
                        if (!inUpperRegion)
                        {
                            rtfText.Append(@"{\cf1 {\b ");
                            inUpperRegion = true;
                        }
                    }
                    else
                    {
                        if (char.IsLower(sequenceToShow[index]))
                        {
                            characterCount++;
                        }

                        if (inUpperRegion)
                        {
                            rtfText.Append("}}");
                            inUpperRegion = false;
                        }
                    }

                    rtfText.Append(sequenceToShow[index]);
                }

                // Add a final paragraph mark
                rtfText.Append(@"\par}");

                targetRichTextBox.Rtf = rtfText.ToString();

                txtRTFCode.Text = targetRichTextBox.Rtf;

                float coveragePercent;
                if (characterCount > 0)
                {
                    coveragePercent = Convert.ToSingle(upperCaseCount / (double)characterCount * 100);
                }
                else
                {
                    coveragePercent = 0;
                }

                txtCoverage.Text = "Coverage: " + Math.Round(coveragePercent, 3) + "% (" + upperCaseCount + " / " + characterCount + ")";
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error in ShowRichText: " + ex.Message);
            }
        }

        private void Start()
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                cmdAbort.Visible = true;
                cmdStart.Visible = false;

                mProteinCoverageSummarizer = new clsProteinCoverageSummarizerRunner
                {
                    CallingAppHandlesEvents = true
                };

                mProteinCoverageSummarizer.Options.KeepDB = KeepDB;
                mProteinCoverageSummarizer.StatusEvent += ProteinCoverageSummarizer_StatusEvent;
                mProteinCoverageSummarizer.ErrorEvent += ProteinCoverageSummarizer_ErrorEvent;
                mProteinCoverageSummarizer.WarningEvent += ProteinCoverageSummarizer_WarningEvent;

                mProteinCoverageSummarizer.ProgressUpdate += ProteinCoverageSummarizer_ProgressChanged;
                mProteinCoverageSummarizer.ProgressReset += ProteinCoverageSummarizer_ProgressReset;

                var success = SetOptionsFromGUI(mProteinCoverageSummarizer);
                if (success)
                {
                    success = mProteinCoverageSummarizer.ProcessFile(txtPeptideInputFilePath.Text, txtOutputFolderPath.Text);

                    if (success &&
                        !(mProteinCoverageSummarizer.Options.SearchAllProteinsForPeptideSequence &&
                          mProteinCoverageSummarizer.Options.SearchAllProteinsSkipCoverageComputationSteps))
                    {
                        CreateSummaryDataTable(mProteinCoverageSummarizer.ResultsFilePath);
                    }

                    if (success && lblStatus.Text.StartsWith("Done (9"))
                    {
                        lblStatus.Text = "Done";
                    }
                    else if (!success)
                    {
                        if (string.IsNullOrWhiteSpace(mProteinCoverageSummarizer.StatusMessage))
                            lblStatus.Text = "Error: " + mProteinCoverageSummarizer.GetErrorMessage();
                        else
                            lblStatus.Text = "Error: " + mProteinCoverageSummarizer.StatusMessage;
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
            // Setting the MappingName of the table style to COVERAGE_RESULTS_DATA_TABLE will cause this style to be used with that table

            var tsResults = new DataGridTableStyle
            {
                MappingName = COVERAGE_RESULTS_DATA_TABLE,
                AllowSorting = true,
                ColumnHeadersVisible = true,
                RowHeadersVisible = true,
                ReadOnly = true
            };

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
            ShowRichTextStart(SequenceDisplayConstants.UseDataGrid);
        }

        #endregion

        #region "TextBox handlers"

        private void txtCoverage_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtCoverage, e, false, false, false, false, false, false, false, false, false, false, true);
        }

        private void txtCustomProteinSequence_Click(object sender, EventArgs e)
        {
            if (txtCustomProteinSequence.TextLength > 0)
                ShowRichTextStart(SequenceDisplayConstants.UseCustom);
        }

        private void txtCustomProteinSequence_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBoxUtils.TextBoxKeyPressHandler(txtCustomProteinSequence, e, false, false, false, true, false, false, false, false, true, true, true);
        }

        private void txtCustomProteinSequence_TextChanged(object sender, EventArgs e)
        {
            ShowRichTextStart(SequenceDisplayConstants.UseCustom);
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
                lblStatus.Text = string.Empty;
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