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
    public partial class GUI : Form
    {
        // Ignore Spelling: Textbox, CrLf, chk, ini, Nikša

        public GUI() : base()
        {
            base.Closing += GUI_Closing;

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

        private enum eSequenceDisplayConstants
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
                ShowErrorMessage("Error confirming that the input files exist: " + ex.Message, "Error");
                txtPeptideInputFilePath.Focus();
                return false;
            }

            return true;
        }

        private void CreateSummaryDataTable(string strResultsFilePath)
        {
            long bytesRead = 0;

            try
            {
                if (string.IsNullOrWhiteSpace(strResultsFilePath))
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
                var srInFile = new StreamReader(strResultsFilePath);
                var intLineCount = 1;
                var blnProteinDescriptionPresent = false;

                while (srInFile.Peek() != -1)
                {
                    var strLineIn = srInFile.ReadLine();
                    if (strLineIn != null)
                        bytesRead += strLineIn.Length + 2;           // Add 2 for CrLf

                    if (intLineCount == 1)
                    {
                        // do nothing, skip the first line
                    }
                    else
                    {
                        var strSplitLine = strLineIn.Split('\t');

                        var objNewRow = mDSCoverageResults.Tables[COVERAGE_RESULTS_DATA_TABLE].NewRow();
                        int intIndex;
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
                            catch (Exception)
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
            catch (Exception)
            {
                // Ignore errors here
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
            var blnFastaFile = clsProteinFileDataCache.IsFastaFile(txtProteinInputFilePath.Text);

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

            var objOpenFile = new OpenFileDialog();

            var strFilePath = mXmlSettingsFilePath;

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
            var objProteinCoverageSummarizer = new clsProteinCoverageSummarizerRunner();

            try
            {
                if (string.IsNullOrWhiteSpace(strFilePath))
                {
                    // No parameter file specified; nothing to load
                    return;
                }

                if (!File.Exists(strFilePath))
                {
                    ShowErrorMessage("Parameter file not Found: " + strFilePath);
                    return;
                }

                var objSettingsFile = new XmlSettingsFileAccessor();

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
                            DelimitedFileReader.eDelimitedFileFormatCode eColumnOrdering;
                            try
                            {
                                eColumnOrdering = (DelimitedFileReader.eDelimitedFileFormatCode)objSettingsFile.GetParam(clsProteinCoverageSummarizer.XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileFormatCode", cboProteinInputFileColumnOrdering.SelectedIndex + PROTEIN_INPUT_FILE_INDEX_OFFSET);
                            }
                            catch (Exception)
                            {
                                eColumnOrdering = DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence;
                            }

                            try
                            {
                                cboProteinInputFileColumnOrdering.SelectedIndex = (int)eColumnOrdering - PROTEIN_INPUT_FILE_INDEX_OFFSET;
                            }
                            catch (Exception)
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
                            catch (Exception)
                            {
                                eColumnOrdering = DelimitedFileReader.eDelimitedFileFormatCode.ProteinName_Sequence;
                            }

                            try
                            {
                                cboPeptideInputFileColumnOrdering.SelectedIndex = (int)eColumnOrdering;
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
                objProteinCoverageSummarizer.LoadParameterFileSettings(strFilePath);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error calling LoadParameterFileSettings: " + ex);
            }

            try
            {
                LoadProcessingClassOptions(ref objProteinCoverageSummarizer);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error calling LoadProcessingClassOptions: " + ex);
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
            catch (Exception)
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
            catch (Exception)
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
                ShowErrorMessage("Error in InitializeDataGrid: " + ex);
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
            catch (Exception)
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
            var dlgOpenFileDialog = new OpenFileDialog()
            {
                Filter = "Fasta Files (*.fasta)|*.fasta|Text Files(*.txt)|*.txt|All Files (*.*)|*.*",
                FilterIndex = 3
            };

            var eResult = dlgOpenFileDialog.ShowDialog();
            if (eResult == DialogResult.OK)
            {
                txtProteinInputFilePath.Text = dlgOpenFileDialog.FileName;
                mLastFolderUsed = Path.GetDirectoryName(dlgOpenFileDialog.FileName);
            }
        }

        private void SelectPeptideInputFile()
        {
            var dlgOpenFileDialog = new OpenFileDialog()
            {
                InitialDirectory = mLastFolderUsed,
                Filter = "Text Files(*.txt)|*.txt|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            var eResult = dlgOpenFileDialog.ShowDialog();
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
            catch (Exception)
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

        private bool lastSequenceWasDataGrid;

        private void ShowRichTextStart(eSequenceDisplayConstants eSequenceDisplayMode = eSequenceDisplayConstants.UsePrevious)
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
            // Define a RegEx to remove whitespace characters
            var reReplaceSymbols = new Regex(@"[ \t\r\n]", RegexOptions.Compiled);

            try
            {
                // Lookup the number of characters per line
                var switchExpr = cboCharactersPerLine.SelectedIndex;
                int intModValue;
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
                var strRtf = @"{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Courier New;}}" +
                             @"{\colortbl\red0\green0\blue0;\red255\green0\blue0;}" +
                             @"\viewkind4\uc1\pard\f0\fs20 ";
                // ReSharper restore StringLiteralTypo

                var blnInUpperRegion = false;
                var intCharCount = 0;
                var intUppercaseCount = 0;
                if (strSequenceToShow == null)
                    strSequenceToShow = string.Empty;

                int intIndex;
                for (intIndex = 0; intIndex <= strSequenceToShow.Length - 1; intIndex++)
                {
                    if (intIndex > 0)
                    {
                        if (intIndex % intModValue == 0)
                        {
                            // Add a new line
                            strRtf += @"\par ";
                        }
                        else if (chkAddSpace.Checked && intIndex % 10 == 0)
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

                float sngCoveragePercent;
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

                var blnSuccess = SetOptionsFromGUI(mProteinCoverageSummarizer);
                if (blnSuccess)
                {
                    blnSuccess = mProteinCoverageSummarizer.ProcessFile(txtPeptideInputFilePath.Text, txtOutputFolderPath.Text);

                    if (blnSuccess & !(mProteinCoverageSummarizer.SearchAllProteinsForPeptideSequence & mProteinCoverageSummarizer.SearchAllProteinsSkipCoverageComputationSteps))
                    {
                        CreateSummaryDataTable(mProteinCoverageSummarizer.ResultsFilePath);
                    }

                    if (blnSuccess && lblStatus.Text.StartsWith("Done (9"))
                    {
                        lblStatus.Text = "Done";
                    }
                    else if (!blnSuccess)
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