
// Add these global variables at top
let signalRConnection = null;
let currentAnalysisId = null;

// Initialize SignalR when page loads
document.addEventListener('DOMContentLoaded', function() {
    initializeSignalR();
});

// Initialize SignalR connection
function initializeSignalR() {
    signalRConnection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/progress")
        .build();

    signalRConnection.on("UpdateProgress", function (data) {
        document.getElementById('progressMessage').innerText = data.status;
        
        if (data.result) {
            document.getElementById('result').innerText = data.result;
            document.getElementById('analysisResult').style.display = 'block';
        }
        
        if (data.error) {
            document.getElementById('result').innerText = '❌ Error: ' + (data.error || 'Unknown error');
            document.getElementById('analysisResult').style.display = 'block';
        }
        
        if (data.isComplete) {
            hideProgress();
            if (currentAnalysisId && signalRConnection) {
                signalRConnection.invoke("LeaveAnalysisGroup", currentAnalysisId)
                    .catch(err => console.error("Failed to leave SignalR group:", err));
            }
        }
    });

    signalRConnection.start().then(function () {
        console.log("SignalR connected successfully");
    }).catch(function (err) {
        console.error("SignalR connection failed:", err);
        // Fallback to polling if SignalR fails
        startPollingFallback();
    });
}

// Fallback polling function (keep existing pollStatus logic)
function startPollingFallback() {
    console.log("Using polling fallback");
    if (currentAnalysisId) {
        pollStatus(currentAnalysisId); // Your existing polling function
    } else {
        console.error("No analysisId available for fallback polling");
        hideProgress();
    }
}

// US-006A: Async analysis with progress updates
function startAnalysis() {
    // Validate form data first
    const repositoryPath = document.querySelector('input[name="repositoryPath"]')?.value;
    const apiKey = document.querySelector('input[name="apiKey"]')?.value;
    
    if (!repositoryPath) {
        alert('Please provide repository path');
        return;
    }
    
    // Show progress immediately
    showProgress();
    
    // Prepare JSON data - get selected documents from checkboxes
    const selectedDocuments = [];
    const checkboxes = document.querySelectorAll('input[name="selectedDocuments"]:checked');
    checkboxes.forEach(checkbox => {
        selectedDocuments.push(checkbox.value);
    });
    
    const formData = {
        repositoryPath: repositoryPath,
        selectedDocuments: selectedDocuments,
        documentsFolder: '' // Will use session default
    };
    
    // Start analysis with JSON
    fetch('/Home/RunAnalysis', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(formData)
    })
    .then(response => {
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
    })
    .then(data => {
        if (data.success) {
            currentAnalysisId = data.analysisId;
            if (signalRConnection && signalRConnection.state === signalR.HubConnectionState.Connected) {
                signalRConnection.invoke("JoinAnalysisGroup", currentAnalysisId)
                    .catch(err => {
                        console.error("Failed to join SignalR group:", err);
                        startPollingFallback();
                    });
            } else {
                startPollingFallback();
            }
        } else {
            hideProgress();
            alert('Error starting analysis: ' + (data.error || 'Unknown error'));
        }
    })
    .catch(error => {
        hideProgress();
        alert('Network error: ' + error.message);
    });
}

function pollStatus(analysisId) {
    const interval = setInterval(() => {
        fetch(`/Home/GetAnalysisStatus?analysisId=${encodeURIComponent(analysisId)}`)
            .then(response => response.json())
            .then(data => {
                updateProgress(data.status);
                
                if (data.isComplete) {
                    clearInterval(interval);
                    hideProgress();
                    
                    // SHOW THE RESULT CARD
                    document.getElementById('analysisResult').style.display = 'block';

                    // DISPLAY THE RESULT TEXT
                    if (data.status === 'Complete') {
                        document.getElementById('result').innerText = data.result;
                    } else {
                        document.getElementById('result').innerText = '❌ Error: ' + (data.error || 'Unknown error');
                    }
                    return; // Stop polling
                }
            })
            .catch(error => {
                clearInterval(interval);
                hideProgress();

                // Show error result card
                document.getElementById('analysisResult').style.display = 'block';
                document.getElementById('result').innerText = '❌ Error checking status: ' + error.message;
            });
    }, 1000); // Poll every second
}

function showProgress() {
    document.getElementById('progressContainer').style.display = 'block';
    document.getElementById('analysisForm').style.display = 'none';
    document.getElementById('analysisResult').style.display = 'none'; // Hide result during progress
}

function hideProgress() {
    document.getElementById('progressContainer').style.display = 'none';
    document.getElementById('analysisForm').style.display = 'block';
}

function updateProgress(status) {
    document.getElementById('progressMessage').textContent = status || 'Processing...';
    document.getElementById('status').textContent = status || 'Processing...';
}
