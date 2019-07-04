﻿using Microsoft.CodeAnalysis.Completion;
using RoslynSandbox.Core.Workspace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoslynSandbox.Core.Analysis
{
    public class AutoCompleteService : IDisposable
    {
        public AutoCompleteService(Sandbox workspace)
        {
            Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));

            Workspace.EditingDocumentChanged += WorkspaceEditingDocumentChanged;

            WorkspaceEditingDocumentChanged(
                null, 
                new EditingDocumentChangedEventArgs(Workspace.EditingFile, Workspace.EditingDocument)
            );
        }

        private void WorkspaceEditingDocumentChanged(object sender, EditingDocumentChangedEventArgs e)
        {
            if (Workspace.EditingDocument != null)
            {
                _completionService = CompletionService.GetService(Workspace.EditingDocument);
            }
        }

        private CompletionService _completionService;

        public Sandbox Workspace { get; }

        public async Task<IEnumerable<CompletionItem>> GetAutoCompleteAsync()
        {
            if (Workspace.EditingDocument is null)
            {
                return new List<CompletionItem>();
            }

            CompletionList completionList = await _completionService.GetCompletionsAsync(Workspace.EditingDocument, Workspace.EditingFile.EditorPosition.Value);

            return FilterByActiveSpan(completionList, Workspace.EditingFile.RawContents);
        }

        private IEnumerable<CompletionItem> FilterByActiveSpan(CompletionList suggested, string originalSource)
        {
            if (suggested is null)
            {
                return new List<CompletionItem>();
            }

            if (originalSource is null || suggested.Span.IsEmpty)
            {
                return suggested.Items;
            }

            var editingPrefix = originalSource.Substring(suggested.Span.Start, suggested.Span.Length);

            return suggested.Items.Where(c => c.DisplayText.StartsWith(editingPrefix));
        }

        public void Dispose()
        {
            if (Workspace != null)
            {
                Workspace.EditingDocumentChanged -= WorkspaceEditingDocumentChanged;
            }
        }
    }
}