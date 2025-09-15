US-006A: Fix Browser Timeout with Basic Progress
As a developer
I want analysis to run in the background without browser timeout
So that I can start long-running analysis without the page freezing
Acceptance Criteria:

Click "Run Analysis" returns immediately (no browser timeout)
Shows current step as text message ("Reading git changes...", "AI analysis...", etc.)
Status updates every second via polling
Page refreshes automatically when analysis complete
Error states handled gracefully

Technical Approach:

Add async controller endpoints (StartAnalysisAsync, GetAnalysisStatus)
Background Task.Run() for analysis
Session-based status storage
Simple AJAX polling every 1 second

Estimated Time: 15 minutes