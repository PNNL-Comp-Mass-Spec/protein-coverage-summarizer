// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Allowed", Scope = "type", Target = "~T:PeptideToProteinMapEngine.clsPeptideToProteinMapEngine")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Allowed", Scope = "type", Target = "~T:PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.udtPepToProteinMappingType")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Allowed", Scope = "type", Target = "~T:PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.udtProteinIDMapInfoType")]
[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Safe to ignore in this class", Scope = "module")]
[assembly: SuppressMessage("Readability", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses not needed", Scope = "member", Target = "~M:PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.PreProcessPHRPDataFile(System.String,System.String)~System.String")]
[assembly: SuppressMessage("Readability", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses not needed", Scope = "member", Target = "~M:PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.ProteinCoverageSummarizer_ProgressChanged(System.String,System.Single)")]
[assembly: SuppressMessage("Readability", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses not needed", Scope = "member", Target = "~M:PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.PreProcessPSMResultsFile(System.String,System.String,PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.PeptideInputFileFormatConstants)~System.String")]
[assembly: SuppressMessage("Readability", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses are correct", Scope = "member", Target = "~M:PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.PostProcessPSMResultsFileReadMapFile(System.String,System.String[]@,System.Int32[]@,PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.udtProteinIDMapInfoType[]@)~System.Boolean")]
[assembly: SuppressMessage("Simplification", "RCS1190:Join string expressions.", Justification = "Keep separate for readability", Scope = "member", Target = "~M:PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.PreProcessDataWriteOutPeptides(System.String,System.String)~System.String")]
