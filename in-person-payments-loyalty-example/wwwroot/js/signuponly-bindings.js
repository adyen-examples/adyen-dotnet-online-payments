var abortController;

// Sends POST request to url
async function sendPostRequest(url, data) {
    abortController = new AbortController(); // Used for cancelling the request
    const res = await fetch(url, {
        method: "POST",
        body: data ? JSON.stringify(data) : "",
        headers: {
            "Content-Type": "application/json",
            "Connection": "keep-alive",
            "Keep-Alive": "timeout=180, max=180"
        },
        signal: abortController.signal
    });

    return await res.json();
}

// Sends abort request to cancel an on-going transaction for the table
async function sendAbortRequest() {
    try {
        await sendGetRequest("/cash-register/abort/");
    }
    catch(error) {
        console.warn(error);
    }
}

// Shows loading animation component
function showLoadingComponent() {
    document.getElementById('loading-grid').classList.remove('disabled');
    document.getElementById('signup-only-button').classList.add('disabled');
}

// Hides loading animation component
function hideLoadingComponent() {
    document.getElementById('loading-grid').classList.add('disabled');
    document.getElementById('signup-only-button').classList.remove('disabled');
}

// Bind table selection buttons and the submit-buttons
function bindButtons() {
    // Bind `cancel-operation-button`
    const cancelOperationButton = document.getElementById('cancel-operation-button');
    cancelOperationButton.addEventListener('click', async () => {
        // Abort sending post request
        abortController.abort(); 

        await sendAbortRequest();
        
        // Hide loading animation component
        hideLoadingComponent();
    });
    
    // Bind `signup-only-button`
    const signupButton = document.getElementById('signup-only-button');
    signupButton.addEventListener('click', async () => {
        try {
            // Show loading animation component and don't allow user to select any tables
            showLoadingComponent();

            // Send card acquisition check request
            var response = await sendPostRequest("/cash-register/card-acquisition-only");
            console.log(response)

            if (response.result == "success")
            {
                window.location.href = "result/success/";
            }
            else
            {
                window.location.href = "result/failure/" + response.refusalReason;
            }
        }
        catch (error) {
            console.warn(error);

            // Hides loading animation component and allow user to select tables again
            hideLoadingComponent();
        }
    });
}

bindButtons();