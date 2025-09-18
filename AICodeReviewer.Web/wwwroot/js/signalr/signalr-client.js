// AI Code Reviewer - SignalR Client

import { apiEndpoints } from '../core/constants.js';
import { updateSignalRStatus } from './signalr-ui.js';

let signalRConnection = null;

export function initializeSignalR() {
    try {
        console.log('🔗 Initializing SignalR connection...');
        
        // Create SignalR connection
        signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl(apiEndpoints.progressHub)
            .withAutomaticReconnect([0, 2000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();
        
        // Connection event handlers
        signalRConnection.start()
            .then(() => {
                console.log('✅ SignalR connected successfully');
                updateSignalRStatus('Connected', true);
                
                // Register for progress updates (including completion and errors)
                signalRConnection.on("UpdateProgress", (progress) => {
                    if (progress && progress.isComplete) {
                        if (progress.error) {
                            handleError({ message: progress.error });
                        } else {
                            handleAnalysisComplete(progress);
                        }
                    } else {
                        handleProgressUpdate(progress);
                    }
                });
            })
            .catch(err => {
                console.error('❌ SignalR connection failed:', err);
                updateSignalRStatus('Connection failed', false);
            });
        
        // Reconnection event handlers
        signalRConnection.onreconnecting(() => {
            console.warn('🔄 SignalR reconnecting...');
            updateSignalRStatus('Reconnecting...', false);
        });
        
        signalRConnection.onreconnected(() => {
            console.log('✅ SignalR reconnected');
            updateSignalRStatus('Connected', true);
        });
        
    } catch (error) {
        console.error('❌ SignalR initialization error:', error);
        updateSignalRStatus('Initialization error', false);
    }
}

// Handle progress updates from SignalR
function handleProgressUpdate(progress) {
    console.log('📊 Progress update:', progress);
    
    // Dispatch custom event for components to listen to
    const event = new CustomEvent('progress-update', { detail: progress });
    document.dispatchEvent(event);
}

// Handle analysis completion
function handleAnalysisComplete(results) {
    console.log('✅ Analysis complete:', results);
    
    // Transform the results to match what the frontend expects
    if (results && results.result) {
        // This is a ProgressDto object from the backend
        // Try to parse the structured results from the backend
        let structuredResults;
        try {
            structuredResults = JSON.parse(results.result);
        } catch (e) {
            console.warn('Failed to parse structured results, using raw data:', e);
            // Fallback to basic transformation
            structuredResults = {
                summary: {
                    totalIssues: 0,
                    critical: 0,
                    warnings: 0
                },
                feedback: [],
                detailedResults: results
            };
        }
        
        if (structuredResults && structuredResults.feedback) {
            // This is properly structured data from the backend
            const feedback = structuredResults.feedback || [];
            
            // Transform feedback items to match frontend expectations
            const transformedFeedback = feedback.map(item => {
                // Convert numeric severity enum to string
                let severityString = 'Suggestion';
                if (typeof item.severity === 'number' || typeof item.Severity === 'number') {
                    const severityValue = item.severity ?? item.Severity;
                    switch (severityValue) {
                        case 0: severityString = 'Critical'; break;
                        case 1: severityString = 'Warning'; break;
                        case 2: severityString = 'Suggestion'; break;
                        case 3: severityString = 'Style'; break;
                        case 4: severityString = 'Info'; break;
                        default: severityString = 'Suggestion';
                    }
                } else {
                    severityString = item.severity || item.Severity || 'Suggestion';
                }
                
                return {
                    title: item.message ? item.message.substring(0, 50) + (item.message.length > 50 ? '...' : '') : 'Untitled Issue',
                    description: item.message || '',
                    severity: severityString,
                    suggestions: item.suggestion ? [item.suggestion] : [],
                    file: item.filePath || item.FilePath || '',
                    line: item.lineNumber || item.LineNumber || ''
                };
            });
            
            const transformedResults = {
                summary: {
                    totalIssues: feedback.length,
                    critical: feedback.filter(f => (f.severity || f.Severity) === 'Critical').length,
                    warnings: feedback.filter(f => (f.severity || f.Severity) === 'Warning').length
                },
                feedback: transformedFeedback,
                detailedResults: structuredResults
            };
            
            // Dispatch custom event for components to listen to
            const event = new CustomEvent('analysis-complete', { detail: transformedResults });
            document.dispatchEvent(event);
        } else {
            // Fallback to basic transformation
            const transformedResults = {
                summary: {
                    totalIssues: 0,
                    critical: 0,
                    warnings: 0
                },
                feedback: [],
                detailedResults: results
            };
            
            const event = new CustomEvent('analysis-complete', { detail: transformedResults });
            document.dispatchEvent(event);
        }
    } else {
        // Dispatch custom event for components to listen to
        const event = new CustomEvent('analysis-complete', { detail: results });
        document.dispatchEvent(event);
    }
}

// Handle errors from SignalR
function handleError(error) {
    console.error('❌ SignalR error:', error);
    
    // Dispatch custom event for components to listen to
    const event = new CustomEvent('signalr-error', { detail: error });
    document.dispatchEvent(event);
}

// Getter for the SignalR connection
export function getSignalRConnection() {
    return signalRConnection;
}