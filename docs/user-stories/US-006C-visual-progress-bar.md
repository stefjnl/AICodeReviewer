US-006C: Visual Progress Bar with Step Indicators
As a developer
I want to see a visual progress bar with checkmarks
So that I know exactly which step is running and what's already complete
Acceptance Criteria:

Bootstrap progress bar shows 0% → 25% → 50% → 75% → 100%
4 distinct steps with icons: Reading changes → Loading docs → AI analysis → Generating report
Current step highlighted with spinner icon
Completed steps show green checkmark
Progress bar fills smoothly as steps complete

Technical Approach:

Update progress UI with Bootstrap progress bar
Add 4-step visual indicators with icons
SignalR sends step number + completion status
CSS animations for smooth transitions
Icons: ⏳ (in progress) → ✅ (complete)

Estimated Time: 15 minutes