Option Strict On

' This class uses ProteinCoverageSummarizer.dll to read in a protein fasta file or delimited protein info file along with
' an accompanying file with peptide sequences to find the proteins that contain each peptide
' It will also optionally compute the percent coverage of each of the proteins
'
' -------------------------------------------------------------------------------
' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Program started September 27, 2008
'
' E-mail: matthew.monroe@pnl.gov or matt@alchemistmatt.com
' Website: http://ncrr.pnl.gov/ or http://www.sysbio.org/resources/staff/
' -------------------------------------------------------------------------------
' 
' Licensed under the Apache License, Version 2.0; you may not use this file except
' in compliance with the License.  You may obtain a copy of the License at 
' http://www.apache.org/licenses/LICENSE-2.0
'
' Notice: This computer software was prepared by Battelle Memorial Institute, 
' hereinafter the Contractor, under Contract No. DE-AC05-76RL0 1830 with the 
' Department of Energy (DOE).  All rights in the computer software are reserved 
' by DOE on behalf of the United States Government and the Contractor as 
' provided in the Contract.  NEITHER THE GOVERNMENT NOR THE CONTRACTOR MAKES ANY 
' WARRANTY, EXPRESS OR IMPLIED, OR ASSUMES ANY LIABILITY FOR THE USE OF THIS 
' SOFTWARE.  This notice including this sentence must appear on any copies of 
' this computer software.

' Last updated May 1, 2014

Public Class clsPeptideToProteinMapEngine
	Inherits clsProcessFilesBaseClass

	Public Sub New()
		InitializeVariables()
	End Sub

#Region "Constants and Enums"

	Public Const FILENAME_SUFFIX_INSPECT_RESULTS_FILE As String = "_inspect.txt"
	Public Const FILENAME_SUFFIX_MSGFDB_RESULTS_FILE As String = "_msgfdb.txt"

	Public Const FILENAME_SUFFIX_PEP_TO_PROTEIN_MAPPING As String = "_PepToProtMap.txt"

	Protected Const FILENAME_SUFFIX_PSM_UNIQUE_PEPTIDES As String = "_peptides"

	' The following are the initial % complete value displayed during each of these stages
	Protected Const PERCENT_COMPLETE_PREPROCESSING As Single = 0
	Protected Const PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER As Single = 5
	Protected Const PERCENT_COMPLETE_POSTPROCESSING As Single = 95

	Public Enum eProteinCoverageErrorCodes
		NoError = 0
		UnspecifiedError = -1
	End Enum

	Public Enum ePeptideInputFileFormatConstants
		Unknown = -1
		AutoDetermine = 0
		PeptideListFile = 1				' First column is peptide sequence
		ProteinAndPeptideFile = 2		' First column is protein name, second column is peptide sequence
		InspectResultsFile = 3			' Inspect results file; pre-process the file to determine the peptides present, then determine the proteins that contain the given peptides
		MSGFDBResultsFile = 4			' MSGF-DB results file; pre-process the file to determine the peptides present, then determine the proteins that contain the given peptides
		PHRPFile = 5					' Sequest, Inspect, X!Tandem, or MSGF-DB synopsis or first-hits file created by PHRP; pre-process the file to determine the peptides present, then determine the proteins that contain the given peptides
	End Enum

#End Region

#Region "Structures"

	Protected Structure udtProteinIDMapInfoType
		Public ProteinID As Integer
		Public Peptide As String
		Public ResidueStart As Integer
		Public ResidueEnd As Integer
	End Structure

	Protected Structure udtPepToProteinMappingType
		Public Peptide As String
		Public Protein As String
		Public ResidueStart As Integer
		Public ResidueEnd As Integer
	End Structure
#End Region

#Region "Classwide variables"
	Protected WithEvents mProteinCoverageSummarizer As ProteinCoverageSummarizer.clsProteinCoverageSummarizer

	Private mPeptideInputFileFormat As ePeptideInputFileFormatConstants
	Private mDeleteTempFiles As Boolean

	' When processing an inspect search result file, if you provide the inspect parameter file name, 
	'  then this program will read the parameter file and look for the "mod," lines.  The user-assigned mod
	'  names will be extracted and used when "cleaning" the peptides prior to looking for matching proteins
	Private mInspectParameterFilePath As String

	Private mLocalErrorCode As eProteinCoverageErrorCodes
	Private mStatusMessage As String

	' The following is used when the input file is Sequest, X!Tandem, Inspect, or MSGF-DB results file
	Private mUniquePeptideList As Generic.SortedSet(Of String)

	' Mod names must be lower case, and 4 characters in length (or shorter)
	' Only used with Inspect since mods in MSGF-DB are simply numbers, e.g. R.DNFM+15.995SATQAVEYGLVDAVM+15.995TK.R
	'  while mods in Sequest and XTandem are symbols (*, #, @)
	Private mInspectModNameList As Generic.List(Of String)

#End Region

#Region "Properties"

	''' <summary>
	''' Legacy property; superseded by DeleteTempFiles
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property DeleteInspectTempFiles() As Boolean
		Get
			Return Me.DeleteTempFiles
		End Get
		Set(ByVal value As Boolean)
			Me.DeleteTempFiles = value
		End Set
	End Property

	Public Property DeleteTempFiles() As Boolean
		Get
			Return mDeleteTempFiles
		End Get
		Set(ByVal value As Boolean)
			mDeleteTempFiles = value
		End Set
	End Property

	Public Property IgnoreILDifferences() As Boolean
		Get
			Return mProteinCoverageSummarizer.IgnoreILDifferences
		End Get
		Set(ByVal Value As Boolean)
			mProteinCoverageSummarizer.IgnoreILDifferences = Value
		End Set
	End Property

	Public Property InspectParameterFilePath() As String
		Get
			Return mInspectParameterFilePath
		End Get
		Set(ByVal value As String)
			If value Is Nothing Then value = String.Empty
			mInspectParameterFilePath = value
		End Set
	End Property

	Public Property MatchPeptidePrefixAndSuffixToProtein() As Boolean
		Get
			Return mProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein
		End Get
		Set(ByVal Value As Boolean)
			mProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein = Value
		End Set
	End Property

	Public Property OutputProteinSequence() As Boolean
		Get
			Return mProteinCoverageSummarizer.OutputProteinSequence
		End Get
		Set(ByVal Value As Boolean)
			mProteinCoverageSummarizer.OutputProteinSequence = Value
		End Set
	End Property

	Public Property PeptideFileSkipFirstLine() As Boolean
		Get
			Return mProteinCoverageSummarizer.PeptideFileSkipFirstLine
		End Get
		Set(ByVal Value As Boolean)
			mProteinCoverageSummarizer.PeptideFileSkipFirstLine = Value
		End Set
	End Property

	Public Property PeptideInputFileDelimiter() As Char
		Get
			Return mProteinCoverageSummarizer.PeptideInputFileDelimiter
		End Get
		Set(ByVal Value As Char)
			mProteinCoverageSummarizer.PeptideInputFileDelimiter = Value
		End Set
	End Property

	Public Property PeptideInputFileFormat() As ePeptideInputFileFormatConstants
		Get
			Return mPeptideInputFileFormat
		End Get
		Set(ByVal value As ePeptideInputFileFormatConstants)
			mPeptideInputFileFormat = value
		End Set
	End Property

	Public Property ProteinDataDelimitedFileDelimiter() As Char
		Get
			Return mProteinCoverageSummarizer.mProteinDataCache.DelimitedFileDelimiter
		End Get
		Set(ByVal value As Char)
			mProteinCoverageSummarizer.mProteinDataCache.DelimitedFileDelimiter = value
		End Set
	End Property
	Public Property ProteinDataDelimitedFileFormatCode() As ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode
		Get
			Return mProteinCoverageSummarizer.mProteinDataCache.DelimitedFileFormatCode
		End Get
		Set(ByVal value As ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode)
			mProteinCoverageSummarizer.mProteinDataCache.DelimitedFileFormatCode = value
		End Set
	End Property
	Public Property ProteinDataDelimitedFileSkipFirstLine() As Boolean
		Get
			Return mProteinCoverageSummarizer.mProteinDataCache.DelimitedFileSkipFirstLine
		End Get
		Set(ByVal value As Boolean)
			mProteinCoverageSummarizer.mProteinDataCache.DelimitedFileSkipFirstLine = value
		End Set
	End Property
	Public Property ProteinDataRemoveSymbolCharacters() As Boolean
		Get
			Return mProteinCoverageSummarizer.mProteinDataCache.RemoveSymbolCharacters
		End Get
		Set(ByVal value As Boolean)
			mProteinCoverageSummarizer.mProteinDataCache.RemoveSymbolCharacters = value
		End Set
	End Property
	Public Property ProteinDataIgnoreILDifferences() As Boolean
		Get
			Return mProteinCoverageSummarizer.mProteinDataCache.IgnoreILDifferences
		End Get
		Set(ByVal value As Boolean)
			mProteinCoverageSummarizer.mProteinDataCache.IgnoreILDifferences = value
		End Set
	End Property

	Public Property ProteinInputFilePath() As String
		Get
			Return mProteinCoverageSummarizer.ProteinInputFilePath
		End Get
		Set(ByVal Value As String)
			If Value Is Nothing Then Value = String.Empty
			mProteinCoverageSummarizer.ProteinInputFilePath = Value
		End Set
	End Property

	Public ReadOnly Property ProteinToPeptideMappingFilePath() As String
		Get
			Return mProteinCoverageSummarizer.ProteinToPeptideMappingFilePath
		End Get
	End Property

	Public Property RemoveSymbolCharacters() As Boolean
		Get
			Return mProteinCoverageSummarizer.RemoveSymbolCharacters
		End Get
		Set(ByVal Value As Boolean)
			mProteinCoverageSummarizer.RemoveSymbolCharacters = Value
		End Set
	End Property

	Public ReadOnly Property ResultsFilePath() As String
		Get
			Return mProteinCoverageSummarizer.ResultsFilePath
		End Get
	End Property

	Public Property SaveProteinToPeptideMappingFile() As Boolean
		Get
			Return mProteinCoverageSummarizer.SaveProteinToPeptideMappingFile
		End Get
		Set(ByVal Value As Boolean)
			mProteinCoverageSummarizer.SaveProteinToPeptideMappingFile = Value
		End Set
	End Property

	Public Property SaveSourceDataPlusProteinsFile() As Boolean
		Get
			Return mProteinCoverageSummarizer.SaveSourceDataPlusProteinsFile
		End Get
		Set(value As Boolean)
			mProteinCoverageSummarizer.SaveSourceDataPlusProteinsFile = value
		End Set
	End Property

	Public Property SearchAllProteinsForPeptideSequence() As Boolean
		Get
			Return mProteinCoverageSummarizer.SearchAllProteinsForPeptideSequence
		End Get
		Set(ByVal Value As Boolean)
			mProteinCoverageSummarizer.SearchAllProteinsForPeptideSequence = Value
		End Set
	End Property

	Public Property UseLeaderSequenceHashTable() As Boolean
		Get
			Return mProteinCoverageSummarizer.UseLeaderSequenceHashTable
		End Get
		Set(ByVal Value As Boolean)
			mProteinCoverageSummarizer.UseLeaderSequenceHashTable = Value
		End Set
	End Property

	Public Property SearchAllProteinsSkipCoverageComputationSteps() As Boolean
		Get
			Return mProteinCoverageSummarizer.SearchAllProteinsSkipCoverageComputationSteps
		End Get
		Set(ByVal Value As Boolean)
			mProteinCoverageSummarizer.SearchAllProteinsSkipCoverageComputationSteps = Value
		End Set
	End Property

	Public ReadOnly Property StatusMessage() As String
		Get
			Return mStatusMessage
		End Get
	End Property

	Public Property TrackPeptideCounts() As Boolean
		Get
			Return mProteinCoverageSummarizer.TrackPeptideCounts
		End Get
		Set(ByVal Value As Boolean)
			mProteinCoverageSummarizer.TrackPeptideCounts = Value
		End Set
	End Property

#End Region

	Public Overrides Sub AbortProcessingNow()
		MyBase.AbortProcessingNow()
		If Not mProteinCoverageSummarizer Is Nothing Then
			mProteinCoverageSummarizer.AbortProcessingNow()
		End If
	End Sub

	Public Function DetermineResultsFileFormat(ByVal strFilePath As String) As ePeptideInputFileFormatConstants
		' Examine the strFilePath to determine the file format

		If System.IO.Path.GetFileName(strFilePath).ToLower.EndsWith(FILENAME_SUFFIX_INSPECT_RESULTS_FILE.ToLower()) Then
			Return ePeptideInputFileFormatConstants.InspectResultsFile

		ElseIf System.IO.Path.GetFileName(strFilePath).ToLower.EndsWith(FILENAME_SUFFIX_MSGFDB_RESULTS_FILE.ToLower()) Then
			Return ePeptideInputFileFormatConstants.MSGFDBResultsFile

		ElseIf mPeptideInputFileFormat <> ePeptideInputFileFormatConstants.AutoDetermine And mPeptideInputFileFormat <> ePeptideInputFileFormatConstants.Unknown Then
			Return mPeptideInputFileFormat
		End If

		Dim strBaseNameLCase As String = System.IO.Path.GetFileNameWithoutExtension(strFilePath)
		If strBaseNameLCase.EndsWith("_msgfdb") OrElse strBaseNameLCase.EndsWith("_msgfplus") Then
			Return ePeptideInputFileFormatConstants.MSGFDBResultsFile
		End If

		Dim eResultType As PHRPReader.clsPHRPReader.ePeptideHitResultType
		eResultType = PHRPReader.clsPHRPReader.AutoDetermineResultType(strFilePath)
		If eResultType <> PHRPReader.clsPHRPReader.ePeptideHitResultType.Unknown Then
			Return ePeptideInputFileFormatConstants.PHRPFile
		End If

		ShowMessage("Unable to determine the format of the input file based on the filename suffix; will assume the first column contains peptide sequence")
		Return ePeptideInputFileFormatConstants.PeptideListFile

	End Function

	Public Function ExtractModInfoFromInspectParamFile(ByVal strInspectParameterFilePath As String, ByRef lstInspectModNames As Generic.List(Of String)) As Boolean

		Dim strLineIn As String
		Dim strSplitLine As String()

		Dim intCurrentLine As Integer

		Dim blnSuccess As Boolean = False

		Try

			If lstInspectModNames Is Nothing Then
				lstInspectModNames = New Generic.List(Of String)
			Else
				lstInspectModNames.Clear()
			End If

			If strInspectParameterFilePath Is Nothing OrElse strInspectParameterFilePath.Length = 0 Then
				Return False
			End If

			ShowMessage("Looking for mod definitions in the Inspect param file: " & System.IO.Path.GetFileName(strInspectParameterFilePath))

			' Read the contents of strProteinToPeptideMappingFilePath
			Using srInFile As System.IO.StreamReader = New System.IO.StreamReader(New System.IO.FileStream(strInspectParameterFilePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))

				intCurrentLine = 1
				Do While srInFile.Peek <> -1
					strLineIn = srInFile.ReadLine

					strLineIn = strLineIn.Trim

					If strLineIn.Length > 0 Then

						If strLineIn.Chars(0) = "#"c Then
							' Comment line; skip it
						ElseIf strLineIn.ToLower.StartsWith("mod") Then
							' Modification definition line

							' Split the line on commas
							strSplitLine = strLineIn.Split(","c)

							If strSplitLine.Length >= 5 AndAlso strSplitLine(0).ToLower.Trim = "mod" Then

								Dim strModName As String
								strModName = strSplitLine(4).ToLower()

								If strModName.Length > 4 Then
									' Only keep the first 4 characters of the modification name
									strModName = strModName.Substring(0, 4)
								End If

								lstInspectModNames.Add(strModName)
								ShowMessage("Found modification: " & strLineIn & "   -->   Mod Symbol """ & strModName & """")

							End If
						End If
					End If
					intCurrentLine += 1

				Loop

			End Using

			Console.WriteLine()

			blnSuccess = True

		Catch ex As Exception
			mStatusMessage = "Error reading the Inspect parameter file (" & System.IO.Path.GetFileName(strInspectParameterFilePath) & ")"
			HandleException(mStatusMessage, ex)
		End Try

		Return blnSuccess

	End Function

	Public Overrides Function GetErrorMessage() As String
		Return MyBase.GetBaseClassErrorMessage
	End Function

	Private Sub InitializeVariables()
		Me.ShowMessages = True

		mPeptideInputFileFormat = ePeptideInputFileFormatConstants.AutoDetermine
		mDeleteTempFiles = True

		mInspectParameterFilePath = String.Empty

		mAbortProcessing = False
		mStatusMessage = String.Empty

		mProteinCoverageSummarizer = New ProteinCoverageSummarizer.clsProteinCoverageSummarizer

		mInspectModNameList = New Generic.List(Of String)

		mUniquePeptideList = New Generic.SortedSet(Of String)
	End Sub

	Public Function LoadParameterFileSettings(ByVal strParameterFilePath As String) As Boolean
		Return mProteinCoverageSummarizer.LoadParameterFileSettings(strParameterFilePath)
	End Function

	Protected Function PostProcessPSMResultsFile(ByVal strPeptideListFilePath As String, _
	   ByVal strProteinToPeptideMappingFilePath As String, _
	   ByVal blnDeleteWorkingFiles As Boolean) As Boolean

		Const UNKNOWN_PROTEIN_NAME As String = "__NoMatch__"

		Dim strPeptideToProteinMappingFilePath As String

		Dim strCleanSequence As String
		Dim strProtein As String

		Dim chPrefixResidue As Char
		Dim chSuffixResidue As Char

		Dim intMatchIndex As Integer
		Dim intProteinIDMatchIndex As Integer

		Dim objProteinMapPeptideComparer As ProteinIDMapInfoPeptideSearchComparer

		Dim strProteins() As String
		Dim intProteinIDPointerArray() As Integer

		Dim udtProteinMapInfo() As udtProteinIDMapInfoType

		Dim lstCachedData As Generic.List(Of udtPepToProteinMappingType)
		Dim objCachedDataComparer As PepToProteinMappingComparer

		Dim blnSuccess As Boolean = False

		Try
			Console.WriteLine()
			Console.WriteLine()

			ShowMessage("Post-processing the results files")

			If mUniquePeptideList Is Nothing OrElse mUniquePeptideList.Count = 0 Then
				mStatusMessage = "Error in PostProcessPSMResultsFile: mUniquePeptideList is empty; this is unexpected; unable to continue"

				HandleException(mStatusMessage, New System.Exception("Empty Array"))

				Return False
			End If

			ReDim strProteins(0)
			ReDim intProteinIDPointerArray(0)
			ReDim udtProteinMapInfo(0)

			blnSuccess = PostProcessPSMResultsFileReadMapFile(strProteinToPeptideMappingFilePath, _
			  strProteins, intProteinIDPointerArray, udtProteinMapInfo)

			' Sort udtProteinMapInfo on peptide, then on protein
			Array.Sort(udtProteinMapInfo, New ProteinIDMapInfoComparer)

			' Create the final result file
			strPeptideToProteinMappingFilePath = strProteinToPeptideMappingFilePath.Replace( _
			  FILENAME_SUFFIX_PSM_UNIQUE_PEPTIDES & ProteinCoverageSummarizer.clsProteinCoverageSummarizer.FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING, _
			  FILENAME_SUFFIX_PEP_TO_PROTEIN_MAPPING)

			LogMessage("Creating " & System.IO.Path.GetFileName(strPeptideToProteinMappingFilePath))

			Using swOutFile As System.IO.StreamWriter = New System.IO.StreamWriter(New System.IO.FileStream(strPeptideToProteinMappingFilePath, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.Read))

				' Write the headers
				swOutFile.WriteLine("Peptide" & ControlChars.Tab & _
				  "Protein" & ControlChars.Tab & _
				  "Residue_Start" & ControlChars.Tab & _
				  "Residue_End")

				' Initialize the Binary Search comparer
				objProteinMapPeptideComparer = New ProteinIDMapInfoPeptideSearchComparer


				' Assure that intProteinIDPointerArray and strProteins are sorted in parallel
				Array.Sort(intProteinIDPointerArray, strProteins)

				' Initialize lstCachedData
				lstCachedData = New Generic.List(Of udtPepToProteinMappingType)

				' Initialize objCachedDataComparer
				objCachedDataComparer = New PepToProteinMappingComparer

				For Each strPeptide As String In mUniquePeptideList

					' Construct the clean sequence for this peptide
					strCleanSequence = ProteinCoverageSummarizer.clsProteinCoverageSummarizer.GetCleanPeptideSequence( _
					 strPeptide, _
					 chPrefixResidue, _
					 chSuffixResidue, _
					 mProteinCoverageSummarizer.RemoveSymbolCharacters)

					If mInspectModNameList.Count > 0 Then
						strCleanSequence = RemoveInspectMods(strCleanSequence, mInspectModNameList)
					End If

					' Look for strCleanSequence in udtProteinMapInfo
					intMatchIndex = Array.BinarySearch(udtProteinMapInfo, strCleanSequence, objProteinMapPeptideComparer)

					If intMatchIndex < 0 Then
						' Match not found; this is unexpected
						' However, this code will be reached if the peptide is not present in any of the proteins in the protein data file
						swOutFile.WriteLine(strPeptide & ControlChars.Tab & _
						 UNKNOWN_PROTEIN_NAME & ControlChars.Tab & _
						  0.ToString & ControlChars.Tab & _
						  0.ToString)

					Else
						' Decrement intMatchIndex until the first match in udtProteinMapInfo is found
						Do While intMatchIndex > 0 AndAlso udtProteinMapInfo(intMatchIndex - 1).Peptide = strCleanSequence
							intMatchIndex -= 1
						Loop

						' Now write out each of the proteins for this peptide
						' We're caching results to lstCachedData so that we can sort by protein name
						lstCachedData.Clear()

						Do
							strProtein = String.Empty

							' Find the Protein for ID udtProteinMapInfo(intMatchIndex).ProteinID
							intProteinIDMatchIndex = Array.BinarySearch(intProteinIDPointerArray, udtProteinMapInfo(intMatchIndex).ProteinID)

							If intProteinIDMatchIndex >= 0 Then
								strProtein = strProteins(intProteinIDMatchIndex)
							Else
								strProtein = UNKNOWN_PROTEIN_NAME
							End If

							Try
								If strProtein <> strProteins(udtProteinMapInfo(intMatchIndex).ProteinID) Then
									' This is unexpected
									ShowMessage("Warning: Unexpected protein ID lookup array mismatch for ID " & udtProteinMapInfo(intMatchIndex).ProteinID.ToString)
								End If
							Catch ex As Exception
								' This code shouldn't be reached
								' Ignore errors occur
							End Try

							Dim udtCachedDataEntry As udtPepToProteinMappingType = New udtPepToProteinMappingType
							With udtCachedDataEntry
								.Peptide = String.Copy(strPeptide)
								.Protein = String.Copy(strProtein)
								.ResidueStart = udtProteinMapInfo(intMatchIndex).ResidueStart
								.ResidueEnd = udtProteinMapInfo(intMatchIndex).ResidueEnd
							End With

							lstCachedData.Add(udtCachedDataEntry)

							intMatchIndex += 1
						Loop While intMatchIndex < udtProteinMapInfo.Length AndAlso udtProteinMapInfo(intMatchIndex).Peptide = strCleanSequence

						If lstCachedData.Count > 1 Then
							lstCachedData.Sort(objCachedDataComparer)
						End If

						For intCacheIndex = 0 To lstCachedData.Count - 1
							With lstCachedData(intCacheIndex)
								swOutFile.WriteLine(.Peptide & ControlChars.Tab & _
								  .Protein & ControlChars.Tab & _
								  .ResidueStart.ToString & ControlChars.Tab & _
								  .ResidueEnd.ToString)

							End With

						Next
					End If

				Next

			End Using

			If blnDeleteWorkingFiles Then
				Try
					LogMessage("Deleting " & System.IO.Path.GetFileName(strPeptideListFilePath))
					System.IO.File.Delete(strPeptideListFilePath)
				Catch ex As Exception
				End Try

				Try
					LogMessage("Deleting " & System.IO.Path.GetFileName(strProteinToPeptideMappingFilePath))
					System.IO.File.Delete(strProteinToPeptideMappingFilePath)
				Catch ex As Exception
				End Try

			End If
			blnSuccess = True

		Catch ex As Exception
			mStatusMessage = "Error writing the Inspect or MSGF-DB peptide to protein map file in PostProcessPSMResultsFile"
			HandleException(mStatusMessage, ex)
		End Try

		Return blnSuccess

	End Function

	Protected Function PostProcessPSMResultsFileReadMapFile(ByVal strProteinToPeptideMappingFilePath As String, _
	  ByRef strProteins() As String, _
	  ByRef intProteinIDPointerArray() As Integer, _
	  ByRef udtProteinMapInfo() As udtProteinIDMapInfoType) As Boolean

		Dim intTerminatorSize As Integer = 2

		Dim strLineIn As String
		Dim strSplitLine As String()

		Dim intCurrentLine As Integer
		Dim bytesRead As Long = 0

		Dim dctProteinList As Generic.Dictionary(Of String, Integer)

		Dim intProteinMapInfoCount As Integer

		Dim strCurrentProtein As String
		Dim intCurrentProteinID As Integer

		Dim blnSuccess As Boolean = False

		Try

			' Initialize the protein list dictionary
			dctProteinList = New Generic.Dictionary(Of String, Integer)

			' Initialize the protein to peptide mapping array
			' We know the length will be at least as long as mUniquePeptideList, and easily twice that length
			ReDim udtProteinMapInfo(mUniquePeptideList.Count * 2 - 1)

			LogMessage("Reading " & System.IO.Path.GetFileName(strProteinToPeptideMappingFilePath))

			' Read the contents of strProteinToPeptideMappingFilePath
			Using srInFile As System.IO.StreamReader = New System.IO.StreamReader(New System.IO.FileStream(strProteinToPeptideMappingFilePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))

				strCurrentProtein = String.Empty

				intCurrentLine = 1
				Do While srInFile.Peek <> -1
					If mAbortProcessing Then Exit Do

					strLineIn = srInFile.ReadLine
					bytesRead += strLineIn.Length + intTerminatorSize

					strLineIn = strLineIn.Trim

					If intCurrentLine = 1 Then
						' Header line; skip it
					ElseIf strLineIn.Length > 0 Then

						' Split the line
						strSplitLine = strLineIn.Split(ControlChars.Tab)

						If strSplitLine.Length >= 4 Then
							If intProteinMapInfoCount >= udtProteinMapInfo.Length Then
								ReDim Preserve udtProteinMapInfo(udtProteinMapInfo.Length * 2 - 1)
							End If

							If strCurrentProtein.Length = 0 OrElse strCurrentProtein <> strSplitLine(0) Then
								' Determine the Protein ID for this protein

								strCurrentProtein = strSplitLine(0)

								If Not dctProteinList.TryGetValue(strCurrentProtein, intCurrentProteinID) Then
									' New protein; add it, assigning it index htProteinList.Count
									intCurrentProteinID = dctProteinList.Count
									dctProteinList.Add(strCurrentProtein, intCurrentProteinID)
								End If

							End If

							With udtProteinMapInfo(intProteinMapInfoCount)
								.ProteinID = intCurrentProteinID
								.Peptide = strSplitLine(1)
								.ResidueStart = Integer.Parse(strSplitLine(2))
								.ResidueEnd = Integer.Parse(strSplitLine(3))
							End With

							intProteinMapInfoCount += 1
						End If

					End If
					If intCurrentLine Mod 1000 = 0 Then
						UpdateProgress(PERCENT_COMPLETE_POSTPROCESSING + _
						   CSng((bytesRead / srInFile.BaseStream.Length) * 100) * (PERCENT_COMPLETE_POSTPROCESSING - PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER) / 100)
					End If
					intCurrentLine += 1

				Loop

			End Using

			' Populate strProteins() and intProteinIDPointerArray() using htProteinList
			ReDim strProteins(dctProteinList.Count - 1)
			ReDim intProteinIDPointerArray(dctProteinList.Count - 1)

			' Note: the Keys and Values are not necessarily sorted, but will be copied in the identical order
			dctProteinList.Keys.CopyTo(strProteins, 0)
			dctProteinList.Values.CopyTo(intProteinIDPointerArray, 0)

			' Shrink udtProteinMapInfo to the appropriate length
			ReDim Preserve udtProteinMapInfo(intProteinMapInfoCount - 1)

			blnSuccess = True

		Catch ex As Exception
			mStatusMessage = "Error reading the newly created protein to peptide mapping file (" & System.IO.Path.GetFileName(strProteinToPeptideMappingFilePath) & ")"
			HandleException(mStatusMessage, ex)
		End Try

		Return blnSuccess

	End Function

	Protected Function PreProcessInspectResultsFile(ByVal strInputFilePath As String, _
	   ByVal strOutputFolderPath As String, _
	   ByVal strInspectParameterFilePath As String) As String

		' Read strInspectParameterFilePath to extract the mod names
		If Not ExtractModInfoFromInspectParamFile(strInspectParameterFilePath, mInspectModNameList) Then
			If mInspectModNameList.Count = 0 Then
				mInspectModNameList.Add("phos")
			End If
		End If

		Return PreProcessPSMResultsFile(strInputFilePath, strOutputFolderPath, ePeptideInputFileFormatConstants.InspectResultsFile)

	End Function

	Protected Function PreProcessPSMResultsFile(ByVal strInputFilePath As String, _
												ByVal strOutputFolderPath As String, _
												ByVal eFileType As ePeptideInputFileFormatConstants) As String


		Dim strPeptideListFilePath As String = String.Empty

		Dim intTerminatorSize As Integer

		Dim strLineIn As String

		Dim chSepChars() As Char = New Char() {ControlChars.Tab}
		Dim strSplitLine As String()
		Dim strPeptideSequence As String = String.Empty

		Dim intCurrentLine As Integer
		Dim bytesRead As Long = 0

		Dim intModPeptidesFound As Integer

		Dim intPeptideSequenceColumnIndex As Integer
		Dim strToolDescription As String = String.Empty

		If eFileType = ePeptideInputFileFormatConstants.InspectResultsFile Then
			' Assume inspect results file line terminators are only a single byte (it doesn't matter if the terminators are actually two bytes)
			intTerminatorSize = 1

			' The 3rd column in the Inspect results file should have the peptide sequence
			intPeptideSequenceColumnIndex = 2
			strToolDescription = "Inspect"

		ElseIf eFileType = ePeptideInputFileFormatConstants.MSGFDBResultsFile Then
			intTerminatorSize = 2
			intPeptideSequenceColumnIndex = -1
			strToolDescription = "MSGF-DB"

		Else
			mStatusMessage = "Unrecognized file type: " & eFileType.ToString() & "; will look for column header 'Peptide'"

			intTerminatorSize = 2
			intPeptideSequenceColumnIndex = -1
			strToolDescription = "Generic PSM result file"
		End If

		Try
			If Not System.IO.File.Exists(strInputFilePath) Then
				SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath)
				mStatusMessage = "File not found: " & strInputFilePath

				If Me.ShowMessages Then
					ShowErrorMessage(mStatusMessage)
				Else
					Throw New System.Exception(mStatusMessage)
				End If

				Exit Try

			End If

			ShowMessage("Pre-processing the " & strToolDescription & " results file: " & System.IO.Path.GetFileName(strInputFilePath))

			' Initialize the peptide list
			If mUniquePeptideList Is Nothing Then
				mUniquePeptideList = New Generic.SortedSet(Of String)
			Else
				mUniquePeptideList.Clear()
			End If


			' Open the PSM results file and construct a unique list of peptides in the file (including any modification symbols)
			Using srInFile As System.IO.StreamReader = New System.IO.StreamReader(New System.IO.FileStream(strInputFilePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))

				intModPeptidesFound = 0
				intCurrentLine = 1
				Do While srInFile.Peek <> -1
					If mAbortProcessing Then Exit Do

					strLineIn = srInFile.ReadLine
					bytesRead += strLineIn.Length + intTerminatorSize

					strLineIn = strLineIn.Trim

					If intCurrentLine = 1 AndAlso (intPeptideSequenceColumnIndex < 0 OrElse strLineIn.StartsWith("#")) Then

						' Header line
						If intPeptideSequenceColumnIndex < 0 Then
							' Split the header line to look for the "Peptide" column
							strSplitLine = strLineIn.Split(chSepChars)
							For intIndex As Integer = 0 To strSplitLine.Length - 1
								If strSplitLine(intIndex).ToLower() = "peptide" Then
									intPeptideSequenceColumnIndex = intIndex
									Exit For
								End If
							Next

							If intPeptideSequenceColumnIndex < 0 Then
								SetBaseClassErrorCode(eProcessFilesErrorCodes.LocalizedError)
								mStatusMessage = "Peptide column not found; unable to continue"

								If Me.ShowMessages Then
									ShowErrorMessage(mStatusMessage)
								Else
									Throw New System.Exception(mStatusMessage)
								End If
								Return String.Empty

							End If

						End If

					ElseIf strLineIn.Length > 0 Then

						' Split the line, but for efficiency purposes, only parse up to column intPeptideSequenceColumnIndex
						strSplitLine = strLineIn.Split(chSepChars, intPeptideSequenceColumnIndex + 2)

						If strSplitLine.Length > intPeptideSequenceColumnIndex Then
							If Not mUniquePeptideList.Contains(strSplitLine(intPeptideSequenceColumnIndex)) Then
								mUniquePeptideList.Add(strSplitLine(intPeptideSequenceColumnIndex))
							End If
						End If

					End If

					If intCurrentLine Mod 1000 = 0 Then
						UpdateProgress(PERCENT_COMPLETE_PREPROCESSING + _
						   CSng((bytesRead / srInFile.BaseStream.Length) * 100) * (PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER - PERCENT_COMPLETE_PREPROCESSING) / 100)
					End If

					intCurrentLine += 1

				Loop

			End Using

			strPeptideListFilePath = PreProcessDataWriteOutPeptides(strInputFilePath, strOutputFolderPath)

		Catch ex As Exception
			mStatusMessage = "Error reading " & strToolDescription & " input file in PreProcessPSMResultsFile"
			HandleException(mStatusMessage, ex)

			strPeptideListFilePath = String.Empty
		End Try

		Return strPeptideListFilePath

	End Function

	Protected Function PreProcessPHRPDataFile(ByVal strInputFilePath As String, _
	  ByVal strOutputFolderPath As String, _
	  ByVal eFileType As ePeptideInputFileFormatConstants) As String

		Dim strPeptideListFilePath As String = String.Empty

		Try
			If Not System.IO.File.Exists(strInputFilePath) Then
				SetBaseClassErrorCode(eProcessFilesErrorCodes.InvalidInputFilePath)
				mStatusMessage = "File not found: " & strInputFilePath

				If Me.ShowMessages Then
					ShowErrorMessage(mStatusMessage)
				Else
					Throw New System.Exception(mStatusMessage)
				End If

				Exit Try

			End If

			Console.WriteLine()
			ShowMessage("Pre-processing PHRP data file: " & System.IO.Path.GetFileName(strInputFilePath))

			' Initialize the peptide list
			If mUniquePeptideList Is Nothing Then
				mUniquePeptideList = New Generic.SortedSet(Of String)
			Else
				mUniquePeptideList.Clear()
			End If

			Dim oStartupOptions = New PHRPReader.clsPHRPStartupOptions()
			With oStartupOptions
				.LoadModsAndSeqInfo = False
				.LoadMSGFResults = False
				.LoadScanStatsData = False
				.MaxProteinsPerPSM = 1
			End With

			' Open the PHRP data file and construct a unique list of peptides in the file (including any modification symbols)
			Using objReader As New PHRPReader.clsPHRPReader(strInputFilePath, PHRPReader.clsPHRPReader.ePeptideHitResultType.Unknown, oStartupOptions)
				objReader.EchoMessagesToConsole = True
				objReader.SkipDuplicatePSMs = False

				For Each strErrorMessage As String In objReader.ErrorMessages
					ShowErrorMessage(strErrorMessage)
				Next

				For Each strWarningMessage As String In objReader.WarningMessages
					ShowMessage("Warning: " & strWarningMessage)
				Next

				objReader.ClearErrors()
				objReader.ClearWarnings()

				AddHandler objReader.ErrorEvent, AddressOf PHRPReader_ErrorEvent
				AddHandler objReader.WarningEvent, AddressOf PHRPReader_WarningEvent

				Do While objReader.MoveNext()
					If mAbortProcessing Then Exit Do

					If Not mUniquePeptideList.Contains(objReader.CurrentPSM.Peptide) Then
						mUniquePeptideList.Add(objReader.CurrentPSM.Peptide)
					End If

					If mUniquePeptideList.Count Mod 1000 = 0 Then
						UpdateProgress(PERCENT_COMPLETE_PREPROCESSING + _
						   objReader.PercentComplete * (PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER - PERCENT_COMPLETE_PREPROCESSING) / 100)
					End If
				Loop
			End Using

			strPeptideListFilePath = PreProcessDataWriteOutPeptides(strInputFilePath, strOutputFolderPath)

		Catch ex As Exception
			mStatusMessage = "Error reading PSM input file in PreProcessPHRPDataFile"
			HandleException(mStatusMessage, ex)

			strPeptideListFilePath = String.Empty
		End Try

		Return strPeptideListFilePath

	End Function

	Protected Function PreProcessDataWriteOutPeptides(ByVal strInputFilePath As String, ByVal strOutputFolderPath As String) As String

		Dim strPeptideListFileName As String
		Dim strPeptideListFilePath As String = String.Empty
		Dim strPeptideOld As String

		Dim intModPeptidesFound As Integer

		Try

			' Now write out the unique list of peptides to strPeptideListFilePath
			strPeptideListFileName = System.IO.Path.GetFileNameWithoutExtension(strInputFilePath) & FILENAME_SUFFIX_PSM_UNIQUE_PEPTIDES & ".txt"

			If Not String.IsNullOrEmpty(strOutputFolderPath) Then
				strPeptideListFilePath = System.IO.Path.Combine(strOutputFolderPath, strPeptideListFileName)
			Else
				Dim ioFileInfo As System.IO.FileInfo
				ioFileInfo = New System.IO.FileInfo(strInputFilePath)

				strPeptideListFilePath = System.IO.Path.Combine(ioFileInfo.DirectoryName, strPeptideListFileName)
			End If

			LogMessage("Creating " & System.IO.Path.GetFileName(strPeptideListFileName))

			' Open the output file
			Using swOutFile As System.IO.StreamWriter = New System.IO.StreamWriter(New System.IO.FileStream(strPeptideListFilePath, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.Read))

				' Write out the peptides, removing any mod symbols that might be present
				For Each strPeptide As String In mUniquePeptideList
					If mInspectModNameList.Count > 0 Then
						strPeptideOld = String.Copy(strPeptide)
						strPeptide = RemoveInspectMods(strPeptide, mInspectModNameList)
						If strPeptide <> strPeptideOld Then
							intModPeptidesFound += 1
						End If
					End If

					swOutFile.WriteLine(strPeptide)
				Next
			End Using

		Catch ex As Exception
			mStatusMessage = "Error writing the Unique Peptides file in PreProcessDataWriteOutPeptides"
			HandleException(mStatusMessage, ex)

			strPeptideListFilePath = String.Empty
		End Try

		Return strPeptideListFilePath

	End Function

	Public Overloads Overrides Function ProcessFile(ByVal strInputFilePath As String, ByVal strOutputFolderPath As String, ByVal strParameterFilePath As String, ByVal blnResetErrorCode As Boolean) As Boolean

		Dim blnSuccess As Boolean

		Dim strInputFilePathWork As String
		Dim strProteinToPeptideMappingFilePath As String = String.Empty

		Dim eInputFileFormat As ePeptideInputFileFormatConstants

		If blnResetErrorCode Then
			MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.NoError)
		End If

		Try
			If strInputFilePath Is Nothing OrElse strInputFilePath.Length = 0 Then
				ShowMessage("Input file name is empty")
				MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.InvalidInputFilePath)
			Else
				' Note that CleanupFilePaths() will update mOutputFolderPath, which is used by LogMessage()
				If Not CleanupFilePaths(strInputFilePath, strOutputFolderPath) Then
					MyBase.SetBaseClassErrorCode(clsProcessFilesBaseClass.eProcessFilesErrorCodes.FilePathError)
				Else

					LogMessage("Processing " & System.IO.Path.GetFileName(strInputFilePath))

					If mPeptideInputFileFormat = ePeptideInputFileFormatConstants.AutoDetermine Then
						eInputFileFormat = DetermineResultsFileFormat(strInputFilePath)
					Else
						eInputFileFormat = mPeptideInputFileFormat
					End If

					If eInputFileFormat = ePeptideInputFileFormatConstants.Unknown Then
						ShowMessage("Input file type not recognized")
						Return False
					End If

					UpdateProgress("Preprocessing input file", PERCENT_COMPLETE_PREPROCESSING)
					mInspectModNameList.Clear()

					Select Case eInputFileFormat
						Case ePeptideInputFileFormatConstants.InspectResultsFile
							' Inspect search results file; need to pre-process it
							strInputFilePathWork = PreProcessInspectResultsFile(strInputFilePath, strOutputFolderPath, mInspectParameterFilePath)
							mProteinCoverageSummarizer.PeptideFileFormatCode = ProteinCoverageSummarizer.clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode.SequenceOnly
							mProteinCoverageSummarizer.PeptideFileSkipFirstLine = False
							mProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein = False

						Case ePeptideInputFileFormatConstants.MSGFDBResultsFile
							' MSGF-DB search results file; need to pre-process it
							' Make sure RemoveSymbolCharacters is true
							Me.RemoveSymbolCharacters = True

							strInputFilePathWork = PreProcessPSMResultsFile(strInputFilePath, strOutputFolderPath, eInputFileFormat)
							mProteinCoverageSummarizer.PeptideFileFormatCode = ProteinCoverageSummarizer.clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode.SequenceOnly
							mProteinCoverageSummarizer.PeptideFileSkipFirstLine = False
							mProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein = False

						Case ePeptideInputFileFormatConstants.PHRPFile
							' Sequest, X!Tandem, Inspect, or MSGF-DB PHRP data file; need to pre-process it
							' Make sure RemoveSymbolCharacters is true
							Me.RemoveSymbolCharacters = True

							strInputFilePathWork = PreProcessPHRPDataFile(strInputFilePath, strOutputFolderPath, eInputFileFormat)
							mProteinCoverageSummarizer.PeptideFileFormatCode = ProteinCoverageSummarizer.clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode.SequenceOnly
							mProteinCoverageSummarizer.PeptideFileSkipFirstLine = False
							mProteinCoverageSummarizer.MatchPeptidePrefixAndSuffixToProtein = False

						Case Else
							' No need to pre-process the input file
							strInputFilePathWork = String.Copy(strInputFilePath)

							If eInputFileFormat = ePeptideInputFileFormatConstants.ProteinAndPeptideFile Then
								mProteinCoverageSummarizer.PeptideFileFormatCode = ProteinCoverageSummarizer.clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
							Else
								mProteinCoverageSummarizer.PeptideFileFormatCode = ProteinCoverageSummarizer.clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode.SequenceOnly
							End If
					End Select

					If String.IsNullOrWhiteSpace(strInputFilePathWork) Then
						Return False
					End If

					UpdateProgress("Running protein coverage summarizer", PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER)

					' Call mProteinCoverageSummarizer.ProcessFile to perform the work
					blnSuccess = mProteinCoverageSummarizer.ProcessFile(strInputFilePathWork, strOutputFolderPath, strParameterFilePath, True, strProteinToPeptideMappingFilePath)
					If Not blnSuccess Then
						mStatusMessage = "Error running ProteinCoverageSummarizer: " & mProteinCoverageSummarizer.ErrorMessage
					End If

					If blnSuccess AndAlso strProteinToPeptideMappingFilePath.Length > 0 Then
						UpdateProgress("Postprocessing", PERCENT_COMPLETE_POSTPROCESSING)

						Select Case eInputFileFormat
							Case ePeptideInputFileFormatConstants.PeptideListFile, ePeptideInputFileFormatConstants.ProteinAndPeptideFile
								' No post-processing is required

							Case Else
								' Sequest, X!Tandem, Inspect, or MSGF-DB PHRP data file; need to post-process the results file
								blnSuccess = PostProcessPSMResultsFile(strInputFilePathWork, strProteinToPeptideMappingFilePath, mDeleteTempFiles)

						End Select
					End If

					If blnSuccess Then
						LogMessage("Processing successful")
						OperationComplete()
					Else
						LogMessage("Processing not successful")
					End If
				End If
			End If

		Catch ex As Exception
			HandleException("Error in ProcessFile", ex)
			blnSuccess = False
		End Try

		Return blnSuccess

	End Function

	Protected Function RemoveInspectMods(ByVal strPeptide As String, ByRef lstInspectModNames As Generic.List(Of String)) As String

		Dim strPrefix As String = String.Empty
		Dim strSuffix As String = String.Empty

		If strPeptide.Length >= 4 Then
			If strPeptide.Chars(1) = "."c AndAlso _
			   strPeptide.Chars(strPeptide.Length - 2) = "."c Then
				strPrefix = strPeptide.Substring(0, 2)
				strSuffix = strPeptide.Substring(strPeptide.Length - 2, 2)

				strPeptide = strPeptide.Substring(2, strPeptide.Length - 4)
			End If
		End If

		For Each strModName As String In lstInspectModNames
			strPeptide = strPeptide.Replace(strModName, String.Empty)
		Next

		Return strPrefix & strPeptide & strSuffix

	End Function

#Region "Protein Coverage Summarizer Event Handlers"
	Private Sub mProteinCoverageSummarizer_ProgressChanged(ByVal taskDescription As String, ByVal percentComplete As Single) Handles mProteinCoverageSummarizer.ProgressChanged
		Dim sngPercentCompleteEffective As Single

		sngPercentCompleteEffective = PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER + _
		   percentComplete * CSng((PERCENT_COMPLETE_POSTPROCESSING - PERCENT_COMPLETE_RUNNING_PROTEIN_COVERAGE_SUMMARIZER) / 100.0)

		UpdateProgress(taskDescription, sngPercentCompleteEffective)
	End Sub

	Private Sub mProteinCoverageSummarizer_ProgressComplete() Handles mProteinCoverageSummarizer.ProgressComplete
		OperationComplete()
	End Sub

	Private Sub mProteinCoverageSummarizer_ProgressReset() Handles mProteinCoverageSummarizer.ProgressReset
		ResetProgress(mProteinCoverageSummarizer.ProgressStepDescription)
	End Sub
#End Region


#Region "PHRPReader Event Handlers"

	Private Sub PHRPReader_ErrorEvent(strErrorMessage As String)
		ShowErrorMessage(strErrorMessage)
	End Sub

	Private Sub PHRPReader_WarningEvent(strWarningMessage As String)
		ShowMessage("Warning: " & strWarningMessage)
	End Sub

#End Region

#Region "IComparer Classes"
	Protected Class ProteinIDMapInfoComparer
		Implements System.Collections.IComparer

		Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements System.Collections.IComparer.Compare
			Dim xData As udtProteinIDMapInfoType = CType(x, udtProteinIDMapInfoType)
			Dim yData As udtProteinIDMapInfoType = CType(y, udtProteinIDMapInfoType)

			If xData.Peptide > yData.Peptide Then
				Return 1
			ElseIf xData.Peptide < yData.Peptide Then
				Return -1
			Else
				If xData.ProteinID > yData.ProteinID Then
					Return 1
				ElseIf xData.ProteinID < yData.ProteinID Then
					Return -1
				Else
					Return 0
				End If
			End If

		End Function
	End Class

	Protected Class ProteinIDMapInfoPeptideSearchComparer
		Implements System.Collections.IComparer

		Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements System.Collections.IComparer.Compare
			Dim xData As udtProteinIDMapInfoType = CType(x, udtProteinIDMapInfoType)
			Dim strPeptide As String = CType(y, String)

			If xData.Peptide > strPeptide Then
				Return 1
			ElseIf xData.Peptide < strPeptide Then
				Return -1
			Else
				Return 0
			End If

		End Function
	End Class

	Protected Class PepToProteinMappingComparer
		Implements System.Collections.Generic.IComparer(Of udtPepToProteinMappingType)

		Public Function Compare(x As udtPepToProteinMappingType, y As udtPepToProteinMappingType) As Integer Implements System.Collections.Generic.IComparer(Of udtPepToProteinMappingType).Compare

			If x.Peptide > y.Peptide Then
				Return 1
			ElseIf x.Peptide < y.Peptide Then
				Return -1
			Else
				If x.Protein > y.Protein Then
					Return 1
				ElseIf x.Protein < y.Protein Then
					Return -1
				Else
					Return 0
				End If
			End If

		End Function

	End Class
#End Region

End Class
