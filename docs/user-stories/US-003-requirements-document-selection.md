US-003: Requirements Document Selection

As a developer
I want to select a requirements document from my file system
So that the AI understands what I'm building
Acceptance Criteria:

File picker allows selection of .txt, .md, .pdf files
Selected file path displayed on form
Basic validation that file exists and is readable
Remember last selected file in session


Files: DocumentService.cs, updated HomeController.cs, updated Index.cshtml