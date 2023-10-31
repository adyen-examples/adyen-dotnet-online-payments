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
async function sendAbortRequest(pizzaName) {
    try {
        var response = await sendGetRequest("/api/abort/" + pizzaName);
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

// Bind table selection buttons and the `pay/transaction-status` submit-buttons
function bindButtons() {
    // Bind `payment-request-form` submit-button
    const paymentRequestForm = document.getElementById('payment-request-form');
    paymentRequestForm.addEventListener('submit', async function(event) {
        event.preventDefault();

        var formData = new FormData(event.target);
        var amount = formData.get('amount');
        var pizzaName = formData.get('pizzaName');

        if (amount && pizzaName) { 
            try {
                // Show loading animation component which doesn't allow users to select any tables
                showLoadingComponent();           

                // Send payment request
                var response = await sendPostRequest("/api/create-payment", { pizzaName: pizzaName, amount: amount, currency: "EUR" });
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
                console.warn(error);

                // Sends an abort request to the terminal
                await sendAbortRequest(pizzaName);
                
                // Hides loading animation component and allow user to select tables again
                hideLoadingComponent();
            }
        }
    });

    // Bind `card-acquisition-request-form` submit-button
    const cardAcquisitionRequestForm = document.getElementById('card-acquisition-request-form');
    cardAcquisitionRequestForm.addEventListener('submit', async function(event) {
        event.preventDefault();

        var formData = new FormData(event.target);
        var cardAcquisitionAmount = formData.get('cardAcquisitionAmount');
        var cardAcquisitionPizzaName = formData.get('cardAcquisitionPizzaName');

        if (cardAcquisitionAmount && cardAcquisitionPizzaName) { 
            try {
                // Show loading animation component which doesn't allow users to select any tables
                showLoadingComponent();           

                // Send card acquisition payment request
                var response = await sendGetRequest("/card-acquisition/create/" + cardAcquisitionPizzaName + "/" + cardAcquisitionAmount);
                console.log(response);

                if (response.loyaltyPoints) // TODO handle success scenario
                {
                    // Hides loading animation component and allow user to select tables again
                    hideLoadingComponent();

                    
                    window.location.href = "result/success";
                }
                else
                {
                    window.location.href = "result/failure/" + "Transaction aborted.";
                }
            }
            catch (error) {
                console.warn(error);

                // Sends an abort request to the terminal
                await sendAbortRequest(pizzaName); // TODO, different abort!
                
                // Hides loading animation component and allow user to select tables again
                hideLoadingComponent();
            }
        }
    });
    
    // Bind `cancel-operation-button`
    const cancelOperationButton = document.getElementById('cancel-operation-button');
    cancelOperationButton.addEventListener('click', () => {
        // Abort sending post request
        abortController.abort(); 

        // Hide loading animation component
        hideLoadingComponent();
    });
    
    // Bind `transaction-status-button`
    const transactionStatusButton = document.getElementById('transaction-status-button');
    transactionStatusButton.addEventListener('click', async () => {
        try {
            // Show loading animation component and don't allow user to select any tables
            showLoadingComponent();

            // Send card acquisition check request
            var response = await sendPostRequest("/card-acquisition/check");
            console.log(response);
            
            
            // Hides loading animation component and allow user to select tables again
            hideLoadingComponent();

            if (response.loyaltyPoints)
            {
                // Show loyalty points
                document.getElementById('loyaltypoints-component').classList.remove('hidden');
                document.getElementById('loyaltypoints-value').textContent = response.loyaltyPoints;
            }

            window.location.href = "/cashregister";
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

            // Copies 'amount' value to the `payment-request-form`
            const amountElement = document.getElementById('amount');
            amountElement.value = table.querySelector('.pizzas-grid-item-amount').textContent;

            // Copies 'amount' value to the `card-acquisition-request-form`
            const cardAcquisitionAmount = document.getElementById('cardAcquisitionAmount');
            cardAcquisitionAmount.value = table.querySelector('.pizzas-grid-item-amount').textContent;

            // Copies 'pizza name' value to the `payment-request-form`
            const pizzaNameElement = document.getElementById('pizzaName');
            pizzaNameElement.value = table.querySelector('.pizzas-grid-item-title').textContent;

            // Copies 'table name' value to the `card-acquisition-request-form`
            const cardAcquisitionPizzaNameElement = document.getElementById('cardAcquisitionPizzaName');
            cardAcquisitionPizzaNameElement.value = table.querySelector('.pizzas-grid-item-title').textContent;

            // Show/hides the `payment-request-button` according to the `PaymentStatus` of currently selected table
            const currentActiveTable = document.getElementsByClassName('current-selection')[0];
            var statusValue = currentActiveTable.querySelector('.pizzas-grid-item-status').textContent;
            switch (statusValue) {
                 case 'NotPaid':
                    enablePaymentRequestButton();
                    disableTransactionStatusButton();
                    break;
                case 'Paid':
                    disablePaymentRequestButton();
                    enableTransactionStatusButton();
                    break;
                case 'PaymentInProgress':
                default:
                    disablePaymentRequestButton();
                    enableTransactionStatusButton();
                    break;
            }
        });
    });
}

// Enable `payment-request-button`
// Enable `card-acquisition-request-button`
function enablePaymentRequestButton() {
   document.getElementById('payment-request-button').classList.remove('disabled');
   document.getElementById('card-acquisition-request-button').classList.remove('disabled');
}

// Disable `payment-request-button`
// Disable `card-acquisition-request-button`
function disablePaymentRequestButton() {
   document.getElementById('payment-request-button').classList.add('disabled');
   document.getElementById('card-acquisition-request-button').classList.add('disabled');
}

// Enable `transaction-status-button`
function enableTransactionStatusButton() {
    document.getElementById('transaction-status-button').classList.remove('disabled');
}

// Disable `transaction-status-button`
function disableTransactionStatusButton() {
    document.getElementById('transaction-status-button').classList.add('disabled');
}

bindButtons();