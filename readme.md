# Adyen Online Payment Integration Demos

This repository includes a collection of PCI-compliant UI integrations that show how to integrate with Adyen using different payment methods. 
The demos below leverages Adyen's API Library for .NET ([GitHub](https://github.com/Adyen/adyen-dotnet-api-library) | [Documentation](https://docs.adyen.com/development-resources/libraries#csharp)).


## [Checkout Example](checkout-example)
The [checkout example](checkout-example) repository includes examples of PCI-compliant UI integrations for online payments with Adyen.
Within this demo app, you'll find a simplified version of an e-commerce website, complete with commented code to highlight key features and concepts of Adyen's API.
Check out the underlying code to see how you can integrate Adyen to give your shoppers the option to pay with their preferred payment methods, all in a seamless checkout experience.

![Card Checkout Demo](checkout-example/wwwroot/images/cardcheckout.gif)

## [Subscription Example](subscription-example)
The [subscription example](subscription-example) repository includes a tokenization example for subscriptions. Within this demo app, you'll find a simplified version of a website that offers a music subscription service.
The shopper can purchase a subscription and administrators can manage the saved (tokenized) payment methods on a separate admin panel.
The panel allows admins to make payments on behalf of the shopper using this token.

![Subscription Demo](subscription-example/wwwroot/images/cardsubscription.gif)

## [Giftcard Example](giftcard-example)
The [giftcard example](giftcard-example) repository includes a giftcard flow during checkout. Within this demo app, you'll find a simplified version of an e-commerce website. The shopper can choose to use giftcards to complete their purchase or use their preferred payment method to pay the remaining amount.

![Giftcard Demo](subscription-example/wwwroot/images/cardgiftcard.gif)

## Contributing

We commit all our new features directly into our GitHub repository. Feel free to request or suggest new features or code changes yourself as well!

Find out more in our [contributing](https://github.com/adyen-examples/.github/blob/main/CONTRIBUTING.md) guidelines.


## License

MIT license. For more information, see the **LICENSE** file.
