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

// Sends GET request to URL
async function sendGetRequest(url) {
    abortController = new AbortController(); // Used for cancelling the request
    const res = await fetch(url, {
        method: "Get",
        headers: {
            "Content-Type": "application/json",
            "Connection": "keep-alive",
            "Keep-Alive": "timeout=180, max=180"
        }
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

// Shows loading animation component and deactivates the table selection
function showLoadingComponent() {
    document.getElementById('loading-grid').classList.remove('disabled');
    document.getElementById('pizzas-section').classList.add('disabled');
}

// Hides loading animation component and shows table selection selection
function hideLoadingComponent() {
    document.getElementById('loading-grid').classList.add('disabled');
    document.getElementById('pizzas-section').classList.remove('disabled');
}

// Bind table selection buttons and the submit-buttons
function bindButtons() {
    // Bind `card-acquisition-request-form` submit-button
    const cardAcquisitionRequestForm = document.getElementById('card-acquisition-request-form');
    cardAcquisitionRequestForm.addEventListener('submit', async function(event) {
        event.preventDefault();

        var formData = new FormData(event.target);
        var pizzaName = formData.get('pizzaName');

        if (!pizzaName) {
            alert("Please select a pizza first.");
            return;
        }
        
        try {
            // Show loading animation component which doesn't allow users to select any tables
            showLoadingComponent();           

            // Send card acquisition payment request
            var response = await sendGetRequest("/cash-register/create/" + pizzaName);
            console.log(response);

            if (response.result === "success")
            {
                // Hides loading animation component and allow user to select tables again
                hideLoadingComponent();
                
                window.location.href = "result/success";
            }
            else
            {
                window.location.href = "result/failure/" + response.refusalReason;
            }
        }
        catch (error) {
            console.warn(error);

            // Sends an abort request to the terminal
            await sendAbortRequest(pizzaName);
            
            // Hides loading animation component and allow user to select tables again
            hideLoadingComponent();
        }
    });
    
    // Bind `cancel-operation-button`
    const cancelOperationButton = document.getElementById('cancel-operation-button');
    cancelOperationButton.addEventListener('click', async () => {
        // Abort sending post request
        abortController.abort(); 

        await sendAbortRequest();
        
        // Hide loading animation component
        hideLoadingComponent();
    });
    
    // Bind `loyalty-button`
    const loyaltyButton = document.getElementById('loyalty-button');
    loyaltyButton.addEventListener('click', async () => {
        try {
            // Show loading animation component and don't allow user to select any tables
            showLoadingComponent();

            // Send card acquisition check request
            var response = await sendPostRequest("/cash-register/apply-discount");
            console.log(response);
            
            // Hides loading animation component and allow user to select tables again
            hideLoadingComponent();

            if (response.result == "success")
            {
                // Hides loading animation component and allow user to select tables again
                hideLoadingComponent();
                window.location.href = "/cashregister";
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
    
    // Allows user to select a table by binding all tables to a click event
    const tables = document.querySelectorAll('.pizzas-grid-item');
    tables.forEach(table => {
        table.addEventListener('click', function() {
            // Remove the 'current-selection' class from all `table-grid-items`
            tables.forEach(item => item.classList.remove('current-selection'));

            // Add the 'current-selection' class to the currently selected `pizzas-grid-item`
            table.classList.add('current-selection');

            // Copies 'pizza name' value to the `card-acquisition-request-form`
            const pizzaNameElement = document.getElementById('pizzaName');
            pizzaNameElement.value = table.querySelector('.pizzas-grid-item-title').textContent;
        });
    });
}

bindButtons();