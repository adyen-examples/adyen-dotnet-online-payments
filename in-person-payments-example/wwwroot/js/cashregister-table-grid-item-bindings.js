// Sends POST request to url
async function sendPostRequest(url, data) {
    const res = await fetch(url, {
        method: "POST",
        body: data ? JSON.stringify(data) : "",
        headers: {
            "Content-Type": "application/json",
            "Connection": "keep-alive",
            "Keep-Alive": "timeout=150, max=180"
        },
    });

    return await res.json();
}

// Send payment request
async function sendPaymentRequest(amount, currency) {
    try {
        const response = await sendPostRequest("/api/create-payment", { amount: amount, currency: currency });
        console.log(response);
        switch (response.result) {
            case "success":
                window.location.href = "result/success";
                break;
            case "failure":
                window.location.href = "result/failure/" + response.refusalReason;
                break;
            default:
                break;
        };
    } catch (error) {
        console.error(error);
        alert("Error occurred. Look at console for details");
    }
}

// Send payment reversal request // TODO
async function sendPaymentReversalRequest(amount, saleReferenceId) {
    try {
        const response = await sendPostRequest("/api/create-payment-reversal", { amount: amount, saleReferenceId: saleReferenceId });
        console.log(response);
        switch (response.result) {
            case "success":
                window.location.href = "result/success";
                break;
            case "failure":
                window.location.href = "result/failure/" + response.refusalReason;
                break;
            default:
                break;
        };
    } catch (error) {
        console.error(error);
        alert("Error occurred. Look at console for details");
    }
}

// Bind table selection buttons and the `send payment request` submit-button
function bindButtons() {
    const tables = document.querySelectorAll('.tables-grid-item');

    tables.forEach(table => {
        table.addEventListener('click', () => {
            // Remove the 'active' class from all table-grid-items
            tables.forEach(item => item.classList.remove('active'));

            // Add the 'active' class to tables-grid-item
            table.classList.add('active');

            // Copies 'amount' value to the form
            const amount = document.getElementById('amount');
            amount.value = table.querySelector('.tables-grid-item-amount').textContent;

            // Copies 'currency' value to the form
            const currency = document.getElementById('currency');
            currency.value = table.querySelector('.tables-grid-item-currency').textContent;

            // Copies 'table name' value to display 
            const currentSelection = document.getElementById('current-selection');
            currentSelection.textContent = table.querySelector('.tables-grid-item-title').textContent;
        });
    });

    // Bind `payment-request-form` submit-button.
    var element = document.getElementById('payment-request-form');
    element.addEventListener('submit', async function(event) {
        event.preventDefault();

        var formData = new FormData(event.target);
        var amount = formData.get('amount');
        var currency = formData.get('currency');

        await sendPaymentRequest(amount, currency);
    });

}

bindButtons();