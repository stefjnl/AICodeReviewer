# User Story: Pull Request Differential Review

## Story Overview
**As a** developer preparing a pull request  
**I want** to review only the code changes between my feature branch and the target branch  
**So that** I can focus on what actually changed and get relevant feedback for my PR

## Acceptance Criteria

### Core Functionality
- [ ] User can select two branches for comparison (source branch vs target branch)
- [ ] System generates diff showing only files and lines that changed between branches
- [ ] AI analysis focuses exclusively on the differential code, not the entire codebase
- [ ] Results display clearly indicates this is a branch comparison review

### Branch Selection Interface
- [ ] Dropdown/selector for source branch (defaults to current branch)
- [ ] Dropdown/selector for target branch (defaults to 'main' or 'master')
- [ ] Validation that both branches exist and are accessible
- [ ] Clear indication of which commits are being compared

### Analysis Scope
- [ ] Only modified/added files are included in the analysis
- [ ] Deleted files are noted but not analyzed
- [ ] Binary files are excluded with appropriate messaging
- [ ] Large diffs (>50,000 lines) are handled gracefully with chunking

## Technical Implementation

### Git Operations
- Use existing `LibGit2Sharp` integration
- Implement `Repository.Diff.Compare()` between branch heads
- Filter results to exclude binary and irrelevant files

### Analysis Pipeline
- Leverage existing `AnalysisOrchestrationService`
- Pass differential content to current AI analysis workflow
- No changes needed to AI service or progress tracking

### UI Integration
- Add branch selection step before current "Analysis Configuration"
- Extend existing workflow: Documents → Repository → **Branch Selection** → Language Detection → Analysis Config → Execution
- Display branch comparison metadata in results

## Definition of Done
- [ ] User can select source and target branches from dropdown
- [ ] System correctly identifies differential changes between branches
- [ ] AI analysis processes only the changed code
- [ ] Results clearly show this is a PR differential review
- [ ] Integration works with existing real-time progress tracking
- [ ] Error handling covers invalid branches and network issues

## Business Value
- **Efficiency**: Focus review time on actual changes, not entire codebase
- **Workflow Integration**: Matches standard PR review process
- **Quality Gate**: Catch issues before code reaches main branch
- **Team Velocity**: Faster, more relevant feedback cycles

## Estimated Effort
**15-20 minutes implementation**
- Modify existing repository selection workflow
- Add branch comparison logic using existing Git service
- Extend UI with branch selection dropdowns
- Test end-to-end with feature branch scenario

## Dependencies
- Existing Git integration (`RepositoryManagementService`)
- Current analysis pipeline (no changes required)
- Frontend workflow components (extend, don't rebuild)