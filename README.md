## Possible Improvements
MAKE THE ANALYZER RUN ASYNCHRONOUS.
Presumably this could be done simply by calling `Task.Run(NamingAnalyzer.AnalyzeProject)` inside the NamingProcessor, adding some [Progress Reports](https://docs.unity3d.com/2020.1/Documentation/ScriptReference/Progress.Report.html) as it goes along, but this would need researching as I have little experience with asynchronous code.

**Found in Edit > Project Settings > Naming Analyzer**
- Directory Wildcards (e.g. **/Prototype).
- Drag & Drop on the Exclude from Naming Analyzers list.

**Found in Asset Menu > Create > Naming Ruleset**
- Create a Type selector dropdown / generic menu - currently it's relying that Type.FullName won't find any conflicting types.
- Create a button next to "pattern" that links you to a regex tester web page with your pattern as input.
