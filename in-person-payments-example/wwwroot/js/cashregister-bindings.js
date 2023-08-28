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
            "Keep-Alive": "timeout=150, max=180"
        },
        signal: abortController.signal
    });

    return await res.json();
}

// Shows loading animation component and deactivates the table selection
function showLoadingComponent() {
    // Show loading animation component
    const loadingGrid = document.getElementById('loading-grid');
    loadingGrid.classList.remove('disabled');
    
    // Deactivate tables selection section
    const tablesSection = document.getElementById('tables-section');
    tablesSection.classList.add('disabled');
}

// Activates the table selection and hides loading animation component
function hideLoadingComponent() {
    // Hides loading animation component
    const loadingGrid = document.getElementById('loading-grid');
    loadingGrid.classList.add('disabled');
    
    // Show tables selection section
    const tablesSection = document.getElementById('tables-section');
    tablesSection.classList.remove('disabled');
}


// Bind table selection buttons and the `send payment request` submit-button
function bindButtons() {
    // Bind `payment-request-form` submit-button
    const paymentRequestForm = document.getElementById('payment-request-form');
    paymentRequestForm.addEventListener('submit', async function(event) {
        event.preventDefault();

        var formData = new FormData(event.target);
        var amount = formData.get('amount');
        var currency = formData.get('currency');

        if (amount != null && currency != null) { 
            try {
                // Show loading animation component and don't allow user to select any tables
                showLoadingComponent();           

                // Send payment request
                var response = await sendPostRequest("/api/create-payment", { amount: amount, currency: currency });
                console.log(response);

                // Handle response
                switch (response.result) {
                    case "success":
                        window.location.href = "result/success";
                        break;
                    case "failure":
                        window.location.href = "result/failure/" + response.refusalReason;
                        break;
                    default:
                        throw Error('Unknown response result');
                }
            }
            catch (error) {
                console.error(error);

                // Hides loading animation component and allow user to select tables again
                hideLoadingComponent();
            }
        }
    });

    // Bind `reversal-request-form` submit-button
    const reversalForm = document.getElementById('reversal-request-form');
    reversalForm.addEventListener('submit', async function(event) {
        event.preventDefault();

        var formData = new FormData(event.target);
        var poiTransactionId = formData.get('poiTransactionId');
        var saleReferenceId = formData.get('saleReferenceId');
        
        if (poiTransactionId != null && saleReferenceId != null) {
            try {
                // Show loading animation component and don't allow user to select any tables
                showLoadingComponent();

                // Send reversal request
                var response = await sendPostRequest("/api/create-reversal", { poiTransactionId: poiTransactionId, saleReferenceId: saleReferenceId });
                console.log(response);

                // Handle response
                switch (response.result) {
                    case "success":
                        window.location.href = "result/success";
                        break;
                    case "failure":
                        window.location.href = "result/failure/" + response.refusalReason;
                        break;
                    default:
                        throw Error('Unknown response result');
                }
            }
            catch (error) {
                console.error(error);

                // Hides loading animation component and allow user to select tables again
                hideLoadingComponent();
            }
        }
    });
    
    // Bind `cancel-operation-button`
    const cancelOperationButton = document.getElementById('cancel-operation-button');
    cancelOperationButton.addEventListener('click', () => {
        abortController.abort(); // Abort sending post request
        hideLoadingComponent(); // Hide loading component
    });

    // Allows user to select a table by binding all tables to a click event
    const tables = document.querySelectorAll('.tables-grid-item');
    tables.forEach(table => {
        table.addEventListener('click', function() {
            // Remove the 'active' class from all table-grid-items
            tables.forEach(item => item.classList.remove('active'));

            // Add the 'active' class to tables-grid-item
            table.classList.add('active');

            // Copies 'amount' value to the form
            const amountElement = document.getElementById('amount');
            amountElement.value = table.querySelector('.tables-grid-item-amount').textContent;

            // Copies 'currency' value to the form
            const currencyElement = document.getElementById('currency');
            currencyElement.value = table.querySelector('.tables-grid-item-currency').textContent;

            // Copies 'table name' value to display 
            const currentSelectionElement = document.getElementById('current-selection');
            currentSelectionElement.textContent = table.querySelector('.tables-grid-item-title').textContent;

        });
    });
}

bindButtons();