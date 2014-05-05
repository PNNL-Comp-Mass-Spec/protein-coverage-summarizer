Option Strict On

' This class will read in a protein fasta file or delimited protein info file along with
' an accompanying file with peptide sequences and compute the percent coverage of each of the proteins
'
' -------------------------------------------------------------------------------
' Written by Matthew Monroe and Nikša Blonder for the Department of Energy (PNNL, Richland, WA)
' Program started June 14, 2005
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

' Last updated October 22, 2012

<CLSCompliant(True)>
Public Class clsProteinCoverageSummarizer

	Public Sub New()
		InitializeVariables()
	End Sub

#Region "Constants and Enums"
	Public Const XML_SECTION_PROCESSING_OPTIONS As String = "ProcessingOptions"

	Public Const OUTPUT_FILE_PROTEIN_DESCRIPTION_COLUMN_NUMBER As Integer = 3
	Public Const OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER As Integer = 7

	Public Const NEW_PROTEINS_CACHE_MEMORY_RESERVE_COUNT As Integer = 500

	Public Const FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING As String = "_ProteinToPeptideMapping.txt"
	Public Const FILENAME_SUFFIX_SOURCE_PLUS_ALL_PROTEINS As String = "_AllProteins.txt"

	Protected Const PROTEIN_CHUNK_COUNT As Integer = 50000

	Public Enum DelimiterCharConstants
		Space = 0
		Tab = 1
		Comma = 2
		Other = 3
	End Enum

	Public Enum ePeptideFileColumnOrderingCode As Integer
		SequenceOnly = 0
		ProteinName_PeptideSequence = 1
	End Enum

	Public Enum eProteinCoverageErrorCodes
		NoError = 0
		InvalidInputFilePath = 1
		ErrorReadingParameterFile = 2
		FilePathError = 16
		UnspecifiedError = -1
	End Enum

	' Note: if you add/remove any steps, then update PERCENT_COMPLETE_LEVEL_COUNT and update the population of mPercentCompleteStartLevels()
	Enum eProteinCoverageProcessingSteps
		Starting = 0
		CacheProteins = 1
		DetermineShortestPeptideLength = 2
		CachePeptides = 3
		SearchProteinsUsingLeaderSequences = 4
		SearchProteinsAgainstShortPeptides = 5
		ComputePercentCoverage = 6
		WriteProteinCoverageFile = 7
		SaveAllProteinsVersionOfInputFile = 8
	End Enum

#End Region

#Region "Structures"
	Protected Structure udtPeptideCountStatsType
		Public UniquePeptideCount As Integer
		Public NonUniquePeptideCount As Integer
	End Structure

	Protected Structure udtSequence
		Public KeyRecord As Integer
		Public NewSequenceValue As String
	End Structure
#End Region

#Region "Classwide variables"
	Public WithEvents mProteinDataCache As clsProteinFileDataCache
	Private WithEvents mLeaderSequenceCache As clsLeaderSequenceCache
	Private mNewProteinsCache() As udtSequence

	' This hashtable contains entries of the form 1234::K.ABCDEFR.A
	'  where the number is the protein ID and the peptide is the peptide sequence
	' The value for each entry is the number of times the peptide is present in the given protein
	' This hashtable is only populated if mTrackPeptideCounts is true
	Private mProteinPeptideStats As Hashtable

	Private mProteinInputFilePath As String
	Private mResultsFilePath As String				' This value is populated by function ProcessFile()

	Private mOutputProteinSequence As Boolean

	Private mPeptideFileSkipFirstLine As Boolean
	Private mPeptideInputFileDelimiter As Char
	Private mPeptideFileColumnOrdering As ePeptideFileColumnOrderingCode

	Private mRemoveSymbolCharacters As Boolean
	Private mMatchPeptidePrefixAndSuffixToProtein As Boolean
	Private mIgnoreILDifferences As Boolean

	Private mSearchAllProteinsForPeptideSequence As Boolean
	Private mSearchAllProteinsSkipCoverageComputationSteps As Boolean

	' If this is disabled, then a brute-force search is applied, 
	' The brute-force search is nearly always much slower than with mUseLeaderSequenceHashTable enabled
	Private mUseLeaderSequenceHashTable As Boolean

	Private mSaveProteinToPeptideMappingFile As Boolean
	Private mProteinToPeptideMappingFilePath As String
	Private mProteinToPeptideMappingOutputFile As System.IO.StreamWriter

	Private mSaveSourceDataPlusProteinsFile As Boolean
	Private mInputFileWithAllProteins As System.IO.StreamWriter

	Private mTrackPeptideCounts As Boolean

	Private mErrorCode As eProteinCoverageErrorCodes
	Private mErrorMessage As String

	Private mShowMessages As Boolean
	Private mAbortProcessing As Boolean

	Private mCachedProteinInfoStartIndex As Integer = -1
	Private mCachedProteinInfoCount As Integer
	Private mCachedProteinInfo() As clsProteinFileDataCache.udtProteinInfoType

	Private mPeptideToProteinMapResults As Generic.Dictionary(Of String, Generic.List(Of String))

	' mPercentCompleteStartLevels is an array that lists the percent complete value to report 
	'  at the start of each of the various processing steps performed in this procedure
	' The percent complete values range from 0 to 100
	Const PERCENT_COMPLETE_LEVEL_COUNT As Integer = 9
	Protected mPercentCompleteStartLevels() As Single

#End Region

#Region "Progress Events and Variables"
	Public Event ProgressReset()
	Public Event ProgressChanged(ByVal taskDescription As String, ByVal percentComplete As Single)	   ' PercentComplete ranges from 0 to 100, but can contain decimal percentage values
	Public Event ProgressComplete()

	' Note: These events are no longer used
	''Public Event SubtaskProgressChanged(ByVal taskDescription As String, ByVal percentComplete As Single)     ' PercentComplete ranges from 0 to 100, but can contain decimal percentage values
	''Public Event SubtaskProgressComplete()

	Protected mCurrentProcessingStep As eProteinCoverageProcessingSteps = eProteinCoverageProcessingSteps.Starting
	Protected mProgressStepDescription As String = String.Empty
	Protected mProgressPercentComplete As Single		' Ranges from 0 to 100, but can contain decimal percentage values

	''Protected mSubtaskStepDescription As String = String.Empty
	''Protected mSubtaskPercentComplete As Single        ' Ranges from 0 to 100, but can contain decimal percentage values
#End Region

#Region "Properties"
	Public ReadOnly Property ErrorCode() As eProteinCoverageErrorCodes
		Get
			Return mErrorCode
		End Get
	End Property

	Public ReadOnly Property ErrorMessage() As String
		Get
			Return GetErrorMessage()
		End Get
	End Property

	Public Property IgnoreILDifferences() As Boolean
		Get
			Return mIgnoreILDifferences
		End Get
		Set(ByVal Value As Boolean)
			mIgnoreILDifferences = Value
		End Set
	End Property

	Public Property MatchPeptidePrefixAndSuffixToProtein() As Boolean
		Get
			Return mMatchPeptidePrefixAndSuffixToProtein
		End Get
		Set(ByVal Value As Boolean)
			mMatchPeptidePrefixAndSuffixToProtein = Value
		End Set
	End Property

	Public Property OutputProteinSequence() As Boolean
		Get
			Return mOutputProteinSequence
		End Get
		Set(ByVal Value As Boolean)
			mOutputProteinSequence = Value
		End Set
	End Property

	Public Property PeptideFileFormatCode() As ePeptideFileColumnOrderingCode
		Get
			Return mPeptideFileColumnOrdering
		End Get
		Set(ByVal Value As ePeptideFileColumnOrderingCode)
			mPeptideFileColumnOrdering = Value
		End Set
	End Property

	Public Property PeptideFileSkipFirstLine() As Boolean
		Get
			Return mPeptideFileSkipFirstLine
		End Get
		Set(ByVal Value As Boolean)
			mPeptideFileSkipFirstLine = Value
		End Set
	End Property

	Public Property PeptideInputFileDelimiter() As Char
		Get
			Return mPeptideInputFileDelimiter
		End Get
		Set(ByVal Value As Char)
			mPeptideInputFileDelimiter = Value
		End Set
	End Property

	Public Overridable ReadOnly Property ProgressStepDescription() As String
		Get
			Return mProgressStepDescription
		End Get
	End Property

	' ProgressPercentComplete ranges from 0 to 100, but can contain decimal percentage values
	Public ReadOnly Property ProgressPercentComplete() As Single
		Get
			Return CType(Math.Round(mProgressPercentComplete, 2), Single)
		End Get
	End Property

	Public Property ProteinInputFilePath() As String
		Get
			Return mProteinInputFilePath
		End Get
		Set(ByVal Value As String)
			mProteinInputFilePath = Value
		End Set
	End Property

	Public ReadOnly Property ProteinToPeptideMappingFilePath() As String
		Get
			Return mProteinToPeptideMappingFilePath
		End Get
	End Property

	Public Property RemoveSymbolCharacters() As Boolean
		Get
			Return mRemoveSymbolCharacters
		End Get
		Set(ByVal Value As Boolean)
			mRemoveSymbolCharacters = Value
		End Set
	End Property

	Public ReadOnly Property ResultsFilePath() As String
		Get
			Return mResultsFilePath
		End Get
	End Property

	Public Property SaveProteinToPeptideMappingFile() As Boolean
		Get
			Return mSaveProteinToPeptideMappingFile
		End Get
		Set(ByVal Value As Boolean)
			mSaveProteinToPeptideMappingFile = Value
		End Set
	End Property

	Public Property SaveSourceDataPlusProteinsFile As Boolean
		Get
			Return mSaveSourceDataPlusProteinsFile
		End Get
		Set(value As Boolean)
			mSaveSourceDataPlusProteinsFile = value
		End Set
	End Property

	Public Property SearchAllProteinsForPeptideSequence() As Boolean
		Get
			Return mSearchAllProteinsForPeptideSequence
		End Get
		Set(ByVal Value As Boolean)
			mSearchAllProteinsForPeptideSequence = Value
		End Set
	End Property

	Public Property UseLeaderSequenceHashTable() As Boolean
		Get
			Return mUseLeaderSequenceHashTable
		End Get
		Set(ByVal Value As Boolean)
			mUseLeaderSequenceHashTable = Value
		End Set
	End Property

	Public Property SearchAllProteinsSkipCoverageComputationSteps() As Boolean
		Get
			Return mSearchAllProteinsSkipCoverageComputationSteps
		End Get
		Set(ByVal Value As Boolean)
			mSearchAllProteinsSkipCoverageComputationSteps = Value
		End Set
	End Property

	Public ReadOnly Property StatusMessage() As String
		Get
			Return mErrorMessage
		End Get
	End Property

	''Public ReadOnly Property SubtaskStepDescription() As String
	''    Get
	''        Return mSubtaskStepDescription
	''    End Get
	''End Property

	''Public ReadOnly Property SubtaskPercentComplete() As Single
	''    Get
	''        Return mSubtaskPercentComplete
	''    End Get
	''End Property

	Public Property TrackPeptideCounts() As Boolean
		Get
			Return mTrackPeptideCounts
		End Get
		Set(ByVal Value As Boolean)
			mTrackPeptideCounts = Value
		End Set
	End Property

#End Region

	Public Sub AbortProcessingNow()
		If Not mLeaderSequenceCache Is Nothing Then
			mLeaderSequenceCache.AbortProcessingNow()
		End If
	End Sub

	Private Function BooleanArrayContainsTrueEntries(ByRef blnArrayToCheck() As Boolean, ByVal intArrayLength As Integer) As Boolean

		Dim intIndex As Integer
		Dim blnContainsTrueEntries As Boolean = False

		For intIndex = 0 To intArrayLength - 1
			If blnArrayToCheck(intIndex) Then
				blnContainsTrueEntries = True
				Exit For
			End If
		Next

		Return blnContainsTrueEntries

	End Function

	Private Function CapitalizeMatchingProteinSequenceLetters(ByVal strProteinSequence As String, ByVal strPeptideSequence As String, ByVal strKey As String, ByVal chPrefixResidue As Char, ByVal chSuffixResidue As Char, ByRef blnMatchFound As Boolean, ByRef blnMatchIsNew As Boolean, ByRef intStartResidue As Integer, ByRef intEndResidue As Integer) As String
		' Note: this function assumes strPeptideSequence, chPrefix, and chSuffix have all uppercase letters
		' chPrefix and chSuffix are only used if mMatchPeptidePrefixAndSuffixToProtein = true

		Dim intCharIndex, intNextStartIndex As Integer
		Dim strNewProteinSequence As String
		Dim intPeptideCount As Integer = 0				' Note: This is a count of the number of times the peptide is present in the protein sequence (typically 1); this value is not stored anywhere

		Dim blnCurrentMatchValid As Boolean

		Dim objItem As Object

		blnMatchFound = False
		blnCurrentMatchValid = False
		blnMatchIsNew = False

		intStartResidue = 0
		intEndResidue = 0

		If mSearchAllProteinsSkipCoverageComputationSteps Then
			' No need to capitalize strProteinSequence since it's already capitalized
			intCharIndex = strProteinSequence.IndexOf(strPeptideSequence)
		Else
			' Need to change strProteinSequence to all caps when searching for strPeptideSequence
			intCharIndex = strProteinSequence.ToUpper.IndexOf(strPeptideSequence)
		End If

		If intCharIndex >= 0 Then
			intStartResidue = intCharIndex + 1
			intEndResidue = intStartResidue + strPeptideSequence.Length - 1

			blnMatchFound = True

			If mMatchPeptidePrefixAndSuffixToProtein Then
				blnCurrentMatchValid = ValidatePrefixAndSuffix(strProteinSequence, chPrefixResidue, chSuffixResidue, intCharIndex, intEndResidue - 1)
			Else
				blnCurrentMatchValid = True
			End If

			If blnCurrentMatchValid Then
				intPeptideCount += 1
			Else
				intStartResidue = 0
				intEndResidue = 0
			End If
		Else
			blnCurrentMatchValid = False
		End If

		If blnMatchFound AndAlso Not mSearchAllProteinsSkipCoverageComputationSteps Then
			Do While intCharIndex >= 0

				If blnCurrentMatchValid Then
					intNextStartIndex = intCharIndex + strPeptideSequence.Length

					strNewProteinSequence = String.Empty
					If intCharIndex > 0 Then
						strNewProteinSequence = strProteinSequence.Substring(0, intCharIndex)
					End If
					strNewProteinSequence &= strProteinSequence.Substring(intCharIndex, intNextStartIndex - intCharIndex).ToUpper
					strNewProteinSequence &= strProteinSequence.Substring(intNextStartIndex)
					strProteinSequence = String.Copy(strNewProteinSequence)
				End If

				' Look for another occurrence of strPeptideSequence in this protein
				intCharIndex = strProteinSequence.ToUpper.IndexOf(strPeptideSequence, intCharIndex + 1)

				If intCharIndex >= 0 Then
					If mMatchPeptidePrefixAndSuffixToProtein Then
						blnCurrentMatchValid = ValidatePrefixAndSuffix(strProteinSequence, chPrefixResidue, chSuffixResidue, intCharIndex, intCharIndex + strPeptideSequence.Length - 1)
					Else
						blnCurrentMatchValid = True
					End If

					If blnCurrentMatchValid Then
						intPeptideCount += 1

						If intStartResidue = 0 Then
							intStartResidue = intCharIndex + 1
							intEndResidue = intStartResidue + strPeptideSequence.Length - 1
						End If
					End If
				End If
			Loop
		End If


		If blnMatchFound Then
			If intPeptideCount = 0 Then
				' The protein contained strPeptideSequence, but mMatchPeptidePrefixAndSuffixToProtein = true and either chPrefixResidue or chSuffixResidue doesn't match
				blnMatchFound = False
			ElseIf mTrackPeptideCounts Then
				Dim intPreviousPeptideCount As Integer

				objItem = mProteinPeptideStats(strKey)
				If Not objItem Is Nothing Then
					intPreviousPeptideCount = CInt(objItem)
					mProteinPeptideStats.Item(strKey) = intPreviousPeptideCount + 1
				Else
					blnMatchIsNew = True
					mProteinPeptideStats.Add(strKey, 1)
				End If
			Else
				' Must always assume the match is new since not tracking peptide counts
				blnMatchIsNew = True
			End If
		End If

		Return strProteinSequence

	End Function

	Private Function ConstructPeptideSequenceForKey(ByVal strPeptideSequence As String, ByVal chPrefixResidue As Char, ByVal chSuffixResidue As Char) As String
		Dim strPeptideSequenceForKey As String

		If System.Convert.ToInt32(chPrefixResidue) = 0 AndAlso System.Convert.ToInt32(chSuffixResidue) = 0 Then
			strPeptideSequenceForKey = String.Copy(strPeptideSequence)
		Else
			If Char.IsLetter(chPrefixResidue) Then
				chPrefixResidue = Char.ToUpper(chPrefixResidue)
				strPeptideSequenceForKey = chPrefixResidue & "."c & strPeptideSequence
			Else
				strPeptideSequenceForKey = "-." & strPeptideSequence
			End If

			If Char.IsLetter(chSuffixResidue) Then
				chSuffixResidue = Char.ToUpper(chSuffixResidue)
				strPeptideSequenceForKey &= "."c & chSuffixResidue
			Else
				strPeptideSequenceForKey &= ".-"
			End If
		End If

		Return strPeptideSequenceForKey
	End Function

	Private Sub CreateProteinCoverageFile(ByVal strPeptideInputFilePath As String, ByVal strOutputFolderPath As String)
		Const INITIAL_PROTEIN_COUNT_RESERVE As Integer = 5000

		Dim intProteinIndex As Integer
		Dim intProteinID As Integer

		Dim strKey As String
		Dim intColonIndex As Integer

		Dim swOutputFile As System.IO.StreamWriter
		Dim strLineOut As String

		Dim NonUniquePeptideCount As Integer
		Dim UniquePeptideCount As Integer

		Dim myEnumerator As IDictionaryEnumerator

		' The data in mProteinPeptideStats is copied into these two arrays for fast lookup 
		' This is necessary since use of the enumerator returned by mProteinPeptideStats.GetEnumerator 
		'  for every protein in mProteinDataCache.mProteins leads to very slow program performance
		Dim intPeptideStatsCount As Integer
		Dim udtPeptideStats() As udtPeptideCountStatsType

		' Contains pointers to entries in udtPeptideStats()
		Dim htProteinIDLookup As Hashtable
		Dim objItem As Object
		Dim intTargetIndex As Integer

		If mResultsFilePath = Nothing OrElse mResultsFilePath.Length = 0 Then
			If strPeptideInputFilePath.Length > 0 Then
				mResultsFilePath = System.IO.Path.Combine(GetOutputFolderPath(strOutputFolderPath, strPeptideInputFilePath), System.IO.Path.GetFileNameWithoutExtension(strPeptideInputFilePath) & "_coverage.txt")
			Else
				mResultsFilePath = System.IO.Path.Combine(GetOutputFolderPath(strOutputFolderPath, String.Empty), "Peptide_coverage.txt")
			End If
		End If

		UpdateProgress("Creating the protein coverage file: " & System.IO.Path.GetFileName(mResultsFilePath), 0, _
		   eProteinCoverageProcessingSteps.WriteProteinCoverageFile)

		swOutputFile = New IO.StreamWriter(New System.IO.FileStream(mResultsFilePath, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.Read))

		' Note: If the column ordering is changed, be sure to update OUTPUT_FILE_PROTEIN_DESCRIPTION_COLUMN_NUMBER and OUTPUT_FILE_PROTEIN_SEQUENCE_COLUMN_NUMBER
		strLineOut = "Protein Name" & ControlChars.Tab & _
		 "Percent Coverage" & ControlChars.Tab & _
		 "Protein Description" & ControlChars.Tab & _
		 "Non Unique Peptide Count" & ControlChars.Tab & _
		 "Unique Peptide Count" & ControlChars.Tab & _
		 "Protein Residue Count"

		If mOutputProteinSequence Then
			strLineOut &= ControlChars.Tab & "Protein Sequence"
		End If
		swOutputFile.WriteLine(strLineOut)

		htProteinIDLookup = New Hashtable

		' Populate udtPeptideStats() using hashtable mProteinPeptideStats
		If mTrackPeptideCounts Then

			' Initially reserve space for INITIAL_PROTEIN_COUNT_RESERVE proteins
			intPeptideStatsCount = 0
			ReDim udtPeptideStats(INITIAL_PROTEIN_COUNT_RESERVE - 1)

			myEnumerator = mProteinPeptideStats.GetEnumerator
			While myEnumerator.MoveNext()

				strKey = CStr(myEnumerator.Key)

				' strKey will be of the form 1234::K.ABCDEFR.A
				' Look for the first colon
				intColonIndex = strKey.IndexOf(":"c)

				If intColonIndex > 0 Then
					intProteinID = CInt(strKey.Substring(0, intColonIndex))

					' Look for intProteinID in htProteinIDLookup
					objItem = htProteinIDLookup.Item(intProteinID)
					If objItem Is Nothing Then
						' ID not found; so add it

						intTargetIndex = intPeptideStatsCount
						intPeptideStatsCount += 1

						htProteinIDLookup.Add(intProteinID, intTargetIndex)

						If intTargetIndex >= udtPeptideStats.Length Then
							' Reserve more space in the arrays
							ReDim Preserve udtPeptideStats(udtPeptideStats.Length * 2 - 1)
						End If
					Else
						' ID found; the target index is the value for the hash entry
						intTargetIndex = CInt(objItem)
					End If

					' Update the protein counts at intTargetIndex
					udtPeptideStats(intTargetIndex).UniquePeptideCount += 1
					udtPeptideStats(intTargetIndex).NonUniquePeptideCount += CInt(myEnumerator.Value)

				End If
			End While

			' Shrink udtPeptideStats
			If intPeptideStatsCount < udtPeptideStats.Length Then
				ReDim Preserve udtPeptideStats(intPeptideStatsCount - 1)
			End If
		Else
			intPeptideStatsCount = 0
			ReDim udtPeptideStats(-1)
		End If

		' Query the SqlLite DB to extract the protein information
		Dim SQLreader As System.Data.SQLite.SQLiteDataReader
		SQLreader = mProteinDataCache.GetSQLiteDataReader("SELECT * FROM udtProteinInfoType")
		While SQLreader.Read()
			' Column names in table udtProteinInfoType:
			'  Name TEXT, 
			'  Description TEXT, 
			'  Sequence TEXT, 
			'  UniqueSequenceID INTEGER, 
			'  PercentCoverage REAL, 
			'  NonUniquePeptideCount INTEGER, 
			'  UniquePeptideCount INTEGER

			intProteinID = CInt(SQLreader("UniqueSequenceID"))

			If mTrackPeptideCounts Then

				' Look for intProteinID in htProteinIDLookup
				objItem = htProteinIDLookup.Item(intProteinID)
				If objItem Is Nothing Then
					UniquePeptideCount = 0
					NonUniquePeptideCount = 0
				Else
					intTargetIndex = CInt(objItem)

					UniquePeptideCount = udtPeptideStats(intTargetIndex).UniquePeptideCount
					NonUniquePeptideCount = udtPeptideStats(intTargetIndex).NonUniquePeptideCount
				End If


			End If

			strLineOut = CStr(SQLreader("Name")) & ControlChars.Tab & _
			 Math.Round(CDbl(SQLreader("PercentCoverage")) * 100, 3) & ControlChars.Tab & _
			 CStr(SQLreader("Description")) & ControlChars.Tab & _
			 NonUniquePeptideCount & ControlChars.Tab & _
			 UniquePeptideCount & ControlChars.Tab & _
			 CStr(SQLreader("Sequence")).Length

			If mOutputProteinSequence Then
				strLineOut &= ControlChars.Tab & CStr(SQLreader("Sequence"))
			End If
			swOutputFile.WriteLine(strLineOut)

			If intProteinIndex Mod 25 = 0 Then
				UpdateProgress(intProteinIndex / CSng(mProteinDataCache.GetProteinCountCached()) * 100, _
				   eProteinCoverageProcessingSteps.WriteProteinCoverageFile)

			End If

			If mAbortProcessing Then Exit While

		End While

		' Close the SQL Reader
		SQLreader.Close()

		swOutputFile.Close()
		swOutputFile = Nothing

	End Sub

	Private Function DetermineLineTerminatorSize(ByVal strInputFilePath As String) As Integer
		Dim intByte As Integer

		Dim intTerminatorSize As Integer = 2

		Try
			' Open the input file and look for the first carriage return or line feed
			Using fsInFile As System.IO.FileStream = New System.IO.FileStream(strInputFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, IO.FileShare.ReadWrite)

				Do While fsInFile.Position < fsInFile.Length AndAlso fsInFile.Position < 100000

					intByte = fsInFile.ReadByte()

					If intByte = 10 Then
						' Found linefeed
						If fsInFile.Position < fsInFile.Length Then
							intByte = fsInFile.ReadByte()
							If intByte = 13 Then
								' LfCr
								intTerminatorSize = 2
							Else
								' Lf only
								intTerminatorSize = 1
							End If
						Else
							intTerminatorSize = 1
						End If
						Exit Do
					ElseIf intByte = 13 Then
						' Found carriage return
						If fsInFile.Position < fsInFile.Length Then
							intByte = fsInFile.ReadByte()
							If intByte = 10 Then
								' CrLf
								intTerminatorSize = 2
							Else
								' Cr only
								intTerminatorSize = 1
							End If
						Else
							intTerminatorSize = 1
						End If
						Exit Do
					End If

				Loop
			End Using

		Catch ex As Exception
			SetErrorMessage("Error in DetermineLineTerminatorSize: " & ex.Message)
			If Not mShowMessages Then Throw New System.Exception("Error in DetermineLineTerminatorSize", ex)
		End Try

		Return intTerminatorSize

	End Function

	''' <summary>
	''' Searches for proteins that contain peptide strPeptideSequence.  
	''' If strProteinNameForPeptide is blank or mSearchAllProteinsForPeptideSequence=True then searches all proteins
	''' </summary>
	''' <param name="strPeptideSequence">The peptide sequence to find</param>
	''' <param name="chPrefixResidue">The prefix for the peptide</param>
	''' <param name="chSuffixResidue">The suffix for the peptide</param>
	''' <param name="strProteinNameForPeptide">The protein to search; only used if mSearchAllProteinsForPeptideSequence=False</param>
	''' <remarks></remarks>
	Private Sub FindSequenceMatchForPeptide(ByVal strPeptideSequence As String, _
	 ByVal chPrefixResidue As Char, ByVal chSuffixResidue As Char, _
	 ByVal strProteinNameForPeptide As String)

		Static htPeptideList As Hashtable

		If htPeptideList Is Nothing Then
			htPeptideList = New Hashtable
		End If
		htPeptideList.Clear()

		htPeptideList.Add(chPrefixResidue & "."c & strPeptideSequence & "."c & chSuffixResidue, 1)

		FindSequenceMatchForPeptideList(htPeptideList, strProteinNameForPeptide)

	End Sub

	''' <summary>
	''' Searches for proteins that contain the peptides in htPeptideList  
	''' If strProteinNameForPeptide is blank or mSearchAllProteinsForPeptideSequence=True then searches all proteins
	''' Otherwise, only searches protein strProteinNameForPeptide
	''' </summary>
	''' <param name="htPeptideList">Hash table containing the peptides to search; peptides must be in the format Prefix.Peptide.Suffix where Prefix and Suffix are single characters; peptides are assumed to only contain letters (no symbols)</param>
	''' <param name="strProteinNameForPeptides">The protein to search; only used if mSearchAllProteinsForPeptideSequence=False</param>
	''' <remarks></remarks>
	Private Sub FindSequenceMatchForPeptideList(ByRef htPeptideList As Hashtable, _
	  ByVal strProteinNameForPeptides As String)

		Dim intProteinIndex As Integer
		Dim strKey As String
		Dim blnMatchFound As Boolean
		Dim blnMatchIsNew As Boolean
		Dim intStartResidue As Integer
		Dim intEndResidue As Integer

		Dim intStartIndex As Integer
		Dim intProteinCount As Integer

		Dim intExpectedPeptideIterations As Integer
		Dim intPeptideIterationsComplete As Integer

		Dim blnProteinUpdated(PROTEIN_CHUNK_COUNT - 1) As Boolean

		Dim strPeptideSequenceForKeySource As String
		Dim strPeptideSequenceForKey As String

		Dim objPeptideListEnum As IDictionaryEnumerator

		Dim strPeptideSequenceClean As String
		Dim strPeptideSequenceToSearchOn As String

		Dim chPrefixResidue As Char
		Dim chSuffixResidue As Char

		Try
			' Make sure strProteinNameForPeptide is a valid string
			If strProteinNameForPeptides Is Nothing Then
				strProteinNameForPeptides = String.Empty
			End If

			intExpectedPeptideIterations = CInt(Math.Ceiling(mProteinDataCache.GetProteinCountCached / PROTEIN_CHUNK_COUNT)) * htPeptideList.Count
			If intExpectedPeptideIterations < 1 Then intExpectedPeptideIterations = 1

			UpdateProgress("Finding matching proteins for peptide list", 0, _
			   eProteinCoverageProcessingSteps.SearchProteinsAgainstShortPeptides)

			intStartIndex = 0
			Do
				' Extract up to PROTEIN_CHUNK_COUNT proteins from the SQL Lite database
				' Store the information in the four local arrays
				intProteinCount = ReadProteinInfoChunk(intStartIndex, PROTEIN_CHUNK_COUNT, blnProteinUpdated, False)

				' Iterate through the peptides in htPeptideList
				objPeptideListEnum = htPeptideList.GetEnumerator

				Do While objPeptideListEnum.MoveNext

					' Retrieve the next peptide from htPeptideList
					' Use GetCleanPeptideSequence() to extract out the sequence, prefix, and suffix letters (we're setting blnRemoveSymbolCharacters to False since that should have been done before the peptides were stored in htPeptideList)
					' Make sure the peptide sequence has uppercase letters
					strPeptideSequenceClean = GetCleanPeptideSequence(CStr(objPeptideListEnum.Key), chPrefixResidue, chSuffixResidue, False).ToUpper

					If mMatchPeptidePrefixAndSuffixToProtein Then
						strPeptideSequenceForKeySource = ConstructPeptideSequenceForKey(strPeptideSequenceClean, chPrefixResidue, chSuffixResidue)
					Else
						strPeptideSequenceForKeySource = String.Copy(strPeptideSequenceClean)
					End If

					If mIgnoreILDifferences Then
						' Replace all L characters with I
						strPeptideSequenceForKey = strPeptideSequenceForKeySource.Replace("L"c, "I"c)

						strPeptideSequenceToSearchOn = strPeptideSequenceClean.Replace("L"c, "I"c)

						If chPrefixResidue = "L"c Then chPrefixResidue = "I"c
						If chSuffixResidue = "L"c Then chSuffixResidue = "I"c
					Else
						strPeptideSequenceToSearchOn = String.Copy(strPeptideSequenceClean)

						' I'm purposely not using String.Copy() here in order to obtain increased speed
						strPeptideSequenceForKey = strPeptideSequenceForKeySource
					End If

					' Search for strPeptideSequence in the protein sequences
					For intProteinIndex = 0 To intProteinCount - 1
						blnMatchFound = False

						If mSearchAllProteinsForPeptideSequence OrElse strProteinNameForPeptides.Length = 0 Then
							' Search through all Protein sequences and capitalize matches for Peptide Sequence

							strKey = CStr(mCachedProteinInfo(intProteinIndex).UniqueSequenceID) & "::" & strPeptideSequenceForKey
							mCachedProteinInfo(intProteinIndex).Sequence = CapitalizeMatchingProteinSequenceLetters(mCachedProteinInfo(intProteinIndex).Sequence, strPeptideSequenceToSearchOn, strKey, chPrefixResidue, chSuffixResidue, blnMatchFound, blnMatchIsNew, intStartResidue, intEndResidue)
						Else
							' Only search protein strProteinNameForPeptide
							If mCachedProteinInfo(intProteinIndex).Name = strProteinNameForPeptides Then

								' Define the peptide match key using the Unique Sequence ID, two colons, and the peptide sequence
								strKey = CStr(mCachedProteinInfo(intProteinIndex).UniqueSequenceID) & "::" & strPeptideSequenceForKey

								' Capitalize matching residues in sequence
								mCachedProteinInfo(intProteinIndex).Sequence = CapitalizeMatchingProteinSequenceLetters(mCachedProteinInfo(intProteinIndex).Sequence, strPeptideSequenceToSearchOn, strKey, chPrefixResidue, chSuffixResidue, blnMatchFound, blnMatchIsNew, intStartResidue, intEndResidue)
							End If
						End If

						If blnMatchFound Then
							If Not mSearchAllProteinsSkipCoverageComputationSteps Then
								blnProteinUpdated(intProteinIndex) = True
							End If

							If blnMatchIsNew Then
								If mSaveProteinToPeptideMappingFile Then
									WriteEntryToProteinToPeptideMappingFile(mCachedProteinInfo(intProteinIndex).Name, strPeptideSequenceForKeySource, intStartResidue, intEndResidue)
								End If

								If mSaveSourceDataPlusProteinsFile Then
									StorePeptideToProteinMatch(strPeptideSequenceClean, mCachedProteinInfo(intProteinIndex).Name)
								End If

							End If
						End If

					Next intProteinIndex

					intPeptideIterationsComplete += 1

					If intPeptideIterationsComplete Mod 10 = 0 Then
						UpdateProgress(CSng((intPeptideIterationsComplete / intExpectedPeptideIterations) * 100), _
						   eProteinCoverageProcessingSteps.SearchProteinsAgainstShortPeptides)

					End If
				Loop

				' Store the updated protein sequence information in the database
				UpdateSequenceDbDataValues(blnProteinUpdated, intProteinCount)

				' Increment intStartIndex to obtain the next chunk of proteins
				intStartIndex += PROTEIN_CHUNK_COUNT

			Loop While intStartIndex < mProteinDataCache.GetProteinCountCached()

		Catch ex As Exception
			SetErrorMessage("Error in FindSequenceMatch:" & ControlChars.NewLine & ex.Message)
			If Not mShowMessages Then Throw New System.Exception("Error in FindSequenceMatch", ex)
		End Try

	End Sub

	Private Sub UpdateSequenceDbDataValues(ByRef blnProteinUpdated() As Boolean, _
	   ByVal intProteinCount As Integer)
		Try
			If Not BooleanArrayContainsTrueEntries(blnProteinUpdated, intProteinCount) Then
				' All of the entries in blnProteinUpdated() are False; nothing to update
				Exit Sub
			End If

			' Store the updated protein sequences in the Sql Lite database
			Dim SQLconnect As System.Data.SQLite.SQLiteConnection
			SQLconnect = mProteinDataCache.ConnectToSqlLiteDB(True)

			Using dbTrans As System.Data.SQLite.SQLiteTransaction = SQLconnect.BeginTransaction()
				Using cmd As System.Data.SQLite.SQLiteCommand = SQLconnect.CreateCommand()

					' Create a parameterized Update query
					cmd.CommandText = "UPDATE udtProteinInfoType Set Sequence = ? Where UniqueSequenceID = ?"

					Dim SequenceFld As System.Data.SQLite.SQLiteParameter = cmd.CreateParameter
					Dim UniqueSequenceIDFld As System.Data.SQLite.SQLiteParameter = cmd.CreateParameter
					cmd.Parameters.Add(SequenceFld)
					cmd.Parameters.Add(UniqueSequenceIDFld)

					' Update each protein that has blnProteinUpdated(intProteinIndex) = True
					For intProteinIndex = 0 To intProteinCount - 1
						If blnProteinUpdated(intProteinIndex) Then
							UniqueSequenceIDFld.Value = mCachedProteinInfo(intProteinIndex).UniqueSequenceID
							SequenceFld.Value = mCachedProteinInfo(intProteinIndex).Sequence
							cmd.ExecuteNonQuery()
						End If
					Next
				End Using
				dbTrans.Commit()
			End Using

			' Close the Sql Reader
			SQLconnect.Close()
			SQLconnect.Dispose()

		Catch ex As Exception
			SetErrorMessage("Error in UpdateSequenceDbDataValues: " & ex.Message)
			If Not mShowMessages Then Throw New Exception("Error in UpdateSequenceDbDataValues", ex)
		End Try

	End Sub

	Public Shared Function GetAppFolderPath() As String
		' Could use Application.StartupPath, but .GetExecutingAssembly is better
		Return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
	End Function

	Public Shared Function GetCleanPeptideSequence(ByVal strPeptideSequence As String, _
	  ByRef chPrefixResidue As Char, _
	  ByRef chSuffixResidue As Char, _
	  ByVal blnRemoveSymbolCharacters As Boolean) As String

		Static reReplaceSymbols As System.Text.RegularExpressions.Regex = New System.Text.RegularExpressions.Regex("[^A-Za-z]", System.Text.RegularExpressions.RegexOptions.Compiled)

		chPrefixResidue = Nothing
		chSuffixResidue = Nothing

		If strPeptideSequence.Length >= 4 Then
			If strPeptideSequence.Chars(1) = "."c AndAlso strPeptideSequence.Chars(strPeptideSequence.Length - 2) = "."c Then
				chPrefixResidue = strPeptideSequence.Chars(0)
				chSuffixResidue = strPeptideSequence.Chars(strPeptideSequence.Length - 1)
				strPeptideSequence = strPeptideSequence.Substring(2, strPeptideSequence.Length - 4)
			End If
		End If

		If blnRemoveSymbolCharacters Then
			strPeptideSequence = reReplaceSymbols.Replace(strPeptideSequence, String.Empty)
		End If

		Return strPeptideSequence

	End Function

	Public Function GetErrorMessage() As String
		' Returns String.Empty if no error

		Dim strMessage As String

		Select Case Me.ErrorCode
			Case eProteinCoverageErrorCodes.NoError
				strMessage = String.Empty
			Case eProteinCoverageErrorCodes.InvalidInputFilePath
				strMessage = "Invalid input file path"
				''Case eProteinCoverageErrorCodes.InvalidOutputFolderPath
				''    strMessage = "Invalid output folder path"
				''Case eProteinCoverageErrorCodes.ParameterFileNotFound
				''    strMessage = "Parameter file not found"

				''Case eProteinCoverageErrorCodes.ErrorReadingInputFile
				''    strMessage = "Error reading input file"
				''Case eProteinCoverageErrorCodes.ErrorCreatingOutputFiles
				''    strMessage = "Error creating output files"

			Case eProteinCoverageErrorCodes.ErrorReadingParameterFile
				strMessage = "Invalid parameter file"

			Case eProteinCoverageErrorCodes.FilePathError
				strMessage = "General file path error"
			Case eProteinCoverageErrorCodes.UnspecifiedError
				strMessage = "Unspecified error"
			Case Else
				' This shouldn't happen
				strMessage = "Unknown error state"
		End Select

		If mErrorMessage.Length > 0 Then
			If strMessage.Length > 0 Then
				strMessage &= "; "
			End If
			strMessage &= mErrorMessage
		End If

		Return strMessage
	End Function

	Protected Function GetOutputFolderPath(ByVal strOutputFolderPath As String, ByVal strOutputFilePath As String) As String
		' Returns the folder given by strOutputFolderPath if it is valid
		' Otherwise, returns the folder in which strOutputFilePath resides
		' If an error, or unable to determine a folder, returns the path the application is in

		Try
			If Not strOutputFolderPath Is Nothing AndAlso strOutputFolderPath.Length > 0 Then
				strOutputFolderPath = System.IO.Path.GetFullPath(strOutputFolderPath)
			Else
				strOutputFolderPath = System.IO.Path.GetDirectoryName(strOutputFilePath)
			End If

			If Not System.IO.Directory.Exists(strOutputFolderPath) Then
				System.IO.Directory.CreateDirectory(strOutputFolderPath)
			End If

		Catch ex As Exception
			strOutputFolderPath = GetAppFolderPath()
		End Try

		Return strOutputFolderPath

	End Function

	Private Sub GetPercentCoverage(ByVal strProteinNameForPeptide As String, ByVal strPeptideSequence As String)
		Dim intIndex As Integer
		Dim intCapitalLetterCount As Integer
		Dim charArray As Char()
		Dim character As Char

		Dim intStartIndex As Integer
		Dim intProteinCount As Integer

		Dim blnProteinUpdated(PROTEIN_CHUNK_COUNT - 1) As Boolean

		UpdateProgress("Computing percent coverage", 0, _
		   eProteinCoverageProcessingSteps.ComputePercentCoverage)

		intStartIndex = 0
		Do
			' Extract up to PROTEIN_CHUNK_COUNT proteins from the Sql Lite database
			' Store the information in the four local arrays
			intProteinCount = ReadProteinInfoChunk(intStartIndex, PROTEIN_CHUNK_COUNT, blnProteinUpdated, False)

			For intProteinIndex = 0 To intProteinCount - 1

				If Not mCachedProteinInfo(intProteinIndex).Sequence Is Nothing Then
					charArray = mCachedProteinInfo(intProteinIndex).Sequence.ToCharArray()
					intCapitalLetterCount = 0
					For Each character In charArray
						If Char.IsUpper(character) Then intCapitalLetterCount += 1
					Next

					If intCapitalLetterCount > mCachedProteinInfo(intProteinIndex).Sequence.Length Then
						Dim temp As String
						temp = "error"
					Else
						mCachedProteinInfo(intProteinIndex).PercentCoverage = intCapitalLetterCount / mCachedProteinInfo(intProteinIndex).Sequence.Length
						If mCachedProteinInfo(intProteinIndex).PercentCoverage > 0 Then
							blnProteinUpdated(intProteinIndex) = True
						End If
					End If
				End If

				If intIndex Mod 100 = 0 Then
					UpdateProgress(intIndex / CSng(mProteinDataCache.GetProteinCountCached()) * 100, _
					   eProteinCoverageProcessingSteps.ComputePercentCoverage)
				End If
			Next

			UpdatePercentCoveragesDbDataValues(blnProteinUpdated, intProteinCount)

			' Increment intStartIndex to obtain the next chunk of proteins
			intStartIndex += PROTEIN_CHUNK_COUNT

		Loop While intStartIndex < mProteinDataCache.GetProteinCountCached()

	End Sub

	Private Sub UpdatePercentCoveragesDbDataValues(ByRef blnProteinUpdated() As Boolean, _
	 ByVal intProteinCount As Integer)
		Try
			If Not BooleanArrayContainsTrueEntries(blnProteinUpdated, intProteinCount) Then
				' All of the entries in blnProteinUpdated() are False; nothing to update
				Exit Sub
			End If

			' Store the updated protein coverage values in the Sql Lite database
			Dim SQLconnect As System.Data.SQLite.SQLiteConnection
			SQLconnect = mProteinDataCache.ConnectToSqlLiteDB(True)

			Using dbTrans As System.Data.SQLite.SQLiteTransaction = SQLconnect.BeginTransaction()
				Using cmd As System.Data.SQLite.SQLiteCommand = SQLconnect.CreateCommand()

					' Create a parameterized Update query
					cmd.CommandText = "UPDATE udtProteinInfoType Set PercentCoverage = ? Where UniqueSequenceID = ?"

					Dim PercentCoverageFld As System.Data.SQLite.SQLiteParameter = cmd.CreateParameter
					Dim UniqueSequenceIDFld As System.Data.SQLite.SQLiteParameter = cmd.CreateParameter
					cmd.Parameters.Add(PercentCoverageFld)
					cmd.Parameters.Add(UniqueSequenceIDFld)

					' Update each protein that has blnProteinUpdated(intProteinIndex) = True
					For intProteinIndex = 0 To intProteinCount - 1
						If blnProteinUpdated(intProteinIndex) Then
							UniqueSequenceIDFld.Value = mCachedProteinInfo(intProteinIndex).UniqueSequenceID
							PercentCoverageFld.Value = mCachedProteinInfo(intProteinIndex).PercentCoverage
							cmd.ExecuteNonQuery()
						End If
					Next
				End Using
				dbTrans.Commit()
			End Using

			' Close the Sql Reader
			SQLconnect.Close()
			SQLconnect.Dispose()

		Catch ex As Exception
			SetErrorMessage("Error in UpdatePercentCoveragesDbDataValues: " & ex.Message)
			If Not mShowMessages Then Throw New Exception("Error in UpdatePercentCoveragesDbDataValues", ex)
		End Try

	End Sub

	Private Sub InitializeVariables()
		mAbortProcessing = False
		mShowMessages = True
		mErrorMessage = String.Empty

		mProteinInputFilePath = String.Empty
		mResultsFilePath = String.Empty

		mProteinDataCache = New clsProteinFileDataCache
		mProteinDataCache.ShowMessages = False
		mCachedProteinInfoStartIndex = -1

		mPeptideFileSkipFirstLine = False
		mPeptideInputFileDelimiter = ControlChars.Tab
		mPeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence

		mOutputProteinSequence = True
		mSearchAllProteinsForPeptideSequence = True
		mSearchAllProteinsSkipCoverageComputationSteps = False
		mUseLeaderSequenceHashTable = True

		mSaveProteinToPeptideMappingFile = False
		mProteinToPeptideMappingFilePath = String.Empty

		mSaveSourceDataPlusProteinsFile = False

		mTrackPeptideCounts = True
		mRemoveSymbolCharacters = True
		mMatchPeptidePrefixAndSuffixToProtein = False
		mIgnoreILDifferences = False

		' Define the percent complete values to use for the start of each processing step

		ReDim mPercentCompleteStartLevels(PERCENT_COMPLETE_LEVEL_COUNT)

		mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.Starting) = 0
		mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.CacheProteins) = 1
		mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.DetermineShortestPeptideLength) = 45
		mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.CachePeptides) = 50
		mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.SearchProteinsUsingLeaderSequences) = 55
		mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.SearchProteinsAgainstShortPeptides) = 90
		mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.ComputePercentCoverage) = 95
		mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.WriteProteinCoverageFile) = 97
		mPercentCompleteStartLevels(eProteinCoverageProcessingSteps.SaveAllProteinsVersionOfInputFile) = 98
		mPercentCompleteStartLevels(PERCENT_COMPLETE_LEVEL_COUNT) = 100

	End Sub

	Public Function LoadParameterFileSettings(ByVal strParameterFilePath As String) As Boolean

		Dim objSettingsFile As XmlSettingsFileAccessor
		Dim strAlternateFilePath As String

		Try

			If strParameterFilePath Is Nothing OrElse strParameterFilePath.Length = 0 Then
				' No parameter file specified; default settings will be used
				Return True
			End If

			If Not System.IO.File.Exists(strParameterFilePath) Then
				' See if strParameterFilePath points to a file in the same directory as the application
				strAlternateFilePath = System.IO.Path.Combine(GetAppFolderPath(), System.IO.Path.GetFileName(strParameterFilePath))
				If Not System.IO.File.Exists(strAlternateFilePath) Then
					' Parameter file still not found
					SetErrorMessage("Parameter file not found: " & strParameterFilePath)
					If Not mShowMessages Then Throw New System.Exception(mErrorMessage)

					Return False
				Else
					strParameterFilePath = String.Copy(strAlternateFilePath)
				End If
			End If

			objSettingsFile = New XmlSettingsFileAccessor

			If objSettingsFile.LoadSettings(strParameterFilePath) Then

				If Not objSettingsFile.SectionPresent(XML_SECTION_PROCESSING_OPTIONS) Then
					If mShowMessages Then
						Console.WriteLine("The node '<section name=""" & XML_SECTION_PROCESSING_OPTIONS & """> was not found in the parameter file: " & strParameterFilePath)
					End If
				Else
					Me.OutputProteinSequence = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "OutputProteinSequence", Me.OutputProteinSequence)
					Me.SearchAllProteinsForPeptideSequence = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "SearchAllProteinsForPeptideSequence", Me.SearchAllProteinsForPeptideSequence)
					Me.SaveProteinToPeptideMappingFile = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "SaveProteinToPeptideMappingFile", Me.SaveProteinToPeptideMappingFile)
					Me.SaveSourceDataPlusProteinsFile = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "mSaveSourceDataPlusProteinsFile", Me.SaveSourceDataPlusProteinsFile)

					Me.TrackPeptideCounts = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "TrackPeptideCounts", Me.TrackPeptideCounts)
					Me.RemoveSymbolCharacters = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "RemoveSymbolCharacters", Me.RemoveSymbolCharacters)
					Me.MatchPeptidePrefixAndSuffixToProtein = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "MatchPeptidePrefixAndSuffixToProtein", Me.MatchPeptidePrefixAndSuffixToProtein)
					Me.IgnoreILDifferences = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "IgnoreILDifferences", Me.IgnoreILDifferences)

					Me.PeptideFileSkipFirstLine = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideFileSkipFirstLine", Me.PeptideFileSkipFirstLine)
					Me.PeptideInputFileDelimiter = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideInputFileDelimiter", Me.PeptideInputFileDelimiter).Chars(0)
					Me.PeptideFileFormatCode = CType(objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "PeptideFileFormatCode", CInt(Me.PeptideFileFormatCode)), ePeptideFileColumnOrderingCode)

					mProteinDataCache.DelimitedFileSkipFirstLine = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "ProteinFileSkipFirstLine", mProteinDataCache.DelimitedFileSkipFirstLine)
					mProteinDataCache.DelimitedFileDelimiter = objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileDelimiter", mProteinDataCache.DelimitedFileDelimiter).Chars(0)
					mProteinDataCache.DelimitedFileFormatCode = CType(objSettingsFile.GetParam(XML_SECTION_PROCESSING_OPTIONS, "DelimitedProteinFileFormatCode", CInt(mProteinDataCache.DelimitedFileFormatCode)), ProteinFileReader.DelimitedFileReader.eDelimitedFileFormatCode)

				End If

			Else
				SetErrorMessage("Error calling objSettingsFile.LoadSettings for " & strParameterFilePath)
				Return False
			End If

		Catch ex As Exception
			SetErrorMessage("Error in LoadParameterFileSettings:" & ex.Message)
			SetErrorCode(eProteinCoverageErrorCodes.ErrorReadingParameterFile)
			If Not mShowMessages Then Throw New System.Exception("Error in LoadParameterFileSettings", ex)
			Return False
		End Try

		Return True

	End Function

	Private Function LookupColumnDelimiterChar(ByVal intDelimiterIndex As Integer, ByVal strCustomDelimiter As String, ByVal strDefaultDelimiter As Char) As Char

		Dim strDelimiter As String

		Select Case intDelimiterIndex
			Case DelimiterCharConstants.Space
				strDelimiter = " "
			Case DelimiterCharConstants.Tab
				strDelimiter = ControlChars.Tab
			Case DelimiterCharConstants.Comma
				strDelimiter = ","
			Case Else
				' Includes DelimiterCharConstants.Other
				strDelimiter = String.Copy(strCustomDelimiter)
		End Select

		If strDelimiter Is Nothing OrElse strDelimiter.Length = 0 Then
			strDelimiter = String.Copy(strDefaultDelimiter)
		End If

		Try
			Return strDelimiter.Chars(0)
		Catch ex As Exception
			Return ControlChars.Tab
		End Try

	End Function

	Private Function ParsePeptideInputFile(ByVal strPeptideInputFilePath As String, _
	   ByVal strOutputFolderPath As String, _
	   ByRef strProteinToPeptideMappingFilePath As String) As Boolean

		Const MAX_SHORT_PEPTIDES_TO_CACHE As Integer = 1000000
		Dim strLineIn As String

		Dim chSepChars() As Char
		Dim strSplitLine As String()
		Dim strProteinName As String = String.Empty
		Dim strPeptideSequence As String = String.Empty
		Dim strPeptideSequenceToCache As String = String.Empty

		Dim strProgressMessageBase As String

		Dim chPrefixResidue As Char
		Dim chSuffixResidue As Char

		Dim intCurrentLine As Integer
		Dim intInvalidLineCount As Integer
		Dim bytesRead As Long = 0

		Dim intColumnNumWithPeptideSequence As Integer

		Dim blnSuccess As Boolean
		Dim blnValidLine As Boolean

		Dim intTerminatorSize As Integer

		Dim htShortPeptideCache As Hashtable

		Try
			' Initialize chSepChars
			ReDim chSepChars(0)
			chSepChars(0) = mPeptideInputFileDelimiter

			' Initialize some hash tables
			htShortPeptideCache = New Hashtable

			If mProteinPeptideStats Is Nothing Then
				mProteinPeptideStats = New Hashtable
			Else
				mProteinPeptideStats.Clear()
			End If

			If mPeptideToProteinMapResults Is Nothing Then
				mPeptideToProteinMapResults = New Generic.Dictionary(Of String, Generic.List(Of String))
			Else
				mPeptideToProteinMapResults.Clear()
			End If


			If Not System.IO.File.Exists(strPeptideInputFilePath) Then
				SetErrorMessage("File not found: " & strPeptideInputFilePath)

				If Not mShowMessages Then
					Throw New Exception(mErrorMessage)
					mAbortProcessing = True
				End If
			Else

				strProgressMessageBase = "Reading peptides from " & System.IO.Path.GetFileName(strPeptideInputFilePath)
				If mUseLeaderSequenceHashTable Then
					strProgressMessageBase &= " and finding leader sequences"
				Else
					If Not mSearchAllProteinsSkipCoverageComputationSteps Then
						strProgressMessageBase &= " and computing coverage"
					End If
				End If

				mProgressStepDescription = String.Copy(strProgressMessageBase)
				Console.WriteLine()
				Console.WriteLine()
				Console.WriteLine("Parsing " & System.IO.Path.GetFileName(strPeptideInputFilePath))
				Console.WriteLine(mProgressStepDescription)

				UpdateProgress(mProgressStepDescription, 0, _
				   eProteinCoverageProcessingSteps.DetermineShortestPeptideLength)

				' Open the file and read, at most, the first 100,000 characters to see if it contains CrLf or just Lf
				intTerminatorSize = DetermineLineTerminatorSize(strPeptideInputFilePath)

				' Possibly open the file and read the first few line to make sure the number of columns is appropriate
				blnSuccess = ValidateColumnCountInInputFile(strPeptideInputFilePath)
				If Not blnSuccess Then
					Return False
				End If

				If mUseLeaderSequenceHashTable Then
					' Determine the shortest peptide present in the input file
					' This is a fast process that involves checking the length of each sequence in the input file

					UpdateProgress("Determining the shortest peptide in the input file", 0, _
					   eProteinCoverageProcessingSteps.DetermineShortestPeptideLength)

					If mLeaderSequenceCache Is Nothing Then
						mLeaderSequenceCache = New clsLeaderSequenceCache
					Else
						mLeaderSequenceCache.InitializeVariables()
					End If
					mLeaderSequenceCache.IgnoreILDifferences = mIgnoreILDifferences

					Select Case mPeptideFileColumnOrdering
						Case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
							intColumnNumWithPeptideSequence = 2
						Case Else
							' Includes ePeptideFileColumnOrderingCode.SequenceOnly
							intColumnNumWithPeptideSequence = 1
					End Select

					blnSuccess = mLeaderSequenceCache.DetermineShortestPeptideLengthInFile(strPeptideInputFilePath, intTerminatorSize, mPeptideFileSkipFirstLine, mPeptideInputFileDelimiter, intColumnNumWithPeptideSequence)

					If mAbortProcessing Then
						Return False
					Else
						strProgressMessageBase &= " (leader seq length = " & mLeaderSequenceCache.LeaderSequenceMinimumLength.ToString & ")"

						UpdateProgress(strProgressMessageBase)
					End If
				End If

				' Open the peptide file and read in the lines
				Using srInFile As System.IO.StreamReader = New System.IO.StreamReader(New System.IO.FileStream(strPeptideInputFilePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))

					' Create the protein to peptide match details file
					mProteinToPeptideMappingFilePath = System.IO.Path.Combine(GetOutputFolderPath(strOutputFolderPath, strPeptideInputFilePath), System.IO.Path.GetFileNameWithoutExtension(strPeptideInputFilePath) & FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING)

					If mSaveProteinToPeptideMappingFile Then
						strProteinToPeptideMappingFilePath = String.Copy(mProteinToPeptideMappingFilePath)

						UpdateProgress("Creating the protein to peptide mapping file: " & System.IO.Path.GetFileName(mProteinToPeptideMappingFilePath))

						mProteinToPeptideMappingOutputFile = New System.IO.StreamWriter(New System.IO.FileStream(mProteinToPeptideMappingFilePath, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.Read))
						mProteinToPeptideMappingOutputFile.AutoFlush = True

						mProteinToPeptideMappingOutputFile.WriteLine("Protein Name" & ControlChars.Tab & "Peptide Sequence" & ControlChars.Tab & "Residue Start" & ControlChars.Tab & "Residue End")
					Else
						strProteinToPeptideMappingFilePath = String.Empty
					End If

					intCurrentLine = 1
					Do While srInFile.Peek > -1
						If mAbortProcessing Then Exit Do

						strLineIn = srInFile.ReadLine()
						bytesRead += strLineIn.Length + intTerminatorSize

						strLineIn = strLineIn.Trim

						If intCurrentLine Mod 500 = 0 Then
							UpdateProgress("Reading peptide input file", CSng((bytesRead / srInFile.BaseStream.Length) * 100), _
							   eProteinCoverageProcessingSteps.CachePeptides)
						End If

						If intCurrentLine = 1 AndAlso mPeptideFileSkipFirstLine Then
							' do nothing, skip the first line
						ElseIf strLineIn.Length > 0 Then

							Try
								blnValidLine = False

								' Split the line, but for efficiency purposes, only parse out the first 3 columns
								strSplitLine = strLineIn.Split(chSepChars, 3)

								Select Case mPeptideFileColumnOrdering
									Case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
										strProteinName = strSplitLine(0)

										If strSplitLine.Length > 1 AndAlso Not strSplitLine(1) Is Nothing AndAlso strSplitLine(1).Length > 0 Then
											strPeptideSequence = strSplitLine(1)
											blnValidLine = True
										End If
									Case Else
										' Includes ePeptideFileColumnOrderingCode.SequenceOnly
										strPeptideSequence = strSplitLine(0)
										strProteinName = String.Empty
										blnValidLine = True
								End Select

							Catch ex As Exception
								blnValidLine = False
							End Try

							If blnValidLine Then
								' Check for and remove prefix and suffix letters
								' Also possibly remove symbol characters

								strPeptideSequence = GetCleanPeptideSequence(strPeptideSequence, chPrefixResidue, chSuffixResidue, mRemoveSymbolCharacters)

								If mUseLeaderSequenceHashTable AndAlso _
								 strPeptideSequence.Length >= mLeaderSequenceCache.LeaderSequenceMinimumLength Then

									If mLeaderSequenceCache.CachedPeptideCount >= clsLeaderSequenceCache.MAX_LEADER_SEQUENCE_COUNT Then
										' Need to step through the proteins and match them to the data in mLeaderSequenceCache
										SearchProteinsUsingLeaderSequences()
										mLeaderSequenceCache.InitializeCachedPeptides()
									End If

									mLeaderSequenceCache.CachePeptide(strPeptideSequence, strProteinName, chPrefixResidue, chSuffixResidue)
								Else
									' Either mUseLeaderSequenceHashTable is false, or the peptide sequence is less than MINIMUM_LEADER_SEQUENCE_LENGTH residues long
									' We must search all proteins for the given peptide

									' Cache the short peptides in htShortPeptideCache
									If htShortPeptideCache.Count >= MAX_SHORT_PEPTIDES_TO_CACHE Then
										' Step through the proteins and match them to the data in htShortPeptideCache
										SearchProteinsUsingCachedPeptides(htShortPeptideCache)
										htShortPeptideCache.Clear()
									End If

									strPeptideSequenceToCache = chPrefixResidue & "." & strPeptideSequence & "." & chSuffixResidue
									If htShortPeptideCache.Contains(strPeptideSequenceToCache) Then
										' Increment the peptide count
										htShortPeptideCache(strPeptideSequenceToCache) = CInt(htShortPeptideCache(strPeptideSequenceToCache)) + 1
									Else
										htShortPeptideCache.Add(strPeptideSequenceToCache, 1)
									End If
								End If

							Else
								intInvalidLineCount += 1
							End If

						End If
						intCurrentLine += 1

					Loop

				End Using

				If mUseLeaderSequenceHashTable Then
					' Step through the proteins and match them to the data in mLeaderSequenceCache
					If mLeaderSequenceCache.CachedPeptideCount > 0 Then
						SearchProteinsUsingLeaderSequences()
					End If
				End If

				' Step through the proteins and match them to the data in htShortPeptideCache
				SearchProteinsUsingCachedPeptides(htShortPeptideCache)

				If Not mAbortProcessing And Not mSearchAllProteinsSkipCoverageComputationSteps Then
					' Compute the residue coverage percent for each protein
					GetPercentCoverage(strProteinName, strPeptideSequence)
				End If

				If Not mProteinToPeptideMappingOutputFile Is Nothing Then
					mProteinToPeptideMappingOutputFile.Close()
					mProteinToPeptideMappingOutputFile = Nothing
				End If

				If mSaveSourceDataPlusProteinsFile Then
					' Create a new version of the input file, but with all of the proteins listed
					SaveDataPlusAllProteinsFile(strPeptideInputFilePath, strOutputFolderPath, chSepChars, intTerminatorSize)

				End If


				If intInvalidLineCount > 0 And mShowMessages Then
					Select Case mPeptideFileColumnOrdering
						Case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
							Console.WriteLine("Warning, found " & intInvalidLineCount & " lines that did not have two columns (Protein Name and Peptide Sequence).  Those line(s) have been skipped.")
						Case Else
							Console.WriteLine("Warning, found " & intInvalidLineCount & " lines that did not contain a peptide sequence.  Those line(s) have been skipped.")
					End Select
				End If

			End If

		Catch ex As Exception
			SetErrorMessage("Error in ParsePeptideInputFile: " & ex.Message)
			If Not mShowMessages Then Throw New Exception("Error in ParsePeptideInputFile", ex)
		End Try

		Return Not mAbortProcessing

	End Function

	Private Function ParseProteinInputFile() As Boolean
		Dim blnSuccess As Boolean = False

		Try
			mProgressStepDescription = "Reading protein input file"

			With mProteinDataCache

				' Protein file options
				If System.IO.Path.GetExtension(mProteinInputFilePath).ToLower = ".fasta" Then
					mProteinDataCache.AssumeFastaFile = True
				ElseIf System.IO.Path.GetExtension(mProteinInputFilePath).ToLower = ".txt" Then
					mProteinDataCache.AssumeDelimitedFile = True
				Else
					mProteinDataCache.AssumeFastaFile = False
				End If

				If mSearchAllProteinsSkipCoverageComputationSteps Then
					' Make sure all of the protein sequences are uppercase
					.ChangeProteinSequencesToLowercase = False
					.ChangeProteinSequencesToUppercase = True
				Else
					' Make sure all of the protein sequences are lowercase
					.ChangeProteinSequencesToLowercase = True
					.ChangeProteinSequencesToUppercase = False
				End If

				blnSuccess = .ParseProteinFile(mProteinInputFilePath)

				If Not blnSuccess Then
					SetErrorMessage("Error parsing protein file: " & .StatusMessage)
				Else
					If .GetProteinCountCached = 0 Then
						blnSuccess = False
						SetErrorMessage("Error parsing protein file: no protein entries were found in the file.  Please verify that the column order defined for the proteins file is correct.")
					End If
				End If
			End With

		Catch ex As Exception
			SetErrorMessage("Error in ParseProteinInputFile: " & ex.Message)
			If Not mShowMessages Then Throw New Exception("Error in ParseProteinInputFile", ex)
		End Try

		Return blnSuccess
	End Function

	Public Function ProcessFile(ByVal strInputFilePath As String, _
	 ByVal strOutputFolderPath As String, _
	 ByVal strParameterFilePath As String, _
	 ByVal blnResetErrorCode As Boolean) As Boolean

		Dim strProteinToPeptideMappingFilePath As String = String.Empty

		Return ProcessFile(strInputFilePath, strOutputFolderPath, strParameterFilePath, blnResetErrorCode, strProteinToPeptideMappingFilePath)
	End Function

	Public Function ProcessFile(ByVal strInputFilePath As String, _
	 ByVal strOutputFolderPath As String, _
	 ByVal strParameterFilePath As String, _
	 ByVal blnResetErrorCode As Boolean, _
	 ByRef strProteinToPeptideMappingFilePath As String) As Boolean

		Dim blnSuccess As Boolean

		If blnResetErrorCode Then
			SetErrorCode(eProteinCoverageErrorCodes.NoError)
		End If

		Console.WriteLine("Initializing")

		If Not LoadParameterFileSettings(strParameterFilePath) Then
			SetErrorMessage("Parameter file load error: " & strParameterFilePath)

			If mErrorCode = eProteinCoverageErrorCodes.NoError Then
				SetErrorCode(eProteinCoverageErrorCodes.ErrorReadingParameterFile)
			End If

			Return False
		End If

		Try
			mCachedProteinInfoStartIndex = -1
			With mProteinDataCache
				.ShowMessages = mShowMessages
				.RemoveSymbolCharacters = Me.RemoveSymbolCharacters
				.IgnoreILDifferences = Me.IgnoreILDifferences
			End With

			If String.IsNullOrWhiteSpace(strInputFilePath) Then
				Console.WriteLine("Input file name is empty")
				SetErrorCode(eProteinCoverageErrorCodes.InvalidInputFilePath)
				Return False
			End If

			' Note that the results file path will be auto-defined in CreateProteinCoverageFile
			mResultsFilePath = String.Empty

			If String.IsNullOrWhiteSpace(mProteinInputFilePath) Then
				SetErrorMessage("Protein file name is empty")
				SetErrorCode(eProteinCoverageErrorCodes.InvalidInputFilePath)
				Return False
			ElseIf Not IO.File.Exists(mProteinInputFilePath) Then
				SetErrorMessage("Protein input file not found: " & mProteinInputFilePath)
				SetErrorCode(eProteinCoverageErrorCodes.InvalidInputFilePath)
				Return False
			End If

			mProteinDataCache.DeleteSQLiteDBFile()

			' First read the protein input file
			mProgressStepDescription = "Reading protein input file: " & System.IO.Path.GetFileName(mProteinInputFilePath)
			Console.WriteLine(mProgressStepDescription)
			UpdateProgress(mProgressStepDescription, 0, eProteinCoverageProcessingSteps.CacheProteins)

			blnSuccess = ParseProteinInputFile()

			If blnSuccess Then
				Console.WriteLine()
				mProgressStepDescription = "Complete reading protein input file: " & System.IO.Path.GetFileName(mProteinInputFilePath)
				Console.WriteLine(mProgressStepDescription)
				UpdateProgress(mProgressStepDescription, 100, eProteinCoverageProcessingSteps.CacheProteins)

				' Now read the peptide input file
				blnSuccess = ParsePeptideInputFile(strInputFilePath, strOutputFolderPath, strProteinToPeptideMappingFilePath)

				If blnSuccess And Not mSearchAllProteinsSkipCoverageComputationSteps Then
					CreateProteinCoverageFile(strInputFilePath, strOutputFolderPath)
				End If

				UpdateProgress("Processing complete; deleting the temporary SqlLite database", 100, _
				   eProteinCoverageProcessingSteps.WriteProteinCoverageFile)

				'All done; delete the temporary SqlLite database
				mProteinDataCache.DeleteSQLiteDBFile()

				UpdateProgress("Done")

				mProteinPeptideStats = Nothing
			End If

		Catch ex As Exception
			SetErrorMessage("Error in ProcessFile:" & ControlChars.NewLine & ex.Message)
			If Not mShowMessages Then Throw New System.Exception("Error in ProcessFile", ex)
			blnSuccess = False
		End Try

		Return blnSuccess

	End Function

	''' <summary>
	''' Read the next chunk of proteins from the database (SequenceID, ProteinName, ProteinSequence)
	''' </summary>
	''' <returns>The number of records read</returns>
	''' <remarks></remarks>
	Private Function ReadProteinInfoChunk(ByVal intStartIndex As Integer, ByVal intProteinCountToRead As Integer, _
		 ByRef blnProteinUpdated() As Boolean, _
		 ByVal blnForceReload As Boolean) As Integer

		' We use a SQLLite database to store the protein sequences (to avoid running out of memory when parsing large protein lists)
		' However, we will store the most recently loaded peptides in mCachedProteinInfoCount() and 
		' will only reload them if intStartIndex is different than mCachedProteinInfoStartIndex

		If blnProteinUpdated Is Nothing Then
			ReDim blnProteinUpdated(PROTEIN_CHUNK_COUNT - 1)
		End If

		' Reset the values in blnProteinUpdated()
		Array.Clear(blnProteinUpdated, 0, blnProteinUpdated.Length)

		If Not blnForceReload AndAlso _
		   mCachedProteinInfoStartIndex >= 0 AndAlso _
		   mCachedProteinInfoStartIndex = intStartIndex AndAlso _
		   Not mCachedProteinInfo Is Nothing Then

			' The data loaded in memory is already valid; no need to reload
			Return mCachedProteinInfoCount
		End If

		' Extract up to PROTEIN_CHUNK_COUNT proteins from the Sql Lite database
		' Store the information in the four local arrays

		Dim strSqlCommand As String
		strSqlCommand = " SELECT UniqueSequenceID, Name, Description, Sequence, PercentCoverage" & _
		 " FROM udtProteinInfoType" & _
		 " WHERE UniqueSequenceID BETWEEN " & CStr(intStartIndex) & " AND " & CStr(intStartIndex + PROTEIN_CHUNK_COUNT - 1)

		Dim SQLreader As System.Data.SQLite.SQLiteDataReader
		SQLreader = mProteinDataCache.GetSQLiteDataReader(strSqlCommand)

		mCachedProteinInfoStartIndex = intStartIndex
		mCachedProteinInfoCount = 0
		If mCachedProteinInfo Is Nothing Then
			ReDim mCachedProteinInfo(PROTEIN_CHUNK_COUNT - 1)
		End If

		While SQLreader.Read
			With mCachedProteinInfo(mCachedProteinInfoCount)
				.UniqueSequenceID = CInt(SQLreader("UniqueSequenceID"))
				.Description = CStr(SQLreader("Description"))
				.Name = CStr(SQLreader("Name"))
				.Sequence = CStr(SQLreader("Sequence"))
				.PercentCoverage = CDbl(SQLreader("PercentCoverage"))
			End With

			mCachedProteinInfoCount += 1
		End While

		' Close the Sql Reader
		SQLreader.Close()

		Return mCachedProteinInfoCount

	End Function

	Private Sub SaveDataPlusAllProteinsFile(ByVal strPeptideInputFilePath As String, ByVal strOutputFolderPath As String, ByVal chSepChars() As Char, ByVal intTerminatorSize As Integer)

		Dim strDataPlusAllProteinsFile As String

		Dim strLineIn As String
		Dim strSplitLine As String()
		Dim strProteinName As String = String.Empty
		Dim strPeptideSequence As String = String.Empty

		Dim chPrefixResidue As Char
		Dim chSuffixResidue As Char

		Dim intCurrentLine As Integer
		Dim bytesRead As Long = 0
		Dim blnValidLine As Boolean

		Dim lstProteins As Generic.List(Of String) = Nothing

		Try
			strDataPlusAllProteinsFile = System.IO.Path.Combine(GetOutputFolderPath(strOutputFolderPath, strPeptideInputFilePath), System.IO.Path.GetFileNameWithoutExtension(strPeptideInputFilePath) & FILENAME_SUFFIX_SOURCE_PLUS_ALL_PROTEINS)
			UpdateProgress("Creating the data plus all-proteins output file: " & System.IO.Path.GetFileName(strDataPlusAllProteinsFile))

			Using swDataPlusAllProteinsFile = New System.IO.StreamWriter(New System.IO.FileStream(strDataPlusAllProteinsFile, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.Read))

				intCurrentLine = 1
				Using srInFile As System.IO.StreamReader = New System.IO.StreamReader(New System.IO.FileStream(strPeptideInputFilePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))
					Do While srInFile.Peek > -1
						strLineIn = srInFile.ReadLine()
						bytesRead += strLineIn.Length + intTerminatorSize
						strLineIn = strLineIn.Trim

						If intCurrentLine Mod 500 = 0 Then
							UpdateProgress("Creating the data plus all-proteins output file", CSng((bytesRead / srInFile.BaseStream.Length) * 100), eProteinCoverageProcessingSteps.SaveAllProteinsVersionOfInputFile)
						End If

						If intCurrentLine = 1 AndAlso mPeptideFileSkipFirstLine Then
							' Print out the first line, but append a new column name
							swDataPlusAllProteinsFile.WriteLine(strLineIn & ControlChars.Tab & "Protein_Name")

						ElseIf strLineIn.Length > 0 Then

							Try
								blnValidLine = False

								' Split the line, but for efficiency purposes, only parse out the first 3 columns
								strSplitLine = strLineIn.Split(chSepChars, 3)

								Select Case mPeptideFileColumnOrdering
									Case ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence
										strProteinName = strSplitLine(0)

										If strSplitLine.Length > 1 AndAlso Not strSplitLine(1) Is Nothing AndAlso strSplitLine(1).Length > 0 Then
											strPeptideSequence = strSplitLine(1)
											blnValidLine = True
										End If
									Case Else
										' Includes ePeptideFileColumnOrderingCode.SequenceOnly
										strPeptideSequence = strSplitLine(0)
										strProteinName = String.Empty
										blnValidLine = True
								End Select

							Catch ex As Exception
								blnValidLine = False
							End Try

							If Not blnValidLine Then
								swDataPlusAllProteinsFile.WriteLine(strLineIn & ControlChars.Tab & "?")
							Else
								strPeptideSequence = GetCleanPeptideSequence(strPeptideSequence, chPrefixResidue, chSuffixResidue, mRemoveSymbolCharacters)

								If mPeptideToProteinMapResults.TryGetValue(strPeptideSequence, lstProteins) Then

									For Each strProtein As String In lstProteins
										swDataPlusAllProteinsFile.WriteLine(strLineIn & ControlChars.Tab & strProtein)
									Next
								Else
									If intCurrentLine = 1 Then
										' This is likely a header line
										swDataPlusAllProteinsFile.WriteLine(strLineIn & ControlChars.Tab & "Protein_Name")
									Else
										swDataPlusAllProteinsFile.WriteLine(strLineIn & ControlChars.Tab & "?")
									End If
								End If
							End If
						Else
							swDataPlusAllProteinsFile.WriteLine()
						End If
					Loop


				End Using

			End Using

		Catch ex As Exception
			SetErrorMessage("Error in SaveDataPlusAllProteinsFile: " & ex.Message)
			If Not mShowMessages Then Throw New Exception("Error in SaveDataPlusAllProteinsFile", ex)
		End Try
	End Sub

	Private Sub SearchProteinsUsingLeaderSequences()

		Dim intProteinIndex As Integer
		Dim intProteinSeqCharIndex As Integer
		Dim intEndIndex As Integer

		Dim intCachedPeptideMatchIndex As Integer
		Dim intNextStartIndex As Integer
		Dim intPreviousPeptideCount As Integer

		Dim blnTestPeptide As Boolean
		Dim blnMatchFound As Boolean
		Dim blnMatchIsNew As Boolean
		Dim blnProteinSequenceUpdated As Boolean

		Dim strProteinSequence As String
		Dim strNewProteinSequence As String

		Dim strPeptideSequenceForKeySource As String
		Dim strPeptideSequenceForKey As String
		Dim strKey As String

		Dim intLeaderSequenceMinimumLength As Integer = mLeaderSequenceCache.LeaderSequenceMinimumLength
		Dim intPeptideLength As Integer

		Dim strProgressMessageBase As String

		Dim objItem As Object

		Dim intStartIndex As Integer
		Dim intProteinCount As Integer

		Dim intProteinProcessIterations As Integer
		Dim intProteinProcessIterationsExpected As Integer

		Dim blnProteinUpdated(PROTEIN_CHUNK_COUNT - 1) As Boolean

		' Step through the proteins in memory and compare the residues for each to mLeaderSequenceHashTable
		' If mSearchAllProteinsForPeptideSequence = False, then require that the protein name in the peptide input file matches the protein being examined

		Try
			Console.WriteLine()
			Console.WriteLine()
			strProgressMessageBase = "Comparing proteins to peptide leader sequences"
			Console.WriteLine(strProgressMessageBase)

			intProteinProcessIterations = 0
			intProteinProcessIterationsExpected = CInt(Math.Ceiling(mProteinDataCache.GetProteinCountCached / PROTEIN_CHUNK_COUNT)) * PROTEIN_CHUNK_COUNT
			If intProteinProcessIterationsExpected < 1 Then intProteinProcessIterationsExpected = 1

			UpdateProgress(strProgressMessageBase, 0, _
			   eProteinCoverageProcessingSteps.SearchProteinsUsingLeaderSequences)

			intStartIndex = 0
			Do
				' Extract up to PROTEIN_CHUNK_COUNT proteins from the Sql Lite database
				' Store the information in the four local arrays
				intProteinCount = ReadProteinInfoChunk(intStartIndex, PROTEIN_CHUNK_COUNT, blnProteinUpdated, False)

				For intProteinIndex = 0 To intProteinCount - 1

					strProteinSequence = String.Copy(mCachedProteinInfo(intProteinIndex).Sequence)
					blnProteinSequenceUpdated = False

					For intProteinSeqCharIndex = 0 To strProteinSequence.Length - intLeaderSequenceMinimumLength

						' Call .GetFirstPeptideIndexForLeaderSequence to see if the hash table contains the intLeaderSequenceMinimumLength residues starting at intProteinSeqCharIndex
						If mSearchAllProteinsSkipCoverageComputationSteps Then
							' No need to capitalize strProteinSequence since it's already capitalized
							intCachedPeptideMatchIndex = mLeaderSequenceCache.GetFirstPeptideIndexForLeaderSequence(strProteinSequence.Substring(intProteinSeqCharIndex, intLeaderSequenceMinimumLength))
						Else
							' Need to change strProteinSequence to all caps when calling GetFirstPeptideIndexForLeaderSequence
							intCachedPeptideMatchIndex = mLeaderSequenceCache.GetFirstPeptideIndexForLeaderSequence(strProteinSequence.Substring(intProteinSeqCharIndex, intLeaderSequenceMinimumLength).ToUpper)
						End If


						If intCachedPeptideMatchIndex >= 0 Then
							' mLeaderSequenceCache contains 1 or more peptides that start with strProteinSequence.Substring(intProteinSeqCharIndex, intLeaderSequenceMinimumLength)
							' Test each of the peptides against this protein

							Do

								If mSearchAllProteinsForPeptideSequence Then
									blnTestPeptide = True
								Else
									' Make sure that the protein for intCachedPeptideMatchIndex matches this protein name
									If mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).ProteinName.ToLower = mCachedProteinInfo(intProteinIndex).Name.ToLower Then
										blnTestPeptide = True
									Else
										blnTestPeptide = False
									End If
								End If

								' Cache the peptide length in intPeptideLength
								intPeptideLength = mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequence.Length

								' Only compare the full sequence to the protein if:
								'  a) the protein name matches (or mSearchAllProteinsForPeptideSequence = True) and
								'  b) the peptide sequence doesn't pass the end of the protein
								If blnTestPeptide AndAlso intProteinSeqCharIndex + intPeptideLength <= strProteinSequence.Length Then

									' See if the full sequence matches the protein
									blnMatchFound = False
									If mSearchAllProteinsSkipCoverageComputationSteps Then
										' No need to capitalize strProteinSequence since it's already capitalized
										If mIgnoreILDifferences Then
											If strProteinSequence.Substring(intProteinSeqCharIndex, intPeptideLength) = mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequenceLtoI Then
												blnMatchFound = True
											End If
										Else
											If strProteinSequence.Substring(intProteinSeqCharIndex, intPeptideLength) = mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequence Then
												blnMatchFound = True
											End If
										End If
									Else
										' Need to change strProteinSequence to all caps when comparing to .PeptideSequence
										If mIgnoreILDifferences Then
											If strProteinSequence.Substring(intProteinSeqCharIndex, intPeptideLength).ToUpper = mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequenceLtoI Then
												blnMatchFound = True
											End If
										Else
											If strProteinSequence.Substring(intProteinSeqCharIndex, intPeptideLength).ToUpper = mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequence Then
												blnMatchFound = True
											End If
										End If

									End If

									If blnMatchFound Then
										intEndIndex = intProteinSeqCharIndex + intPeptideLength - 1
										If mMatchPeptidePrefixAndSuffixToProtein Then
											blnMatchFound = ValidatePrefixAndSuffix(strProteinSequence, mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PrefixLtoI, mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).SuffixLtoI, intProteinSeqCharIndex, intEndIndex)
										End If

										If blnMatchFound Then
											If mMatchPeptidePrefixAndSuffixToProtein Then
												strPeptideSequenceForKeySource = ConstructPeptideSequenceForKey(mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequence, mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).Prefix, mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).Suffix)
											Else
												' I'm purposely not using String.Copy() here in order to obtain increased speed
												strPeptideSequenceForKeySource = mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequence
											End If

											If mIgnoreILDifferences Then
												' Replace all L characters with I
												strPeptideSequenceForKey = strPeptideSequenceForKeySource.Replace("L"c, "I"c)
											Else
												' I'm purposely not using String.Copy() here in order to obtain increased speed
												strPeptideSequenceForKey = strPeptideSequenceForKeySource
											End If

											If Not mSearchAllProteinsSkipCoverageComputationSteps Then
												' Capitalize the protein sequence letters where this peptide matched
												intNextStartIndex = intEndIndex + 1

												strNewProteinSequence = String.Empty
												If intProteinSeqCharIndex > 0 Then
													strNewProteinSequence = strProteinSequence.Substring(0, intProteinSeqCharIndex)
												End If
												strNewProteinSequence &= strProteinSequence.Substring(intProteinSeqCharIndex, intNextStartIndex - intProteinSeqCharIndex).ToUpper
												strNewProteinSequence &= strProteinSequence.Substring(intNextStartIndex)
												strProteinSequence = String.Copy(strNewProteinSequence)

												blnProteinSequenceUpdated = True
											End If

											If mTrackPeptideCounts Then
												strKey = CStr(mCachedProteinInfo(intProteinIndex).UniqueSequenceID) & "::" & strPeptideSequenceForKey

												objItem = mProteinPeptideStats(strKey)
												If Not objItem Is Nothing Then
													blnMatchIsNew = False
													intPreviousPeptideCount = CInt(objItem)
													mProteinPeptideStats.Item(strKey) = intPreviousPeptideCount + 1
												Else
													blnMatchIsNew = True
													mProteinPeptideStats.Add(strKey, 1)
												End If
											Else
												' Must always assume the match is new since not tracking peptide counts
												blnMatchIsNew = True
											End If

											If blnMatchIsNew Then
												If mSaveProteinToPeptideMappingFile Then
													WriteEntryToProteinToPeptideMappingFile(mCachedProteinInfo(intProteinIndex).Name, strPeptideSequenceForKeySource, intProteinSeqCharIndex + 1, intEndIndex + 1)
												End If

												If mSaveSourceDataPlusProteinsFile Then
													StorePeptideToProteinMatch(mLeaderSequenceCache.mCachedPeptideSeqInfo(intCachedPeptideMatchIndex).PeptideSequence, mCachedProteinInfo(intProteinIndex).Name)
												End If

											End If
										End If
									End If
								End If

								intCachedPeptideMatchIndex = mLeaderSequenceCache.GetNextPeptideWithLeaderSequence(intCachedPeptideMatchIndex)
							Loop While intCachedPeptideMatchIndex >= 0
						End If
					Next intProteinSeqCharIndex

					If blnProteinSequenceUpdated Then
						mCachedProteinInfo(intProteinIndex).Sequence = String.Copy(strProteinSequence)
						blnProteinUpdated(intProteinIndex) = True
					End If

					intProteinProcessIterations += 1
					If intProteinProcessIterations Mod 100 = 0 Then
						UpdateProgress(CSng(intProteinProcessIterations / intProteinProcessIterationsExpected * 100), _
						   eProteinCoverageProcessingSteps.SearchProteinsUsingLeaderSequences)
					End If

					If mAbortProcessing Then Exit For

				Next

				' Store the updated protein sequence information in the SQL Lite DB
				UpdateSequenceDbDataValues(blnProteinUpdated, intProteinCount)

				' Increment intStartIndex to obtain the next chunk of proteins
				intStartIndex += PROTEIN_CHUNK_COUNT

			Loop While intStartIndex < mProteinDataCache.GetProteinCountCached()

		Catch ex As Exception
			SetErrorMessage("Error in SearchProteinsUsingLeaderSequences: " & ex.Message)
			If Not mShowMessages Then Throw New System.Exception("Error in SearchProteinsUsingLeaderSequences", ex)
		End Try

	End Sub

	Private Sub SearchProteinsUsingCachedPeptides(ByRef htShortPeptideCache As Hashtable)

		Dim strProgressMessageBase As String

		If htShortPeptideCache.Count > 0 Then
			Console.WriteLine()
			Console.WriteLine()
			strProgressMessageBase = "Comparing proteins to short peptide sequences"
			Console.WriteLine(strProgressMessageBase)

			UpdateProgress(strProgressMessageBase)

			' Need to step through the proteins and match them to the data in htShortPeptideCache
			FindSequenceMatchForPeptideList(htShortPeptideCache, String.Empty)
		End If

	End Sub

	Private Sub StorePeptideToProteinMatch(ByVal strCleanPeptideSequence As String, ByVal strProteinName As String)

		' Store the mapping between peptide sequence and protein name
		Dim lstProteins As Generic.List(Of String) = Nothing
		If mPeptideToProteinMapResults.TryGetValue(strCleanPeptideSequence, lstProteins) Then
			lstProteins.Add(strProteinName)
		Else
			lstProteins = New Generic.List(Of String)
			lstProteins.Add(strProteinName)
			mPeptideToProteinMapResults.Add(strCleanPeptideSequence, lstProteins)
		End If

	End Sub

	Private Function ValidateColumnCountInInputFile(ByVal strPeptideInputFilePath As String) As Boolean

		Dim blnSuccess As Boolean

		If mPeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.SequenceOnly Then
			' Simply return true; don't even pre-read the file
			' However, auto-switch mSearchAllProteinsForPeptideSequence to true if not true
			If Not mSearchAllProteinsForPeptideSequence Then
				mSearchAllProteinsForPeptideSequence = True
			End If
			Return True
		End If

		blnSuccess = ValidateColumnCountInInputFile(strPeptideInputFilePath, mPeptideFileColumnOrdering, mPeptideFileSkipFirstLine, mPeptideInputFileDelimiter)

		If blnSuccess Then
			If mPeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.SequenceOnly Then
				' Need to auto-switch to search all proteins
				mSearchAllProteinsForPeptideSequence = True
			End If
		End If

		Return blnSuccess
	End Function

	Public Shared Function ValidateColumnCountInInputFile(ByVal strPeptideInputFilePath As String, _
	   ByRef ePeptideFileColumnOrdering As ePeptideFileColumnOrderingCode, _
	   ByVal blnSkipFirstLine As Boolean, _
	   ByVal chColumnDelimiter As Char) As Boolean

		' Read the first two lines to check whether the data file actually has only one column when the user has
		'  specified mPeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence

		' Returns True if no problems; False if the user chooses to abort
		' If mPeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.SequenceOnly then the file isn't even opened

		Dim strLineIn As String
		Dim strSplitLine As String()

		Dim intCurrentLine As Integer

		' Open the file and read in the lines
		Using srInFile As System.IO.StreamReader = New System.IO.StreamReader(New System.IO.FileStream(strPeptideInputFilePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read))

			intCurrentLine = 1
			Do While srInFile.Peek <> -1 AndAlso intCurrentLine < 3
				strLineIn = srInFile.ReadLine.Trim

				If intCurrentLine = 1 AndAlso blnSkipFirstLine Then
					' do nothing, skip the first line
				ElseIf strLineIn.Length > 0 Then
					Try
						strSplitLine = strLineIn.Split(chColumnDelimiter)

						If (Not blnSkipFirstLine And intCurrentLine = 1) OrElse _
						   (blnSkipFirstLine And intCurrentLine = 2) Then
							If strSplitLine.Length = 1 AndAlso ePeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.ProteinName_PeptideSequence Then
								' Auto switch to ePeptideFileColumnOrderingCode.SequenceOnly
								ePeptideFileColumnOrdering = ePeptideFileColumnOrderingCode.SequenceOnly
							End If
						End If

					Catch ex As Exception
						' Ignore the error
					End Try
				End If
				intCurrentLine += 1
			Loop

		End Using

		Return True

	End Function

	Private Function ValidatePrefixAndSuffix(ByVal strProteinSequence As String, ByVal chPrefixResidue As Char, ByVal chSuffixResidue As Char, ByVal intStartIndex As Integer, ByVal intEndIndex As Integer) As Boolean
		Dim blnMatchValid As Boolean

		blnMatchValid = True
		If Char.IsLetter(chPrefixResidue) Then
			If intStartIndex >= 1 Then
				If Char.ToUpper(strProteinSequence.Chars(intStartIndex - 1)) <> chPrefixResidue Then
					blnMatchValid = False
				End If
			End If
		ElseIf chPrefixResidue = "-"c AndAlso intStartIndex <> 0 Then
			blnMatchValid = False
		End If

		If blnMatchValid Then
			If Char.IsLetter(chSuffixResidue) Then
				If intEndIndex < strProteinSequence.Length - 1 Then
					If Char.ToUpper(strProteinSequence.Chars(intEndIndex + 1)) <> chSuffixResidue Then
						blnMatchValid = False
					End If
				Else
					blnMatchValid = False
				End If
			ElseIf chSuffixResidue = "-"c AndAlso intEndIndex < strProteinSequence.Length - 1 Then
				blnMatchValid = False
			End If
		End If

		Return blnMatchValid

	End Function

	Private Sub WriteEntryToProteinToPeptideMappingFile(ByVal strProteinName As String, ByVal strPeptideSequenceForKey As String, ByVal intStartResidue As Integer, ByVal intEndResidue As Integer)
		If mSaveProteinToPeptideMappingFile AndAlso Not mProteinToPeptideMappingOutputFile Is Nothing Then
			mProteinToPeptideMappingOutputFile.WriteLine(strProteinName & ControlChars.Tab & strPeptideSequenceForKey & ControlChars.Tab & intStartResidue & ControlChars.Tab & intEndResidue)
		End If
	End Sub

	Protected Sub ResetProgress()
		ResetProgress(String.Empty)
	End Sub

	Protected Sub ResetProgress(ByVal strProgressStepDescription As String)
		mProgressStepDescription = String.Copy(strProgressStepDescription)
		mProgressPercentComplete = 0
		RaiseEvent ProgressReset()
	End Sub

	Protected Sub SetErrorCode(ByVal eNewErrorCode As eProteinCoverageErrorCodes)
		SetErrorCode(eNewErrorCode, False)
	End Sub

	Protected Sub SetErrorCode(ByVal eNewErrorCode As eProteinCoverageErrorCodes, ByVal blnLeaveExistingErrorCodeUnchanged As Boolean)
		If blnLeaveExistingErrorCodeUnchanged AndAlso mErrorCode <> eProteinCoverageErrorCodes.NoError Then
			' An error code is already defined; do not change it
		Else
			mErrorCode = eNewErrorCode
		End If
	End Sub

	Protected Sub SetErrorMessage(ByVal strMessage As String)
		If strMessage Is Nothing Then strMessage = String.Empty
		mErrorMessage = String.Copy(strMessage)

		If mErrorMessage.Length > 0 Then
			Console.WriteLine(mErrorMessage)
			UpdateProgress(mErrorMessage)
		End If
	End Sub

	Protected Sub UpdateProgress(ByVal strProgressStepDescription As String)
		mProgressStepDescription = String.Copy(strProgressStepDescription)
		RaiseEvent ProgressChanged(Me.ProgressStepDescription, Me.ProgressPercentComplete)
	End Sub

	Protected Sub UpdateProgress(ByVal sngPercentComplete As Single, ByVal eCurrentProcessingStep As eProteinCoverageProcessingSteps)
		UpdateProgress(Me.ProgressStepDescription, sngPercentComplete, eCurrentProcessingStep)
	End Sub

	Protected Sub UpdateProgress(ByVal strProgressStepDescription As String, ByVal sngPercentComplete As Single, ByVal eCurrentProcessingStep As eProteinCoverageProcessingSteps)
		Dim sngStartPercent As Single
		Dim sngEndPercent As Single

		mProgressStepDescription = String.Copy(strProgressStepDescription)
		mCurrentProcessingStep = eCurrentProcessingStep

		If sngPercentComplete < 0 Then
			sngPercentComplete = 0
		ElseIf sngPercentComplete > 100 Then
			sngPercentComplete = 100
		End If

		sngStartPercent = mPercentCompleteStartLevels(eCurrentProcessingStep)
		sngEndPercent = mPercentCompleteStartLevels(eCurrentProcessingStep + 1)

		' Use the start and end percent complete values for the specified processing step to convert sngPercentComplete to an overall percent complete value
		mProgressPercentComplete = sngStartPercent + CSng(sngPercentComplete / 100.0 * (sngEndPercent - sngStartPercent))

		RaiseEvent ProgressChanged(Me.ProgressStepDescription, Me.ProgressPercentComplete)
	End Sub

	''Protected Sub UpdateSubtaskProgress(ByVal sngPercentComplete As Single)
	''    UpdateSubtaskProgress(mSubtaskStepDescription, sngPercentComplete)
	''End Sub

	''Protected Sub UpdateSubtaskProgress(ByVal strSubtaskStepDescription As String, ByVal sngPercentComplete As Single)
	''    mSubtaskStepDescription = String.Copy(strSubtaskStepDescription)
	''    If sngPercentComplete < 0 Then
	''        sngPercentComplete = 0
	''    ElseIf sngPercentComplete > 100 Then
	''        sngPercentComplete = 100
	''    End If
	''    mSubtaskPercentComplete = sngPercentComplete

	''    RaiseEvent SubtaskProgressChanged(Me.SubtaskStepDescription, Me.SubtaskPercentComplete)
	''End Sub

	Private Sub mLeaderSequenceCache_ProgressChanged(ByVal taskDescription As String, ByVal percentComplete As Single) Handles mLeaderSequenceCache.ProgressChanged
		UpdateProgress(percentComplete, eProteinCoverageProcessingSteps.DetermineShortestPeptideLength)
	End Sub

	Private Sub mLeaderSequenceCache_ProgressComplete() Handles mLeaderSequenceCache.ProgressComplete
		UpdateProgress(100, eProteinCoverageProcessingSteps.DetermineShortestPeptideLength)
	End Sub

	Private Sub mProteinDataCache_ProteinCachedWithProgress(ByVal intProteinsCached As Integer, ByVal sngPercentFileProcessed As Single) Handles mProteinDataCache.ProteinCachedWithProgress
		Const CONSOLE_UPDATE_INTERVAL_SECONDS As Integer = 3

		Static dtLastUpdate As DateTime = System.DateTime.UtcNow

		If System.DateTime.UtcNow.Subtract(dtLastUpdate).TotalSeconds >= CONSOLE_UPDATE_INTERVAL_SECONDS Then
			dtLastUpdate = System.DateTime.UtcNow
			Console.Write(".")
		End If

		UpdateProgress(sngPercentFileProcessed, eProteinCoverageProcessingSteps.CacheProteins)

	End Sub

	Private Sub mProteinDataCache_ProteinCachingComplete() Handles mProteinDataCache.ProteinCachingComplete
		UpdateProgress(100, eProteinCoverageProcessingSteps.CacheProteins)
	End Sub
End Class
