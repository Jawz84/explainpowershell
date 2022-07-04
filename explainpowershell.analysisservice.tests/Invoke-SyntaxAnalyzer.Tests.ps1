using namespace Microsoft.PowerShell.Commands

Describe "Invoke-SyntaxAnalyzer" {
    BeforeAll {
        . $PSCommandPath.Replace('.Tests.ps1', '.ps1')
        . $PSScriptRoot/Start-FunctionApp.ps1
        . $PSScriptRoot/Test-IsAzuriteUp.ps1
    }

    It "Should explain Classes and constructors" {
        $code = 'class Person {[int]$age ; Person($a) {$this.age = $a}}; class Child : Person {[string]$School; Child([int]$a, [string]$s ) : base($a) { $this.School = $s}}'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -BeLike "Defines a 'class', with the name 'Person'. A class is a blueprint for a type.*"
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_Classes"
    }

    It "Should display correct help for assigment operators" {
        $code = '$D=[Datetime]::Now'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -Be "The assignment operator '='. Assigns a value to '`$D'."
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_Assignment_Operators"
    }

    It "Should correctly explain static properties on classes" {
        $code = '[Datetime]::Now'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -Be "Access the static property 'Now' on class '[Datetime]'"
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_Properties"
    }

    It "Should not fail explaining ``. {gci -path 'sdf'}``" {
        $code = '. {gci -path "sdf"}'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations | Should -Not -BeNullOrEmpty
    }

    It "Should not fail explaining ``. {}``" {
        $code = '. {}'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations | Should -Not -BeNullOrEmpty
    }

    It "Switches to the right module on demand" {
        $code = 'myTestModule\get-testinfo'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].HelpResult.ModuleName | Should -BeExactly 'myTestModule'
        $content.Explanations[0].CommandName | Should -BeExactly 'Get-TestInfo'
        $content.Explanations[0].HelpResult.ModuleProjectUri | Should -BeExactly 'https://www.explainpowershell.com'
    }

    It "Warns the user if a command is present in more than one module" {
        $code = 'get-testinfo'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.ParseErrorMessage | Should -BeLike "The command 'get-testinfo' is present in more than one module:*"
    }

    It "Doesn't warn the user if a command is present in more than one module, but the module is specified" {
        $code = 'myTestModule\get-testinfo'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.ParseErrorMessage | Should -BeNullOrEmpty
    }

    It "Doesn't warn the user if a command is not present in more than one module" {
        $code = 'get-childitem'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.ParseErrorMessage | Should -BeNullOrEmpty
    }

    It "Provides descriptions for parameters, even if some values are 'null' in the underlying json" {
        $code = 'get-help -full'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].Description | Should -BeLike "Displays the entire help article for a cmdlet.*"
    }

    It "Provides descriptions for parameters, also where the parameter appears to be abmiguous, but one of them is a dynamic parameter (static params take precedence)" {
        $code = 'get-childitem -r'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].Description | Should -BeExactly "Gets the items in the specified locations and in all child items of the locations.`nThis parameter is present in all parameter sets."
        $content.Explanations[1].CommandName | Should -Not -BeNullOrEmpty
    }

    It "Provides descriptions for parameters" {
        $code = 'get-childitem -path "foo"'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].Description | Should -BeExactly "Specifies a path to one or more locations. Wildcards are accepted. The default location is the current directory (``.``).`nThis parameter is in parameter set: Items"
        $content.Explanations[1].CommandName | Should -BeExactly "Parameter of type [System.String[]] (supports wildcards like '*' and '?')"
    }

    It "Does not provide descriptions for non-existing parameters" {
        $code = 'get-childitem -bar "foo"'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].Description | Should -BeNullOrEmpty
        $content.Explanations[1].CommandName | Should -BeExactly "Parameter"
    }

    It "Explains a hash table" {
        $code = '[ordered]@{key1 = "value"}'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].Description | Should -BeExactly "An object that holds key-value pairs, optimized for hash-searching for keys. This hash table has the following keys: 'key1'"
        $content.Explanations[1].CommandName | Should -BeExactly "Hash table"
        $content.Explanations[1].HelpResult.DocumentationLink | Should -Match "about_hash_tables"
    }


    It "Should not generate identical explanationIds" {
        $code = 'Get-aduser -Filter { $_.SamAccountName -like "*demo*" -and $_.PasswordNeverExpires -eq $true }'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        ($content.Explanations.Id | Sort-Object -Unique ).Count | Should -BeExactly $content.Explanations.Id.Count
    }

    It "Explains stream redirection all streams" {
        $code = '"foo" *>&1'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].Description | Should -BeExactly "Redirects output from stream 'All' to stream 'Output'."
        $content.Explanations[1].CommandName | Should -BeExactly "Stream redirection operator"
        $content.Explanations[1].HelpResult.DocumentationLink | Should -Match "about_redirection"
    }


    It "Explains file redirection all streams" {
        $code = '"foo" > .\file.txt'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].Description | Should -BeExactly "Redirects output to location '.\file.txt'."
        $content.Explanations[1].CommandName | Should -BeExactly "File redirection operator"
        $content.Explanations[1].HelpResult.DocumentationLink | Should -Match "about_redirection"
    }

    It "Explains file redirection all streams" {
        $code = '"foo" >> .\file.txt'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].Description | Should -BeExactly "Appends output to location '.\file.txt'."
        $content.Explanations[1].CommandName | Should -BeExactly "File redirection operator"
        $content.Explanations[1].HelpResult.DocumentationLink | Should -Match "about_redirection"
    }

    It "Explains file redirection error stream" {
        $code = '"foo" 2> .\file.txt'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].Description | Should -BeExactly "Redirects output from stream 'Error' to location '.\file.txt'."
        $content.Explanations[1].CommandName | Should -BeExactly "File redirection operator"
        $content.Explanations[1].HelpResult.DocumentationLink | Should -Match "about_redirection"
    }

    It "Explains file redirection error stream" {
        $code = '"foo" *> .\file.txt'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].Description | Should -BeExactly "Redirects output from stream 'All' to location '.\file.txt'."
        $content.Explanations[1].CommandName | Should -BeExactly "File redirection operator"
        $content.Explanations[1].HelpResult.DocumentationLink | Should -Match "about_redirection"
    }

    It "Explains array expression" {
        $code = '@(123, gci)'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -BeExactly "The array sub-expression operator creates an array from the statements inside it. Whatever the statement inside the operator produces, the operator will place it in an array. Even if there is zero or one object."
        $content.Explanations[0].CommandName | Should -BeExactly "Array expression"
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_arrays#the-array-sub-expression-operator"
    }

    It "Explains a function member" {
        $code = 'class foo { hidden [string] func() {return "bar"} }'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].Description | Should -BeExactly "A hidden method 'func' that returns type 'string'."
        $content.Explanations[1].CommandName | Should -BeExactly "Method member"
        $content.Explanations[1].HelpResult.DocumentationLink | Should -Match "about_classes#class-methods"
    }

    It "Explains a pipeline chain" {
        $code = '123 && "sdfs"'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -BeExactly "The '&&' operator executes the right-hand pipeline, if the left-hand pipeline succeeded."
        $content.Explanations[0].CommandName | Should -BeExactly "Pipeline chain"
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_Pipeline_Chain_Operators"
    }

    It "Explains a property member" {
        $code = 'class foo { [validatenotnull()] [string] $bar }'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].Description | Should -BeExactly "Property 'bar' of type 'string', with attributes 'validatenotnull'."
        $content.Explanations[1].CommandName | Should -BeExactly "Property member"
        $content.Explanations[1].HelpResult.DocumentationLink | Should -Match "about_classes#class-properties"
    }

    It "Explains enum declaration" {
        $code = 'enum foo {bar = 1}'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].Description | Should -Match "Enum label 'bar', with value '1'."
        $content.Explanations[1].CommandName | Should -BeExactly "Property member"
        $content.Explanations[1].HelpResult.DocumentationLink | Should -Match "about_enum"
    }

    It "Explains the ternary operator" {
        $code = '42 ? "true" : "false"'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -BeExactly "A condensed if-else construct, used for simple situations."
        $content.Explanations[0].CommandName | Should -BeExactly "Ternary expression"
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_if#using-the-ternary-operator-syntax"
    }

    It "Explains class declaration" {
        $code = 'class foo {}'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -Match "'class'.*'foo'\..*\[foo\]::new\(\)"
        $content.Explanations[0].CommandName | Should -BeExactly "Type definition"
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_classes"
    }

    It "Explains enum declaration" {
        $code = 'enum foo {bar = 1}'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -Match "'enum'.*'foo'\..*enumeration"
        $content.Explanations[0].CommandName | Should -BeExactly "Type definition"
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_enum"
    }

    It "Explains enum declaration" {
        $code = '[flags()] enum foo {bar = 1}'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -Match "'enum'.*'foo'.*'flags'.*enumeration"
        $content.Explanations[0].CommandName | Should -BeExactly "Type definition"
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_enum"
    }

    It "Explains using statements" {
        $code = 'using namespace system.text'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -Match "a namespace"
        $content.Explanations[0].CommandName | Should -BeExactly "using statement"
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_using"
    }

    It "Explains CmdletBinding attribute" {
        $code = '[CmdletBinding()] param()'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].Description | Should -BeExactly "The CmdletBinding attribute adds common parameters to your script or function, among other things."
        $content.Explanations[1].CommandName | Should -BeExactly "CmdletBinding Attribute"
        $content.Explanations[1].HelpResult.DocumentationLink | Should -Match "about_Functions_CmdletBindingAttribute"
    }

    It "Explains type accelerators" {
        $code = '[string]$mytext = 123' 
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[2].Description | Should -BeExactly "Constrains the type to 'string', which is a type accelerator for 'System.String'"
        $content.Explanations[2].CommandName | Should -BeExactly "Type accelerator"
        $content.Explanations[2].HelpResult.DocumentationLink | Should -Match "about_type_accelerators"
    }

    It "Explains the While statement" {
        $code = 'while ($abc -lt 29) {$abc++}' 
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations.Description.Count | Should -BeExactly 5 
    }

    It "In the explanation, should correctly show a splatted variable with an @ sign at the beginning" {
        $code = 'gci @splat' 
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[1].OriginalExtent | Should -BeExactly '@splat' 
    }

    It "Explains Try catch finally" {
        $code = 'try {gci -name} catch [ArgumentNullException], [ArgumentException]  {"blah"} catch {"foo"} finally {remove-variable a}'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations.Count | Should -BeExactly 9
    }

    It "Calculates Id and ParentId for Explanations" {
        $code = "if (`$abc -eq 123) {}";
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Id | Should -Not -BeNullOrEmpty
        $content.Explanations[0].ParentId | Should -BeNullOrEmpty
        $content.Explanations[1].ParentId | Should -Be $content.Explanations[0].Id
    }

    It "Gets help article links for known commands and About_.. articles" {
        $code = "if (`$abc -eq 123) {Get-ChildItem}";
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].HelpResult.DocumentationLink | Should -Match "about_If"
        $content.Explanations[3].HelpResult.DocumentationLink | Should -Match "get-childitem"
    }

    It "Explains numeric constants" {
        $code = "0b0110; 0xAB234F; 12e-3";
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -BeExactly "Binary number (value: 6)"
        $content.Explanations[1].Description | Should -BeExactly "Hexadecimal number (value: 11215695)"
        $content.Explanations[2].Description | Should -BeLikeExactly "Number (value: 0?012)"
        $content.Explanations[2].HelpResult.DocumentationLink | Should -Match "about_Numeric_Literals"
    }

    It "Converts alias to full command name" {
        $code = 'gci'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.ExpandedCode | Should -BeExactly 'Get-ChildItem'
    }

    It "Converts multiple aliases to full command names" {
        $code = 'gps | select name | ? name -like dotnet'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.ExpandedCode | Should -BeExactly 'Get-Process | Select-Object name | Where-Object name -like dotnet'
    }

    It "Notifies user about detected syntax errors, while still explaining as much as possible" {
        $code = 'gps | ? {$_.id'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $result.StatusCode | Should -Be 200
        $content.ParseErrorMessage | Should -BeExactly "Missing closing '}' in statement block or type definition."
        $content.ExpandedCode | Should -BeExactly 'Get-Process | Where-Object {$_.id'
    }

    It "works with if statements" {
        $code = 'if ($abc -eq 123) {gci -name}'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $result.StatusCode | Should -Be 200
        $content.ExpandedCode | Should -BeExactly 'if ($abc -eq 123) {Get-ChildItem -name}'
    }

    It "Explains variables, even complex ones" {
        $code = '$abc ; $env:path ; @splatted ; $script:myVar'
        [BasicHtmlWebResponseObject]$result = Invoke-SyntaxAnalyzer -PowerShellCode $code
        $content = $result.Content | ConvertFrom-Json
        $content.Explanations[0].Description | Should -BeExactly "A variable named 'abc'"
        $content.Explanations[1].Description | Should -BeExactly "An environment variable named 'path' (on PSDrive 'env:')"
        $content.Explanations[2].Description | Should -BeExactly "A splatted variable named 'splatted'"
    }

    $testCase = (Get-Content "$PSScriptRoot\testfiles\oneliners.ps1").split("`n") | ForEach-Object {
        [psobject]@{
            PowerShellCode = $_
        }
    }

    It "Can handle all kinds of different oneliners without freaking out: <PowerShellCode>" -ForEach $testCase {
            $result = Invoke-SyntaxAnalyzer -PowerShellCode $PowerShellCode
            $result.StatusCode | Should -Be 200
    }
}
