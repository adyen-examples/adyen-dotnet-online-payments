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
        const res = await sendPostRequest("/api/create-payment", { amount: amount, currency: currency });
        console.log(res);
        switch (res.status) {
            case "received":
                window.location.href = "result/received/" + res.PoiData.SaleData.SaleTransactionID; // TODO
                break;
            default:
                window.location.href = "result/error";
                break;
        };
    } catch (error) {
        console.error(error);
        alert("Error occurred. Look at console for details");
    }
}

// Send payment reversal request
async function sendPaymentReversalRequest(amount, saleReferenceId) {
    try {
        const res = await sendPostRequest("/api/create-payment-reversal", { amount: amount, saleReferenceId: saleReferenceId });
        console.log(res);
        switch (res.status) {
            case "received":
                window.location.href = "result/received/" + saleReferenceId; // TODO
                break;
            default:
                window.location.href = "result/error" + saleReferenceId;
                break;
        };
    } catch (error) {
        console.error(error);
        alert("Error occurred. Look at console for details");
    }
}

// Binds submit buttons to `capture-payment`-endpoint
/*function bindCapturePaymentFormButtons() { 
    var elements = document.getElementsByName('capturePaymentForm');
    for (var i = 0; i < elements.length;  i++) {
        elements[i].addEventListener('submit', async function(event) {
            event.preventDefault();

            var formData = new FormData(event.target);
            var reference = formData.get('reference');

            await sendCapturePaymentRequest(reference);
        });
    }
}*/

function bindTableElements() {
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
            
            // Copies 'table name' value to the form
            const tableName = document.getElementById('currentActiveTable');
            tableName.textContent = table.querySelector('.tables-grid-item-title').textContent;
            
        });
    });
}

function updateButtons() {
    var elements = document.getElementsByName('sendPaymentRequestForm');
    for (var i = 0; i < elements.length;  i++) {
        elements[i].addEventListener('submit', async function(event) {
            event.preventDefault();

            var formData = new FormData(event.target);
            var amount = formData.get('amount');
            var currency = formData.get('currency');
            console.log(amount);
            await sendPaymentRequest(amount, currency);
        });
    }

    // TODO: bind reversals
    /*var elements = document.getElementsByName('sendPaymentReversalForm');
    for (var i = 0; i < elements.length; i++) {
        elements[i].addEventListener('submit', async function(event) {
            event.preventDefault();

            var formData = new FormData(event.target);
            var saleReferenceId = formData.get('saleReferenceId');
            var reversalAmount = formData.get('reversalAmount');

            await sendPaymentReversalRequest(reversalAmount, saleReferenceId);
        });
    }*/
}

bindTableElements();
updateButtons();