## Possible Improvements
**MAKE THE ANALYZER RUN ASYNCHRONOUS.**

Presumably this could be done simply by calling `Task.Run(NamingAnalyzer.AnalyzeProject)` inside the NamingProcessor, adding some [Progress Reports](https://docs.unity3d.com/2020.1/Documentation/ScriptReference/Progress.Report.html) as it goes along, but this would need researching as I have little experience with asynchronous code.

---

**FIND NON-MATCHING CHARACTERS TO PROVIDE BETTER LOG MESSAGES.**

Right now the error message just tells you that a string is wrong, but it doesn't tell you what points of the string aren't excepted. Adding this would provide more transparency as to how the string needs to change in order to pass validation. [Use this as a starting point.](https://stackoverflow.com/questions/12383945/find-not-matching-characters-in-a-string-with-regex)

---

**RENAME TO NamingValidator**

It's just a better name for this package.

---

**Found in Edit > Project Settings > Naming Analyzer**
- Directory Wildcards (e.g. **/Prototype).
- Drag & Drop on the Exclude from Naming Analyzers list.

**Found in Asset Menu > Create > Naming Ruleset**
- Create a Type selector dropdown / generic menu - currently it's relying that Type.FullName won't find any conflicting types.
- Create a button next to "pattern" that links you to a regex tester web page with your pattern as input.
