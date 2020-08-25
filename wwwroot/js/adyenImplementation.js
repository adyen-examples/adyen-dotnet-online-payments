const clientKey = document.getElementById("clientKey").innerHTML;
const type = document.getElementById("type").innerHTML;

function filterUnimplemented(pm) {
  pm.paymentMethods = pm.paymentMethods.filter((it) =>
    [
      "ach",
      "scheme",
      "dotpay",
      "giropay",
      "ideal",
      "directEbanking",
      "klarna_paynow",
      "klarna",
      "klarna_account",
    ].includes(it.type)
  );
  return pm;
}

callServer("/api/getPaymentMethods", { merchantAccount: "" })
  .then((paymentMethodsResponse) => {
    const configuration = {
      paymentMethodsResponse: filterUnimplemented(paymentMethodsResponse),
      clientKey,
      locale: "en_US",
      environment: "test",
      showPayButton: true,
      paymentMethodsConfiguration: {
        ideal: {
          showImage: true,
        },
        card: {
          hasHolderName: true,
          holderNameRequired: true,
          name: "Credit or debit card",
          amount: {
            value: 1000,
            currency: "EUR",
          },
        },
      },
      onSubmit: (state, component) => {
        handleSubmission(state, component, "/api/initiatePayment");
      },
      onAdditionalDetails: (state, component) => {
        handleSubmission(state, component, "/api/submitAdditionalDetails");
      },
    };

    const checkout = new AdyenCheckout(configuration);

    checkout.create(type).mount(document.getElementById(type));
  })
  .catch((error) => {
    throw Error(error);
  });

// Calls your server endpoints
function callServer(url, data) {
  return fetch(url, {
    method: "POST",
    body: JSON.stringify(data),
    headers: {
      "Content-Type": "application/json",
    },
  }).then((res) => res.json());
}

// Handles responses sent from your server to the client
function handleServerResponse(res, component) {
  if (res.action) {
    component.handleAction(res.action);
  } else {
    switch (res.resultCode) {
      case "Authorised":
        window.location.href = "/Home/Result/success";
        break;
      case "Pending":
        window.location.href = "/Home/Result/pending";
        break;
      case "Refused":
        window.location.href = "/Home/Result/failed";
        break;
      default:
        window.location.href = `/Home/Result/error?reason=${res.resultCode}`;
        break;
    }
  }
}

// Event handlers called when the shopper selects the pay button,
// or when additional information is required to complete the payment
function handleSubmission(state, component, url) {
  if (state.isValid) {
    callServer(url, state.data)
      .then((res) => handleServerResponse(res, component))
      .catch((error) => {
        throw Error(error);
      });
  }
}
