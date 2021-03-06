﻿using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Table;

namespace explainpowershell.models
{
    public class Command
    {
        public string CommandName { get; set; }
    }

    public class Code
    {
        public string PowershellCode { get; set; }
    }


    public class AnalysisResult
    {
        public string ExpandedCode { get; set; }
        public List<Explanation> Explanations { get; set; } = new List<Explanation>();
        public List<Module> DetectedModules { get; set; } = new List<Module>();
        public string ParseErrorMessage { get; set; }
    }

    public class Module
    {
        public string ModuleName { get; set; }
    }

    public class Explanation
    {
        public string OriginalExtent { get; set; }
        public string CommandName { get; set; }
        public string Description { get; set; }
        public HelpEntity HelpResult { get; set; }
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string TextToHighlight { get; set; }
    }

    public class HelpEntity : TableEntity
    {
        public string DocumentationLink { get; set; }
        public string Synopsis { get; set; }
        public string Syntax { get; set; }
        public string ModuleName { get; set; }
        public string CommandName { get; set; }
    }

    public class HelpMetaData
    {
        public int NumberOfCommands { get; set; }
        public int NumberOfAboutArticles { get; set; }
        public int NumberOfModules { get; set; }
        public IEnumerable<string> ModuleNames { get; set; }

        public string LastPublished {get; set;}
    }
}
