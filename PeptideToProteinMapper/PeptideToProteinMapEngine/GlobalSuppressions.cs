// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Safe to ignore in this class", Scope = "module")]
[assembly: SuppressMessage("Readability", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses not needed", Scope = "member", Target = "~M:PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.PostProcessPSMResultsFileReadMapFile(System.String,System.String[]@,System.Int32[]@,PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.ProteinIDMapInfo[]@)~System.Boolean")]
[assembly: SuppressMessage("Readability", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses not needed", Scope = "member", Target = "~M:PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.PreProcessPHRPDataFile(System.String,System.String)~System.String")]
[assembly: SuppressMessage("Readability", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses not needed", Scope = "member", Target = "~M:PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.PreProcessPSMResultsFile(System.String,System.String,PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.PeptideInputFileFormatConstants)~System.String")]
[assembly: SuppressMessage("Readability", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses not needed", Scope = "member", Target = "~M:PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.ProteinCoverageSummarizer_ProgressChanged(System.String,System.Single)")]
[assembly: SuppressMessage("Simplification", "RCS1190:Join string expressions.", Justification = "Keep separate for readability", Scope = "member", Target = "~M:PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.PreProcessDataWriteOutPeptides(System.String,System.String)~System.String")]
[assembly: SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "Keep separate for readability", Scope = "member", Target = "~M:PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.ProcessFile(System.String,System.String,System.String,System.Boolean)~System.Boolean")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Allowed", Scope = "type", Target = "~T:PeptideToProteinMapEngine.clsPeptideToProteinMapEngine")]
