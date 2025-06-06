﻿
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Silently ignore this", Scope = "member", Target = "~M:ProteinCoverageSummarizerGUI.GUI.CreateSummaryDataTable(System.String)")]
[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Silently ignore this", Scope = "member", Target = "~M:ProteinCoverageSummarizerGUI.GUI.ShowRichTextStart(ProteinCoverageSummarizerGUI.GUI.SequenceDisplayConstants)")]
[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "Silently ignore this", Scope = "member", Target = "~M:ProteinCoverageSummarizerGUI.Program.SetOptionsUsingCommandLineParameters(PRISM.clsParseCommandLine,ProteinCoverageSummarizer.ProteinCoverageSummarizerOptions,System.Boolean@)~System.Boolean")]
[assembly: SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "Leave as-is for readability", Scope = "member", Target = "~M:ProteinCoverageSummarizerGUI.GUI.CreateSummaryDataTable(System.String)")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Allow object type prefixes in this class", Scope = "module")]
[assembly: SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "Prefer to use .ToList()", Scope = "member", Target = "~M:ProteinCoverageSummarizerGUI.Program.SetOptionsUsingCommandLineParameters(PRISM.clsParseCommandLine,ProteinCoverageSummarizer.ProteinCoverageSummarizerOptions,System.Boolean@)~System.Boolean")]
