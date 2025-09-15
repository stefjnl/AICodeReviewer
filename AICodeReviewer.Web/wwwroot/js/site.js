// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

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
            pollStatus();
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

function pollStatus() {
    const interval = setInterval(() => {
        fetch('/Home/GetAnalysisStatus')
            .then(response => response.json())
            .then(data => {
                updateProgress(data.status);
                
                if (data.isComplete) {
                    clearInterval(interval);
                    hideProgress();
                    
                    if (data.error) {
                        alert('Analysis failed: ' + data.error);
                    } else {
                        // Reload page to show results
                        location.reload();
                    }
                }
            })
            .catch(error => {
                clearInterval(interval);
                hideProgress();
                alert('Error checking status: ' + error.message);
            });
    }, 1000); // Poll every second
}

function showProgress() {
    document.getElementById('progressContainer').style.display = 'block';
    document.getElementById('analysisForm').style.display = 'none';
}

function hideProgress() {
    document.getElementById('progressContainer').style.display = 'none';
    document.getElementById('analysisForm').style.display = 'block';
}

function updateProgress(status) {
    document.getElementById('progressMessage').textContent = status || 'Processing...';
}
