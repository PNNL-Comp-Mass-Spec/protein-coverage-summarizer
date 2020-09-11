// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "type", Target = "~T:ProteinCoverageSummarizer.clsProteinCoverageSummarizer")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "type", Target = "~T:ProteinCoverageSummarizer.clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "type", Target = "~T:ProteinCoverageSummarizer.clsProteinCoverageSummarizer.eProteinCoverageErrorCodes")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "type", Target = "~T:ProteinCoverageSummarizer.clsProteinCoverageSummarizer.eProteinCoverageProcessingSteps")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "type", Target = "~T:ProteinCoverageSummarizer.clsProteinCoverageSummarizer.udtPeptideCountStatsType")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "type", Target = "~T:ProteinCoverageSummarizer.clsProteinFileDataCache")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "type", Target = "~T:ProteinCoverageSummarizer.clsProteinFileDataCache.udtProteinInfoType")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "type", Target = "~T:ProteinCoverageSummarizer.clsLeaderSequenceCache")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "type", Target = "~T:ProteinCoverageSummarizer.clsLeaderSequenceCache.udtPeptideSequenceInfoType")]
[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Ignore attempts to delete the temporary file", Scope = "member", Target = "~M:ProteinCoverageSummarizer.clsProteinFileDataCache.DefineSQLiteDBPath(System.String)~System.String")]
[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Silently ignore this; the file is likely corrupt", Scope = "member", Target = "~M:ProteinCoverageSummarizer.clsProteinCoverageSummarizer.ValidateColumnCountInInputFile(System.String,ProteinCoverageSummarizer.clsProteinCoverageSummarizer.ePeptideFileColumnOrderingCode@,System.Boolean,System.Char)~System.Boolean")]
[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Ignore errors deleting the SQLite database", Scope = "member", Target = "~M:ProteinCoverageSummarizer.clsProteinFileDataCache.DeleteSQLiteDBFile(System.String,System.Boolean)")]
