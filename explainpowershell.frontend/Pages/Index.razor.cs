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
        private string titleMargin = "mt-16";
        private Dictionary<string, bool> SyntaxPopoverIsOpen = new Dictionary<string, bool>();
        private Dictionary<string, bool> CommandDetailsPopoverIsOpen = new Dictionary<string, bool>();
        private bool requestHasError {get;set;}
        private string reasonPhrase {get;set;}
        private bool waiting {get;set;}
        private bool hideExpandedCode {get;set;}
        private string expandedCode {get; set;}
        private HashSet<TreeItem<Explanation>> TreeItems { get; set; } = new HashSet<TreeItem<Explanation>>();
        private bool shrinkTitle = false;
        private bool hasNoExplanations {
            get {
                return TreeItems.Count <= 0;
            }
        }
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

        private void acknowledgeAlert() {
            requestHasError = false;
            reasonPhrase = "";
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
            shrinkTitle = true;
            titleMargin = "mt-6";
            StateHasChanged();
        }

        private async Task DoSearch()
        {
            hideExpandedCode = true;
            waiting = false;
            requestHasError = false;
            reasonPhrase = string.Empty;

            if (string.IsNullOrEmpty(InputValue))
                return;

            ShrinkTitle();

            waiting = true;
            var code = new Code() { PowershellCode = InputValue };

            HttpResponseMessage temp;
            try {
                temp = await Http.PostAsJsonAsync<Code>("SyntaxAnalyzer", code);
            }
            catch {
                requestHasError = true;
                waiting = false;
                reasonPhrase = "oops!";
                return;
            }

            if (!temp.IsSuccessStatusCode)
            {
                requestHasError = true;
                waiting = false;
                reasonPhrase = await temp.Content.ReadAsStringAsync();
                return;
            }

            var analysisResult = await JsonSerializer.DeserializeAsync<AnalysisResult>(temp.Content.ReadAsStream());

            if (!string.IsNullOrEmpty(analysisResult.ParseErrorMessage))
            {
                requestHasError = true;
                reasonPhrase = analysisResult.ParseErrorMessage;
            }

            waiting = false;
            hideExpandedCode = false;

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
            expandedCode = analysisResult.ExpandedCode;
        }

        private string _inputValue;
    }
}