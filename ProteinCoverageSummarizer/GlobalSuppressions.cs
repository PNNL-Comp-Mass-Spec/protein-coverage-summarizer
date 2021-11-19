// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Keep for debugging purposes", Scope = "member", Target = "~F:ProteinCoverageSummarizer.clsProteinCoverageSummarizer.mCurrentProcessingStep")]
[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Ignore attempts to delete the temporary file", Scope = "member", Target = "~M:ProteinCoverageSummarizer.clsProteinFileDataCache.DefineSQLiteDBPath(System.String)~System.String")]
[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Ignore errors deleting the SQLite database", Scope = "member", Target = "~M:ProteinCoverageSummarizer.clsProteinFileDataCache.DeleteSQLiteDBFile(System.String,System.Boolean)")]
[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Silently ignore this; the file is likely corrupt", Scope = "member", Target = "~M:ProteinCoverageSummarizer.clsProteinCoverageSummarizer.ValidateColumnCountInInputFile(System.String,ProteinCoverageSummarizer.ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode@,System.Boolean,System.Char)~System.Boolean")]
[assembly: SuppressMessage("Readability", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses not needed", Scope = "member", Target = "~M:ProteinCoverageSummarizer.clsProteinCoverageSummarizer.ValidateColumnCountInInputFile(System.String,ProteinCoverageSummarizer.ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode@,System.Boolean,System.Char)~System.Boolean")]
[assembly: SuppressMessage("Simplification", "RCS1190:Join string expressions.", Justification = "Keep separate for readability", Scope = "member", Target = "~M:ProteinCoverageSummarizer.clsProteinCoverageSummarizer.ParsePeptideInputFile(System.String,System.String,System.String,System.String@)~System.Boolean")]
[assembly: SuppressMessage("Simplification", "RCS1190:Join string expressions.", Justification = "Keep separate for readability", Scope = "member", Target = "~M:ProteinCoverageSummarizer.clsProteinCoverageSummarizer.SaveDataPlusAllProteinsFile(System.String,System.String,System.String,System.Char[],System.Int32)")]
[assembly: SuppressMessage("Simplification", "RCS1190:Join string expressions.", Justification = "Keep separate for readability", Scope = "member", Target = "~M:ProteinCoverageSummarizer.clsProteinCoverageSummarizer.WriteCachedLinesToAllProteinsFile(System.String,System.Collections.Generic.IReadOnlyCollection{System.Collections.Generic.KeyValuePair{System.String,System.String}},System.IO.TextWriter,System.Int32)")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Allowed", Scope = "type", Target = "~T:ProteinCoverageSummarizer.clsLeaderSequenceCache")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Allowed", Scope = "type", Target = "~T:ProteinCoverageSummarizer.clsProteinCoverageSummarizer")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Allowed", Scope = "type", Target = "~T:ProteinCoverageSummarizer.clsProteinFileDataCache")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Allowed", Scope = "type", Target = "~T:ProteinCoverageSummarizer.ProteinCoverageSummarizerOptions.PeptideFileColumnOrderingCode")]
