# Project Explorer 2026

Project Explorer is a tool window that lists **every project below a search folder**, whether or not
it is part of the current solution. Find a project in seconds, then add it to the solution, unload
it, reload it or remove it again.

It is made for large repositories where a solution only ever contains a subset of all projects.

Supports Visual Studio 2022 and Visual Studio 2026.

## Getting started

1. Open the tool window via **View › Other Windows › Project Explorer**.
2. Choose a search folder: click the settings button in the tool window's toolbar and select **Open**.
3. Project Explorer scans the folder and lists every project that Visual Studio can open.

## All projects at a glance

Project Explorer recognizes every project type installed in your Visual Studio. The scan runs in the
background, can be cancelled at any time and is repeated with **Refresh**.

![Project Explorer tool window](https://raw.githubusercontent.com/IInspectable/ProjectExplorer/master/_art/pe.png)

## Find projects fast

Just start typing. The filter uses the same pattern matching as Visual Studio's *Go To*, so parts of
a name or camel-case abbreviations are enough.

Tip: under *Tools › Options › Environment › Keyboard*, search for **ActivateProjectExplorerSearch**
and assign a shortcut to jump straight into the search box.

![Filter projects by name](https://raw.githubusercontent.com/IInspectable/ProjectExplorer/master/_art/PatternMatching.gif)

## Add, remove, unload and reload projects

Use the toolbar or the context menu to add projects to the solution, remove them, unload or reload
them, or open their folder in File Explorer. Multiple projects can be selected at once.

A **double-click** or **Enter** performs the obvious action:

| Project status | Action              |
|----------------|---------------------|
| Closed         | Add to solution     |
| Unloaded       | Reload project      |
| Loaded         | Unload project      |

![Context menu for a closed project](https://raw.githubusercontent.com/IInspectable/ProjectExplorer/master/_art/pe_context_menu.png)
![Context menu for a loaded project](https://raw.githubusercontent.com/IInspectable/ProjectExplorer/master/_art/pe_context_menu_loaded.png)

## See the status of every project

Loaded, unloaded and closed projects are clearly distinguished, so you always know what is part of
your solution.

![Project status](https://raw.githubusercontent.com/IInspectable/ProjectExplorer/master/_art/pe_project_status.png)

## Feedback

Found a bug or have an idea? Source code and issues are on
[GitHub](https://github.com/IInspectable/ProjectExplorer).
