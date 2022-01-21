using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using explainpowershell.models;
using System.Linq;
using System.Net.Http.Json;

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
        private bool HideExpandedCode { get; set; }
        private string ExpandedCode { get; set; }
        private HashSet<TreeItem<Explanation>> TreeItems { get; set; } = new HashSet<TreeItem<Explanation>>();
        private bool ShouldShrinkTitle { get; set; } = false;
        private bool HasNoExplanations => TreeItems.Count <= 0;
        private string InputValue {
            get {
                return _inputValue;
            }
            set {
                // The MudTextField can be overloaded with large amounts of text. Prevent this with a hard string length limit.
                if (value.Length <= 255)
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
        }

        private string _inputValue;
    }
}