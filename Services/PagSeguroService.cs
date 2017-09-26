﻿using Grand.Core;
using Grand.Core.Domain.Directory;
using Grand.Core.Domain.Orders;
using Grand.Core.Domain.Payments;
using Grand.Services.Catalog;
using Grand.Services.Configuration;
using Grand.Services.Customers;
using Grand.Services.Directory;
using Grand.Services.Orders;
using Grand.Services.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using Uol.PagSeguro;

namespace NopBrasil.Plugin.Payments.PagSeguro.Services
{
    public class PagSeguroService : IPagSeguroService
    {
        //todo: colocar a moeda utilizada como configuração
        private const string CURRENCY_CODE = "BRL";

        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly PagSeguroPaymentSetting _pagSeguroPaymentSetting;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IStoreContext _storeContext;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;

        public PagSeguroService(ISettingService settingService, ICurrencyService currencyService, CurrencySettings currencySettings, PagSeguroPaymentSetting pagSeguroPaymentSetting, IOrderService orderService, 
            IOrderProcessingService orderProcessingService, IStoreContext storeContext, ICustomerService customerService, IProductService productService, ICountryService countryService, IStateProvinceService stateProvinceService)
        {
            this._settingService = settingService;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._pagSeguroPaymentSetting = pagSeguroPaymentSetting;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._storeContext = storeContext;
            this._customerService = customerService;
            this._productService = productService;
            this._countryService = countryService;
            this._stateProvinceService = stateProvinceService;
        }

        public Uri CreatePayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            // Seta as credenciais
            AccountCredentials credentials = new AccountCredentials(@_pagSeguroPaymentSetting.PagSeguroEmail, @_pagSeguroPaymentSetting.PagSeguroToken);

            PaymentRequest payment = new PaymentRequest();
            payment.Currency = CURRENCY_CODE;
            payment.Reference = postProcessPaymentRequest.Order.Id.ToString();

            LoadingItems(postProcessPaymentRequest, payment);
            LoadingShipping(postProcessPaymentRequest, payment);
            LoadingSender(postProcessPaymentRequest, payment);

            return Uol.PagSeguro.PaymentService.Register(credentials, payment);
        }

        private void LoadingSender(PostProcessPaymentRequest postProcessPaymentRequest, PaymentRequest payment)
        {
            payment.Sender = new Sender();
            payment.Sender.Email = _customerService.GetCustomerById(postProcessPaymentRequest.Order.CustomerId)?.Email;
            payment.Sender.Name = $"{postProcessPaymentRequest.Order.BillingAddress.FirstName} {postProcessPaymentRequest.Order.BillingAddress.LastName}";
        }

        private decimal GetConvertedRate(decimal rate)
        {
            var usedCurrency = _currencyService.GetCurrencyByCode(CURRENCY_CODE);
            if (usedCurrency == null)
                throw new GrandException($"PagSeguro payment service. Could not load \"{CURRENCY_CODE}\" currency");

            if (usedCurrency.CurrencyCode == _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode)
                return rate;
            else
                return _currencyService.ConvertFromPrimaryStoreCurrency(rate, usedCurrency);
        }

        private void LoadingShipping(PostProcessPaymentRequest postProcessPaymentRequest, PaymentRequest payment)
        {
            payment.Shipping = new Shipping();
            payment.Shipping.ShippingType = ShippingType.NotSpecified;
            Address adress = new Address();
            adress.Complement = string.Empty;
            adress.District = string.Empty;
            adress.Number = string.Empty;
            if (postProcessPaymentRequest.Order.ShippingAddress != null)
            {
                adress.City = postProcessPaymentRequest.Order.ShippingAddress.City;
                adress.Country = _countryService.GetCountryById(postProcessPaymentRequest.Order.ShippingAddress.CountryId)?.Name;
                adress.PostalCode = postProcessPaymentRequest.Order.ShippingAddress.ZipPostalCode;
                adress.State = _stateProvinceService.GetStateProvinceById(postProcessPaymentRequest.Order.ShippingAddress.StateProvinceId)?.Name;
                adress.Street = postProcessPaymentRequest.Order.ShippingAddress.Address1;
            }
            payment.Shipping.Cost = Math.Round(GetConvertedRate(postProcessPaymentRequest.Order.OrderShippingInclTax), 2);
        }

        private void LoadingItems(PostProcessPaymentRequest postProcessPaymentRequest, PaymentRequest payment)
        {
            foreach (var product in postProcessPaymentRequest.Order.OrderItems)
            {
                Item item = new Item();
                item.Amount = Math.Round(GetConvertedRate(product.UnitPriceInclTax), 2);
                item.Description = _productService.GetProductById(product.ProductId)?.Name;
                item.Id = product.Id.ToString();
                item.Quantity = product.Quantity;
                if (product.ItemWeight.HasValue)
                    item.Weight = Convert.ToInt64(product.ItemWeight);
                payment.Items.Add(item);
            }
        }

        private IEnumerable<Order> GetPendingOrders() => _orderService.SearchOrders(_storeContext.CurrentStore.Id, paymentMethodSystemName: "Payments.PagSeguro", ps: PaymentStatus.Pending).Where(o => _orderProcessingService.CanMarkOrderAsPaid(o));

        private TransactionSummary GetTransaction(AccountCredentials credentials, string referenceCode) => TransactionSearchService.SearchByReference(credentials, referenceCode)?.Items?.FirstOrDefault();

        private bool TransactionIsPaid(TransactionSummary transaction) => (transaction?.TransactionStatus == TransactionStatus.Paid || transaction?.TransactionStatus == TransactionStatus.Available);

        public void CheckPayments()
        {
            AccountCredentials credentials = new AccountCredentials(@_pagSeguroPaymentSetting.PagSeguroEmail, @_pagSeguroPaymentSetting.PagSeguroToken);
            foreach (var order in GetPendingOrders())
                if (TransactionIsPaid(GetTransaction(credentials, order.Id.ToString())))
                    _orderProcessingService.MarkOrderAsPaid(order);
        }
    }
}