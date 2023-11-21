# Adyen [In-person Payment Loyalty](https://docs.adyen.com/point-of-sale/) Integration Demo

## Run demo in one-click
[![Open in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/adyen-examples/adyen-java-spring-online-payments/tree/main/in-person-payments-loyalty-example)

[First time with Gitpod?](https://github.com/adyen-examples/.github/blob/main/pages/gitpod-get-started.md)

## Description
This demo shows developers how to implement a loyalty flow using the [Adyen Cloud Terminal API](https://docs.adyen.com/point-of-sale/design-your-integration/choose-your-architecture/cloud/). 
This is done using [Card Acquisition](https://docs.adyen.com/point-of-sale/card-acquisition/), and enables you to sign-up a shopper to a loyalty program or check cardholder details before applying discounts.
We describe two flows:
1. One where the shopper wants to buy a pizza and wants to sign-up to your loyalty program - This is known as a [card acquisition request followed by a payment](https://docs.adyen.com/point-of-sale/card-acquisition/#continue-with-payment).
2. One where the shopper only wants to sign-up to your loyalty program - This is known as a [card acquisition request followed by a cancellation](https://docs.adyen.com/point-of-sale/card-acquisition/#cancel-completed).

There are typically two ways to integrate in-person payments: local or cloud communications.
To find out which solution (or hybrid) suits your needs, visit the following [documentation page](https://docs.adyen.com/point-of-sale/design-your-integration/choose-your-architecture/#choosing-between-cloud-and-local).


This demo leverages Adyen's API Library for .NET ([GitHub](https://github.com/Adyen/adyen-dotnet-api-library) | [Docs](https://docs.adyen.com/development-resources/libraries?tab=c__5#csharp)). You can find the [Terminal API documentation](https://docs.adyen.com/point-of-sale/design-your-integration/terminal-api/terminal-api-reference/) here.


![In-person Payments Loyalty Demo Card](wwwroot/images/cardacquisitioncard.gif)

## Requirements
- A [terminal device](https://docs.adyen.com/point-of-sale/user-manuals/) and a [test card](https://docs.adyen.com/point-of-sale/testing-pos-payments/) from Adyen
- An Adyen account, learn how an Adyen account is structured in [our documentation](https://docs.adyen.com/point-of-sale/design-your-integration/determine-account-structure/)
- .NET Core SDK 6.0+


## 1. Installation
```
git clone https://github.com/adyen-examples/adyen-dotnet-online-payments.git
```


## 2. Set the environment variables 

You will need .NET Core SDK 6.x. to run this application locally.
* [API key](https://docs.adyen.com/user-management/how-to-get-the-api-key)
* [HMAC Key](https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures)
* `ADYEN_POS_POI_ID`: the unique ID of your payment terminal for the NEXO Sale to POI protocol.
   - **Format:** `[device model]-[serial number]` **Example:** `V400m-123456789`

On Linux/Mac/Windows export/set the environment variables.
```shell
export ADYEN_API_KEY=yourAdyenApiKey
export ADYEN_HMAC_KEY=yourHmacKey
export ADYEN_POS_POI_ID=v400m-123456789
```

Alternatively, it's possible to define the variables in the `appsettings.Development.json`.
```json
{
  "ADYEN_API_KEY": "yourAdyenApiKey",
  "ADYEN_HMAC_KEY": "yourHmacKey",
  "ADYEN_POS_POI_ID": "v400m-123456789"
}
```

## 3. Run the application

```shell
dotnet run --project in-person-payments-loyalty-example 
```

## Usage

1. Buy a pizza by clicking 'Pay', which sends a card acquisition request
2. Present your card on the terminal
3. Terminal asks the shopper whether they want to sign-up for a loyalty program (collect +100 points on every purchase, and get a discount after 200 points).
4. The terminal then asks the shopper to either:
   - Finish the the request with a payment: shopper pays and is signed up for the loyalty program if they consent.
   - Finish the request with a cancellation request: shopper only signs up for the loyalty program (without making a payment).
5. When a shopper returns, they can click the 'Apply discount'-button to get a discount if they have over 200 points.

# Webhooks

Webhooks deliver asynchronous notifications about the payment status and other events that are important to receive and process.
You can find more information about webhooks in [this blog post](https://www.adyen.com/knowledge-hub/consuming-webhooks).

### Webhook setup

In the Customer Area under the `Developers → Webhooks` section, [create](https://docs.adyen.com/development-resources/webhooks/#set-up-webhooks-in-your-customer-area) a new `Standard webhook`.

A good practice is to set up basic authentication, copy the generated HMAC Key and set it as an environment variable. The application will use this to verify the [HMAC signatures](https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures/).

Make sure the webhook is **enabled**, so it can receive notifications.

### Expose an endpoint

This demo provides a simple webhook implementation exposed at `/api/webhooks/notifications` that shows you how to receive, validate and consume the webhook payload.

### Test your webhook

The following webhooks `events` should be enabled:
* **AUTHORISATION**

To make sure that the Adyen platform can reach your application, we have written a [Webhooks Testing Guide](https://github.com/adyen-examples/.github/blob/main/pages/webhooks-testing.md)
that explores several options on how you can easily achieve this (e.g. running on localhost or cloud).

