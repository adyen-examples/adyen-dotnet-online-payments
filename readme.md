# Adyen [online payment](https://docs.adyen.com/checkout) integration demos

## Details

This repository includes a collection of examples:

- Checkout examples - Use different payment methods to make a payment request.
- Subscription example - Tokenize and initiate a payment request using the token.

## Requirements

- .NET Core SDK 6.x
- A set of [API keys](https://docs.adyen.com/user-management/how-to-get-the-api-key).


## Run this integration in seconds using [Gitpod](https://gitpod.io/)

* Open your [Adyen Test Account](https://ca-test.adyen.com/ca/ca/overview/default.shtml) and create a set of [API keys](https://docs.adyen.com/user-management/how-to-get-the-api-key).
* Go to [gitpod account variables](https://gitpod.io/variables).
* Set the `ADYEN_API_KEY`, `ADYEN_CLIENT_KEY`, `ADYEN_HMAC_KEY` and `ADYEN_MERCHANT_ACCOUNT variables`.
* Click the button below!

[![Open in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/adyen-examples/adyen-dotnet-online-payments/)

* Once the environment is ready, navigate to the Terminal and navigate to the respective folder `cd 'src/checkout-example'` or `cd 'src/subscription-example'`
* To run the application type: `dotnet run`

_NOTE: To allow the Adyen Drop-In and Components to load, you have to add `https://*.gitpod.io` as allowed origin for your chosen set of [API Credentials](https://ca-test.adyen.com/ca/ca/config/api_credentials_new.shtml)_


## Contributing

We commit all our new features directly into our GitHub repository. Feel free to request or suggest new features or code changes yourself as well!

Find out more in our [Contributing](https://github.com/adyen-examples/.github/blob/main/CONTRIBUTING.md) guidelines.

## License

MIT license. For more information, see the **LICENSE** file in the root directory.
