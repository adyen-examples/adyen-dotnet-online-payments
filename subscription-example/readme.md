# Adyen [online payment](https://docs.adyen.com/checkout) integration demos

## Details

This repository includes an subscription example. Within this demo app, you'll find a simplified version of a website that offers a subscription service. You'll learn about tokenization and making recurring payments using this token.

## Requirements

- .NET Core SDK 6.x
- A set of [API keys](https://docs.adyen.com/user-management/how-to-get-the-api-key).


## Run this integration in seconds using [Gitpod](https://gitpod.io/)

* Open your [Adyen Test Account](https://ca-test.adyen.com/ca/ca/overview/default.shtml) and create a set of [API keys](https://docs.adyen.com/user-management/how-to-get-the-api-key).
* Go to [gitpod account variables](https://gitpod.io/variables).
* Set the `ADYEN_API_KEY`, `ADYEN_CLIENT_KEY`, `ADYEN_HMAC_KEY` and `ADYEN_MERCHANT_ACCOUNT variables`.
* Click the button below!

[![Open in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/adyen-examples/adyen-dotnet-online-payments/)

* Once the environment is ready, navigate to the Terminal and navigate to the respective folder `cd subscription-example`
* To run the application, use the command: `dotnet run`

_NOTE: To allow the Adyen Drop-In and Components to load, you have to add `https://*.gitpod.io` as allowed origin for your chosen set of [API Credentials](https://ca-test.adyen.com/ca/ca/config/api_credentials_new.shtml)_


## Installation

1. Clone this repo:

```
git clone https://github.com/adyen-examples/adyen-dotnet-online-payments.git
```

2. Set the below environment variables in your terminal environment ([API key](https://docs.adyen.com/user-management/how-to-get-the-api-key), [Client Key](https://docs.adyen.com/user-management/client-side-authentication), [Merchant Account](https://docs.adyen.com/account/account-structure), [HMAC Key](https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures) - Remember to add `https://localhost:5001` as an origin for client key, and merchant account name, all credentials are in string format)

```shell
export ADYEN_API_KEY=yourAdyenApiKey
export ADYEN_MERCHANT_ACCOUNT=yourAdyenMerchantAccount
export ADYEN_CLIENT_KEY=yourAdyenClientKey
export ADYEN_HMAC_KEY=yourAdyenHmacKey
```

On Windows CMD you can use below commands instead

```shell
set ADYEN_API_KEY=yourAdyenApiKey
set ADYEN_MERCHANT_ACCOUNT=yourAdyenMerchantAccount
set ADYEN_CLIENT_KEY=yourAdyenClientKey
set ADYEN_HMAC_KEY=yourAdyenHmacKey
```

## Usage

1. Start the application by navigating to the respective example:

```
dotnet run
```

2. Visit [https://localhost:5001/](https://localhost:5001/) to initiate a subscription.

To try out subscriptions with test card numbers and payment method details, see [Test card numbers](https://docs.adyen.com/development-resources/test-cards/test-card-numbers).

3. Make sure your webhook is reachable (see instructions below)!
**[!]** Make sure to **enable** `Recurring Details` (under the Payments section) in your ca-environment. If this is not enabled, you will **not** receive the recurringDetailReference from the webhook!

4. To test the application, enter one or multiple card details. Once the payment is authorized, you will receive a webhook notification with the recurringDetailReference (per payment method)!

5. These references are shown under 'Manage Tokens' which you can use to make a payment request (or choose to disable them).

_Note: We current store these values in a local memory cache, if you restart the application these values are lost!_
_Note 2: If you want to take a look at previously stored payment methods, you can directly call `managetokens/listRecurringDetails/` to see previously issued tokens that Adyen has saved for you.

## Testing webhooks

This demo provides simple webhook integration at `/api/webhooks/notifications`. For it to work, you need to:

* Provide a way for Adyen's servers to reach your running application
* Add a standard webhook in your customer area

### Making your server reachable

One possibility is to use a service like [ngrok](ngrok) (which can be used for free).

```bash
$ ngrok http https://localhost:5001 -host-header="localhost:5001"
```

Once you have  set up ngrok, make sure to add the provided ngrok URL to the list of Allowed Origins in the �API Credentials" part of your Customer Area.

### Setting up a webhook

* In the developers -> webhooks part of the customer area, create a new 'standard notifications' webhook.
* Make sure to check 'Accept self-signed', 'Accept non-trusted root certificates' (test only) and Active.
* In additional settings, add the data you want to receive. A good example is 'Payment Account Reference' and the mandatory `Recurring Details` for subscriptions.

That's it! Every time you test a new payment method, your server will receive a notification from Adyen's server.

You can find more information about webhooks in [this detailed blog post](https://www.adyen.com/blog/Integrating-webhooks-notifications-with-Adyen-Checkout).