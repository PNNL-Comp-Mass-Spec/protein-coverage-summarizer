// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Silently ignore this", Scope = "member", Target = "~M:PeptideToProteinMapper.Program.SetOptionsUsingCommandLineParameters(PRISM.clsParseCommandLine,ProteinCoverageSummarizer.ProteinCoverageSummarizerOptions)~System.Boolean")]
[assembly: SuppressMessage("Simplification", "RCS1190:Join string expressions.", Justification = "Keep separate for readability", Scope = "member", Target = "~M:PeptideToProteinMapper.Program.CreateVerboseLogFile")]
