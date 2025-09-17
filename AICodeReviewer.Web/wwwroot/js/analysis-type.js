/**
 * Analysis Type Functions - Handles analysis type selection and validation
 */

document.addEventListener('DOMContentLoaded', function() {
    const uncommittedRadio = document.getElementById('uncommittedChanges');
    const stagedRadio = document.getElementById('stagedChanges');
    const commitRadio = document.getElementById('specificCommit');
    const singleFileRadio = document.getElementById('singleFile');
    const commitIdContainer = document.getElementById('commitIdContainer');
    const commitIdInput = document.getElementById('commitId');
    const filePickerContainer = document.getElementById('filePickerContainer');
    const filePathInput = document.getElementById('filePath');
    
    // Check if elements exist before proceeding
    if (!uncommittedRadio || !stagedRadio || !commitRadio || !singleFileRadio) {
        console.log('[AnalysisType] Radio buttons not found, skipping initialization');
        return;
    }
    
    function toggleAnalysisTypeFields() {
        const gitDiffSection = document.getElementById('gitDiffSection');
        const stagedAnalysisInfo = document.getElementById('stagedAnalysisInfo');
        const commitAnalysisInfo = document.getElementById('commitAnalysisInfo');
        const singleFileAnalysisInfo = document.getElementById('singleFileAnalysisInfo');
        
        // Initialize on first call
        if (!gitDiffSection || !stagedAnalysisInfo || !commitAnalysisInfo || !singleFileAnalysisInfo) {
            return;
        }
        
        if (commitRadio.checked) {
            commitIdContainer.style.display = 'block';
            commitIdInput.required = true;
            filePickerContainer.style.display = 'none';
            filePathInput.required = false;
            
            // Hide git diff section and show commit analysis info
            gitDiffSection.style.display = 'none';
            stagedAnalysisInfo.style.display = 'none';
            commitAnalysisInfo.style.display = 'block';
            singleFileAnalysisInfo.style.display = 'none';
        } else if (singleFileRadio.checked) {
            commitIdContainer.style.display = 'none';
            commitIdInput.required = false;
            commitIdInput.value = '';
            document.getElementById('commitValidationResult').innerHTML = '';
            filePickerContainer.style.display = 'block';
            filePathInput.required = true;
            
            // Hide git diff section and show single file analysis info
            gitDiffSection.style.display = 'none';
            stagedAnalysisInfo.style.display = 'none';
            commitAnalysisInfo.style.display = 'none';
            singleFileAnalysisInfo.style.display = 'block';
        } else if (stagedRadio.checked) {
            commitIdContainer.style.display = 'none';
            commitIdInput.required = false;
            commitIdInput.value = '';
            document.getElementById('commitValidationResult').innerHTML = '';
            filePickerContainer.style.display = 'none';
            filePathInput.required = false;
            filePathInput.value = '';
            document.getElementById('fileValidationResult').innerHTML = '';
            
            // Hide git diff section and show staged analysis info
            gitDiffSection.style.display = 'none';
            stagedAnalysisInfo.style.display = 'block';
            commitAnalysisInfo.style.display = 'none';
            singleFileAnalysisInfo.style.display = 'none';
        } else {
            commitIdContainer.style.display = 'none';
            commitIdInput.required = false;
            commitIdInput.value = '';
            document.getElementById('commitValidationResult').innerHTML = '';
            filePickerContainer.style.display = 'none';
            filePathInput.required = false;
            filePathInput.value = '';
            document.getElementById('fileValidationResult').innerHTML = '';
            
            // Show git diff section and hide analysis info
            gitDiffSection.style.display = 'block';
            stagedAnalysisInfo.style.display = 'none';
            commitAnalysisInfo.style.display = 'none';
            singleFileAnalysisInfo.style.display = 'none';
        }
    }
    
    // Initialize the UI state based on the default radio button selection
    toggleAnalysisTypeFields();
    
    uncommittedRadio.addEventListener('change', toggleAnalysisTypeFields);
    stagedRadio.addEventListener('change', toggleAnalysisTypeFields);
    commitRadio.addEventListener('change', toggleAnalysisTypeFields);
    singleFileRadio.addEventListener('change', toggleAnalysisTypeFields);
    
    // Validate commit button handler
    const validateCommitBtn = document.getElementById('validateCommit');
    if (validateCommitBtn) {
        validateCommitBtn.addEventListener('click', async function() {
            const commitId = commitIdInput.value.trim();
            const validationResult = document.getElementById('commitValidationResult');
            const repositoryPath = window.repositoryPath || ''; // Will be set by the page
            
            if (!commitId) {
                validationResult.innerHTML = '<span class="text-warning">Please enter a commit ID</span>';
                return;
            }
            
            validationResult.innerHTML = '<span class="text-info">Validating...</span>';
            
            try {
                const response = await fetch('/Home/ValidateCommit', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        commitId: commitId,
                        repositoryPath: repositoryPath
                    })
                });
                
                const result = await response.json();
                if (result.success) {
                    validationResult.innerHTML = '<span class="text-success">✓ Valid commit: ' + result.message + '</span>';
                } else {
                    validationResult.innerHTML = '<span class="text-danger">✗ ' + result.error + '</span>';
                }
            } catch (error) {
                validationResult.innerHTML = '<span class="text-danger">✗ Validation failed: ' + error.message + '</span>';
            }
        });
    }

    // File validation function
    function validateFilePath(filePath) {
        const validationResult = document.getElementById('fileValidationResult');
        
        if (!filePath) {
            validationResult.innerHTML = '<span class="text-warning">Please select or enter a file path</span>';
            return false;
        }
        
        const allowedExtensions = ['.cs', '.js', '.py'];
        const extension = filePath.toLowerCase().substring(filePath.lastIndexOf('.'));
        
        if (!allowedExtensions.includes(extension)) {
            validationResult.innerHTML = '<span class="text-danger">✗ Unsupported file type. Allowed: ' + allowedExtensions.join(', ') + '</span>';
            return false;
        }
        
        validationResult.innerHTML = '<span class="text-success">✓ File type supported</span>';
        return true;
    }

    // File path input validation
    if (filePathInput) {
        filePathInput.addEventListener('input', function() {
            const filePath = this.value.trim();
            if (filePath) {
                validateFilePath(filePath);
            } else {
                document.getElementById('fileValidationResult').innerHTML = '';
            }
        });
    }

    // Browse file button handler - triggers hidden file input
    const browseFileBtn = document.getElementById('browseFile');
    const fileInput = document.getElementById('fileInput');
    if (browseFileBtn && fileInput) {
        browseFileBtn.addEventListener('click', function() {
            fileInput.click();
        });

        // Handle file selection from file input
        fileInput.addEventListener('change', function(event) {
            const file = event.target.files[0];
            if (file) {
                // Browsers don't provide full path for security reasons
                const fileNameOnly = file.name;
                
                // Clear the input and show a message to user
                filePathInput.value = '';
                
                // Store the file object for potential future use
                
                // Provide feedback about the selected file and instructions
                const validationResult = document.getElementById('fileValidationResult');
                validationResult.innerHTML = '<span class="text-info">📁 File selected: ' + file.name + ' (' + Math.round(file.size / 1024) + ' KB)</span><br>' +
                    '<span class="text-warning">⚠️ Please enter the full file path above (browsers don\'t provide full paths for security)</span>';
                
                // Focus on the file path input to encourage manual entry
                filePathInput.focus();
            }
        });
    }

    // Allow drag and drop for file selection (enhanced UX)
    if (filePathInput) {
        filePathInput.addEventListener('dragover', function(event) {
            event.preventDefault();
            this.style.backgroundColor = '#f0f0f0';
            this.style.borderColor = '#007bff';
        });

        filePathInput.addEventListener('dragleave', function(event) {
            event.preventDefault();
            this.style.backgroundColor = '';
            this.style.borderColor = '';
        });

        filePathInput.addEventListener('drop', function(event) {
            event.preventDefault();
            this.style.backgroundColor = '';
            this.style.borderColor = '';
            
            const files = event.dataTransfer.files;
            if (files.length > 0) {
                const file = files[0];
                const fileNameOnly = file.name;
                
                // Clear the input and show a message to user
                this.value = '';
                
                // Store the file object for potential future use
                
                // Provide feedback about the selected file and instructions
                const validationResult = document.getElementById('fileValidationResult');
                validationResult.innerHTML = '<span class="text-info">📁 File dropped: ' + file.name + ' (' + Math.round(file.size / 1024) + ' KB)</span><br>' +
                    '<span class="text-warning">⚠️ Please enter the full file path above (drag & drop only provides filename)</span>';
                
                // Focus on the file path input to encourage manual entry
                this.focus();
            }
        });
    }
});