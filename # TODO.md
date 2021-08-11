# TODO

- find out if `Get-ParameterSetNames.ps1` will be usefull
- check the `add_module_help.yml` pipeline 
    - can it handle 'microsoft.powershell.core' module?
    - make it handle 'Az' module, this is not currently working
- refactor `explainpowershell.aboutcollector.ps1`, make sure its output is also written to db.
- cleanup old code:
    - `classes.ps1` can be removed once refactor is complete
- add new features to the `VisitCommand()` method in [AstVisitorExplainer.cs](/workspace/explainpowershell.analysisservice/AstVisitorExplainer.cs)