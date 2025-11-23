using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using explainpowershell.models;
using System.Linq;
using System.Net.Http.Json;
using MudBlazor;

namespace explainpowershell.frontend.Pages
{
    public partial class Index : ComponentBase {
        [Inject]
        private HttpClient Http { get; set; }
        private string TitleMargin { get; set; }= "mt-16";
        private Dictionary<string, bool> SyntaxPopoverIsOpen { get; set; }= new();
        private Dictionary<string, bool> CommandDetailsPopoverIsOpen { get; set; } = new();
        private bool RequestHasError { get; set; }
        private string ReasonPhrase { get; set; }
        private bool Waiting { get; set; }
        private bool AiExplanationLoading { get; set; }
        private bool HideExpandedCode { get; set; }
        private string ExpandedCode { get; set; }
        private string AiExplanation { get; set; }
        private List<TreeItemData<Explanation>> TreeItems { get; set; } = new();
        private bool ShouldShrinkTitle { get; set; } = false;
        private bool HasNoExplanations => TreeItems.Count == 0;
        private string InputValue {
            get {
                return _inputValue;
            }
            set {
                _inputValue = value;
            }
        }

        private Task OnKeyUp(KeyboardEventArgs arg) {
            if (arg.Key == "Enter")
            {
                return DoSearch();
            }
            return Task.CompletedTask;
        }

        private void AcknowledgeAlert() {
            RequestHasError = false;
            ReasonPhrase = "";
        }

        protected override Task OnInitializedAsync()
        {
            return DoSearch();
        }

        private void ToggleSyntaxPopoverIsOpen(string id)
        {
            SyntaxPopoverIsOpen[id] = !SyntaxPopoverIsOpen[id];
        }

        private void ToggleCommandDetailsPopoverIsOpen(string id)
        {
            CommandDetailsPopoverIsOpen[id] = !CommandDetailsPopoverIsOpen[id];
        }

        private void ShrinkTitle()
        {
            ShouldShrinkTitle = true;
            TitleMargin = "mt-6";
            StateHasChanged();
        }

        private async Task DoSearch()
        {
            HideExpandedCode = true;
            Waiting = false;
            RequestHasError = false;
            ReasonPhrase = string.Empty;
            TreeItems = new();
            ExpandedCode = null;
            AiExplanation = null;
            AiExplanationLoading = false;

            if (string.IsNullOrEmpty(InputValue))
                return;

            ShrinkTitle();

            Waiting = true;
            var code = new Code() { PowershellCode = InputValue };

            HttpResponseMessage temp;
            try {
                temp = await Http.PostAsJsonAsync<Code>("SyntaxAnalyzer", code);
            }
            catch {
                RequestHasError = true;
                Waiting = false;
                ReasonPhrase = "oops!";
                return;
            }

            if (!temp.IsSuccessStatusCode)
            {
                RequestHasError = true;
                Waiting = false;
                ReasonPhrase = await temp.Content.ReadAsStringAsync();
                return;
            }

            var analysisResult = await JsonSerializer.DeserializeAsync<AnalysisResult>(temp.Content.ReadAsStream());

            if (!string.IsNullOrEmpty(analysisResult.ParseErrorMessage))
            {
                RequestHasError = true;
                ReasonPhrase = analysisResult.ParseErrorMessage;
            }

            Waiting = false;
            HideExpandedCode = false;

            SyntaxPopoverIsOpen.Clear();
            foreach (var syntaxedExplanation in analysisResult.Explanations.Where(i => i.HelpResult?.Syntax != null))
            {
                SyntaxPopoverIsOpen.Add(syntaxedExplanation.Id, false);
            }

            CommandDetailsPopoverIsOpen.Clear();
            foreach (var CommandDetails in analysisResult.Explanations.Where(i => i.HelpResult?.Syntax != null))
            {
                CommandDetailsPopoverIsOpen.Add(CommandDetails.Id, false);
            }

            TreeItems = analysisResult.Explanations.GenerateTree(expl => expl.Id, expl => expl.ParentId);
            ExpandedCode = analysisResult.ExpandedCode;
            AiExplanation = null; // Will be loaded separately

            // Start fetching AI explanation in background
            _ = LoadAiExplanationAsync(code, analysisResult);
        }

        private async Task LoadAiExplanationAsync(Code code, AnalysisResult analysisResult)
        {
            AiExplanationLoading = true;
            StateHasChanged();

            try
            {
                var aiRequest = new
                {
                    PowershellCode = code.PowershellCode,
                    AnalysisResult = analysisResult
                };

                var response = await Http.PostAsJsonAsync("AiExplanation", aiRequest);

                if (response.IsSuccessStatusCode)
                {
                    var aiResult = await JsonSerializer.DeserializeAsync<AiExplanationResponse>(response.Content.ReadAsStream());
                    AiExplanation = aiResult?.AiExplanation ?? string.Empty;
                    AiModelName = aiResult?.ModelName ?? string.Empty;
                }
                else
                {
                    // Silently fail - AI explanation is optional
                    AiExplanation = string.Empty;
                    AiModelName = string.Empty;
                }
            }
            catch
            {
                // Silently fail - AI explanation is optional
                AiExplanation = string.Empty;
                AiModelName = string.Empty;
            }
            finally
            {
                AiExplanationLoading = false;
                StateHasChanged();
            }
        }

        private string _inputValue;
        private string AiModelName { get; set; }

        private class AiExplanationResponse
        {
            public string AiExplanation { get; set; } = string.Empty;
            public string? ModelName { get; set; }
        }
    }
}