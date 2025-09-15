US-006B: Add SignalR Real-time Updates
As a developer
I want real-time progress updates instead of polling
So that I get immediate feedback without delays
Acceptance Criteria:

SignalR hub broadcasts progress updates instantly
Replace polling with WebSocket communication
Connection established on page load
Progress messages sent to specific user only
Graceful fallback if SignalR connection fails

Technical Approach:

Add Microsoft.AspNetCore.SignalR package
Create ProgressHub with group-based messaging
Update background analysis to broadcast via hub
Replace AJAX polling with SignalR client
Use session ID for user isolation

Estimated Time: 20 minutes