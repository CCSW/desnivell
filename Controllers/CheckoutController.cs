﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Web.Extensions;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Security;
using Nop.Web.Models.Checkout;
using Nop.Web.Models.Common;

namespace Nop.Web.Controllers
{
    [NopHttpsRequirement(SslRequirement.Yes)]
    public class CheckoutController : BaseNopController
    {
		#region Fields

        private readonly IWorkContext _workContext;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ILocalizationService _localizationService;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ICustomerService _customerService;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IShippingService _shippingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly IWebHelper _webHelper;
        private readonly HttpContextBase _httpContext;
        private readonly IMobileDeviceHelper _mobileDeviceHelper;
        

        private readonly OrderSettings _orderSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly PaymentSettings _paymentSettings;

        #endregion

		#region Constructors

        public CheckoutController(IWorkContext workContext,
            IShoppingCartService shoppingCartService, ILocalizationService localizationService, 
            ITaxService taxService, ICurrencyService currencyService, 
            IPriceFormatter priceFormatter, IOrderProcessingService orderProcessingService,
            ICustomerService customerService,  ICountryService countryService,
            IStateProvinceService stateProvinceService, IShippingService shippingService, 
            IPaymentService paymentService, IOrderTotalCalculationService orderTotalCalculationService,
            ILogger logger, IOrderService orderService, IWebHelper webHelper,
            HttpContextBase httpContext, IMobileDeviceHelper mobileDeviceHelper,
            OrderSettings orderSettings, RewardPointsSettings rewardPointsSettings,
            PaymentSettings paymentSettings)
        {
            this._workContext = workContext;
            this._shoppingCartService = shoppingCartService;
            this._localizationService = localizationService;
            this._taxService = taxService;
            this._currencyService = currencyService;
            this._priceFormatter = priceFormatter;
            this._orderProcessingService = orderProcessingService;
            this._customerService = customerService;
            this._countryService = countryService;
            this._stateProvinceService = stateProvinceService;
            this._shippingService = shippingService;
            this._paymentService = paymentService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._logger = logger;
            this._orderService = orderService;
            this._webHelper = webHelper;
            this._httpContext = httpContext;
            this._mobileDeviceHelper = mobileDeviceHelper;

            this._orderSettings = orderSettings;
            this._rewardPointsSettings = rewardPointsSettings;
            this._paymentSettings = paymentSettings;
        }

        #endregion

        #region Utilities

        [NonAction]
        protected bool IsPaymentWorkflowRequired(IList<ShoppingCartItem> cart, bool ignoreRewardPoints = false)
        {
            bool result = true;

            //check whether order total equals zero
            decimal? shoppingCartTotalBase = _orderTotalCalculationService.GetShoppingCartTotal(cart, ignoreRewardPoints);
            if (shoppingCartTotalBase.HasValue && shoppingCartTotalBase.Value == decimal.Zero)
                result = false;
            return result;
        }

        [NonAction]
        protected CheckoutBillingAddressModel PrepareBillingAddressModel(int? selectedCountryId = null)
        {
            var model = new CheckoutBillingAddressModel();
            //existing addresses
            var addresses = _workContext.CurrentCustomer.Addresses.Where(a => a.Country == null || a.Country.AllowsBilling).ToList();
            foreach (var address in addresses)
                model.ExistingAddresses.Add(address.ToModel());

            //new address model
            model.NewAddress = new AddressModel();
            //countries
            model.NewAddress.AvailableCountries.Add(new SelectListItem() { Text = _localizationService.GetResource("Address.SelectCountry"), Value = "0" });
            foreach (var c in _countryService.GetAllCountriesForBilling())
                model.NewAddress.AvailableCountries.Add(new SelectListItem() { Text = c.GetLocalized(x => x.Name), Value = c.Id.ToString(), Selected = (selectedCountryId.HasValue && c.Id == selectedCountryId.Value) });
            //states
            var states = selectedCountryId.HasValue ? _stateProvinceService.GetStateProvincesByCountryId(selectedCountryId.Value).ToList() : new List<StateProvince>();
            if (states.Count > 0)
            {
                foreach (var s in states)
                    model.NewAddress.AvailableStates.Add(new SelectListItem() { Text = s.GetLocalized(x => x.Name), Value = s.Id.ToString() });
            }
            else
                model.NewAddress.AvailableStates.Add(new SelectListItem() { Text = _localizationService.GetResource("Address.OtherNonUS"), Value = "0" });

            return model;
        }

        [NonAction]
        protected CheckoutShippingAddressModel PrepareShippingAddressModel(int? selectedCountryId = null)
        {
            var model = new CheckoutShippingAddressModel();
            //existing addresses
            var addresses = _workContext.CurrentCustomer.Addresses.Where(a => a.Country == null || a.Country.AllowsShipping).ToList();
            foreach (var address in addresses)
                model.ExistingAddresses.Add(address.ToModel());

            //new address model
            model.NewAddress = new AddressModel();
            //countries
            model.NewAddress.AvailableCountries.Add(new SelectListItem() { Text = _localizationService.GetResource("Address.SelectCountry"), Value = "0" });
            foreach (var c in _countryService.GetAllCountriesForShipping())
                model.NewAddress.AvailableCountries.Add(new SelectListItem() { Text = c.GetLocalized(x => x.Name), Value = c.Id.ToString(), Selected = (selectedCountryId.HasValue && c.Id == selectedCountryId.Value) });
            //states
            var states = selectedCountryId.HasValue ? _stateProvinceService.GetStateProvincesByCountryId(selectedCountryId.Value).ToList() : new List<StateProvince>();
            if (states.Count > 0)
            {
                foreach (var s in states)
                    model.NewAddress.AvailableStates.Add(new SelectListItem() { Text = s.GetLocalized(x => x.Name), Value = s.Id.ToString() });
            }
            else
                model.NewAddress.AvailableStates.Add(new SelectListItem() { Text = _localizationService.GetResource("Address.OtherNonUS"), Value = "0" });

            return model;
        }

        [NonAction]
        protected CheckoutShippingMethodModel PrepareShippingMethodModel(IList<ShoppingCartItem> cart)
        {
            var model = new CheckoutShippingMethodModel();

            var getShippingOptionResponse = _shippingService.GetShippingOptions(cart, _workContext.CurrentCustomer.ShippingAddress);
            if (getShippingOptionResponse.Success)
            {
                foreach (var shippingOption in getShippingOptionResponse.ShippingOptions)
                {
                    var soModel = new CheckoutShippingMethodModel.ShippingMethodModel()
                    {
                        Name = shippingOption.Name,
                        Description = shippingOption.Description,
                        ShippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName,
                    };

                    //adjust rate
                    Discount appliedDiscount = null;
                    var shippingTotal = _orderTotalCalculationService.AdjustShippingRate(
                        shippingOption.Rate, cart, out appliedDiscount);

                    decimal rateBase = _taxService.GetShippingPrice(shippingTotal, _workContext.CurrentCustomer);
                    decimal rate = _currencyService.ConvertFromPrimaryStoreCurrency(rateBase, _workContext.WorkingCurrency);
                    soModel.Fee = _priceFormatter.FormatShippingPrice(rate, true);

                    model.ShippingMethods.Add(soModel);
                }
            }
            else
                foreach (var error in getShippingOptionResponse.Errors)
                    model.Warnings.Add(error);

            return model;
        }

        [NonAction]
        protected CheckoutPaymentMethodModel PreparePaymentMethodModel(IList<ShoppingCartItem> cart)
        {
            var model = new CheckoutPaymentMethodModel();

            //reward points
            if (_rewardPointsSettings.Enabled && !cart.IsRecurring())
            {
                int rewardPointsBalance = _workContext.CurrentCustomer.GetRewardPointsBalance();
                decimal rewardPointsAmountBase = _orderTotalCalculationService.ConvertRewardPointsToAmount(rewardPointsBalance);
                decimal rewardPointsAmount = _currencyService.ConvertFromPrimaryStoreCurrency(rewardPointsAmountBase, _workContext.WorkingCurrency);
                if (rewardPointsAmount > decimal.Zero)
                {
                    model.DisplayRewardPoints = true;
                    model.RewardPointsAmount = _priceFormatter.FormatPrice(rewardPointsAmount, true, false);
                    model.RewardPointsBalance = rewardPointsBalance;
                }
            }

            var boundPaymentMethods = _paymentService
                .LoadActivePaymentMethods(_workContext.CurrentCustomer.Id)
                .Where(pm => pm.PaymentMethodType == PaymentMethodType.Standard || pm.PaymentMethodType == PaymentMethodType.Redirection)
                .ToList();
            foreach (var pm in boundPaymentMethods)
            {
                if (cart.IsRecurring() && pm.RecurringPaymentType == RecurringPaymentType.NotSupported)
                    continue;

                var pmModel = new CheckoutPaymentMethodModel.PaymentMethodModel()
                {
                    Name = pm.GetLocalizedFriendlyName(_localizationService, _workContext.WorkingLanguage.Id),
                    PaymentMethodSystemName = pm.PluginDescriptor.SystemName,
                };
                //payment method additional fee
                decimal paymentMethodAdditionalFee = _paymentService.GetAdditionalHandlingFee(pm.PluginDescriptor.SystemName);
                decimal rateBase = _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, _workContext.CurrentCustomer);
                decimal rate = _currencyService.ConvertFromPrimaryStoreCurrency(rateBase, _workContext.WorkingCurrency);
                if (rate > decimal.Zero)
                    pmModel.Fee = _priceFormatter.FormatPaymentMethodAdditionalFee(rate, true);

                model.PaymentMethods.Add(pmModel);
            }

            return model;
        }

        [NonAction]
        protected CheckoutPaymentInfoModel PreparePaymentInfoModel(IPaymentMethod paymentMethod)
        {
            var model = new CheckoutPaymentInfoModel();
            string actionName;
            string controllerName;
            RouteValueDictionary routeValues;
            paymentMethod.GetPaymentInfoRoute(out actionName, out controllerName, out routeValues);
            model.PaymentInfoActionName = actionName;
            model.PaymentInfoControllerName = controllerName;
            model.PaymentInfoRouteValues = routeValues;
            model.DisplayOrderTotals = _orderSettings.OnePageCheckoutDisplayOrderTotalsOnPaymentInfoTab;
            return model;
        }

        [NonAction]
        protected CheckoutConfirmModel PrepareConfirmOrderModel(IList<ShoppingCartItem> cart)
        {
            var model = new CheckoutConfirmModel();
            //min order amount validation
            bool minOrderTotalAmountOk = _orderProcessingService.ValidateMinOrderTotalAmount(cart);
            if (!minOrderTotalAmountOk)
            {
                decimal minOrderTotalAmount = _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderTotalAmount, _workContext.WorkingCurrency);
                model.MinOrderTotalWarning = string.Format(_localizationService.GetResource("Checkout.MinOrderTotalAmount"), _priceFormatter.FormatPrice(minOrderTotalAmount, true, false));
            }
            return model;
        }

        [NonAction]
        protected bool UseOnePageCheckout()
        {
            bool useMobileDevice = _mobileDeviceHelper.IsMobileDevice(_httpContext)
                && _mobileDeviceHelper.MobileDevicesSupported()
                && !_mobileDeviceHelper.CustomerDontUseMobileVersion();

            //mobile version doesn't support one-page checkout
            if (useMobileDevice)
                return false;

            //check the appropriate setting
            return _orderSettings.OnePageCheckoutEnabled;
        }

        [NonAction]
        protected bool IsMinimumOrderPlacementIntervalValid(Customer customer)
        {
            //prevent 2 orders being placed within an X seconds time frame
            if (_orderSettings.MinimumOrderPlacementInterval == 0)
                return true;

            var lastOrderPlaced = _orderService.GetOrdersByCustomerId(_workContext.CurrentCustomer.Id)
                .FirstOrDefault();
            if (lastOrderPlaced == null)
                return true;
            
            var interval = DateTime.UtcNow - lastOrderPlaced.CreatedOnUtc;
            return interval.TotalSeconds > _orderSettings.MinimumOrderPlacementInterval;
        }

        #endregion

        #region Methods (multistep checkout)

        public ActionResult Index()
        {
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            //reset checkout data
            _customerService.ResetCheckoutData(_workContext.CurrentCustomer, false);

            //validation (cart)
            var scWarnings = _shoppingCartService.GetShoppingCartWarnings(cart, _workContext.CurrentCustomer.CheckoutAttributes, true);
            if (scWarnings.Count > 0)
                return RedirectToRoute("ShoppingCart");
            //validation (each shopping cart item)
            foreach (ShoppingCartItem sci in cart)
            {
                var sciWarnings = _shoppingCartService.GetShoppingCartItemWarnings(_workContext.CurrentCustomer,
                    sci.ShoppingCartType,
                    sci.ProductVariant,
                    sci.AttributesXml,
                    sci.CustomerEnteredPrice,
                    sci.Quantity,
                    false);
                if (sciWarnings.Count > 0)
                    return RedirectToRoute("ShoppingCart");
            }

            if (UseOnePageCheckout())
                return RedirectToRoute("CheckoutOnePage");
            else
                return RedirectToRoute("CheckoutBillingAddress");
        }


        public ActionResult BillingAddress()
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (UseOnePageCheckout())
                return RedirectToRoute("CheckoutOnePage");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            //model
            var model = PrepareBillingAddressModel();
            return View(model);
        }
        public ActionResult SelectBillingAddress(int addressId)
        {
            var address = _workContext.CurrentCustomer.Addresses.Where(a => a.Id == addressId).FirstOrDefault();
            if (address == null)
                return RedirectToRoute("CheckoutBillingAddress");

            _workContext.CurrentCustomer.SetBillingAddress(address);
            _customerService.UpdateCustomer(_workContext.CurrentCustomer);

            return RedirectToRoute("CheckoutShippingAddress");
        }
        [HttpPost, ActionName("BillingAddress")]
        [FormValueRequired("nextstep")]
        public ActionResult NewBillingAddress(CheckoutBillingAddressModel model)
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (UseOnePageCheckout())
                return RedirectToRoute("CheckoutOnePage");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            if (ModelState.IsValid)
            {
                var address = model.NewAddress.ToEntity();
                address.CreatedOnUtc = DateTime.UtcNow;
                //some validation
                if (address.CountryId == 0)
                    address.CountryId = null;
                if (address.StateProvinceId == 0)
                    address.StateProvinceId = null;
                _workContext.CurrentCustomer.AddAddress(address);
                _workContext.CurrentCustomer.SetBillingAddress(address);
                _customerService.UpdateCustomer(_workContext.CurrentCustomer);

                return RedirectToRoute("CheckoutShippingAddress");
            }


            //If we got this far, something failed, redisplay form
            model = PrepareBillingAddressModel(model.NewAddress.CountryId);
            return View(model);
        }

        public ActionResult ShippingAddress()
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (UseOnePageCheckout())
                return RedirectToRoute("CheckoutOnePage");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            if (!cart.RequiresShipping())
            {
                _workContext.CurrentCustomer.SetShippingAddress(null);
                _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                return RedirectToRoute("CheckoutShippingMethod");
            }

            //model
            var model = PrepareShippingAddressModel();
            return View(model);
        }
        public ActionResult SelectShippingAddress(int addressId)
        {
            var address = _workContext.CurrentCustomer.Addresses.Where(a => a.Id == addressId).FirstOrDefault();
            if (address == null)
                return RedirectToRoute("CheckoutShippingAddress");

            _workContext.CurrentCustomer.SetShippingAddress(address);
            _customerService.UpdateCustomer(_workContext.CurrentCustomer);

            return RedirectToRoute("CheckoutShippingMethod");
        }
        [HttpPost, ActionName("ShippingAddress")]
        [FormValueRequired("nextstep")]
        public ActionResult NewShippingAddress(CheckoutShippingAddressModel model)
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (UseOnePageCheckout())
                return RedirectToRoute("CheckoutOnePage");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            if (!cart.RequiresShipping())
            {
                _workContext.CurrentCustomer.SetShippingAddress(null);
                _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                return RedirectToRoute("CheckoutShippingMethod");
            }

            if (ModelState.IsValid)
            {
                var address = model.NewAddress.ToEntity();
                address.CreatedOnUtc = DateTime.UtcNow;
                //some validation
                if (address.CountryId == 0)
                    address.CountryId = null;
                if (address.StateProvinceId == 0)
                    address.StateProvinceId = null;
                _workContext.CurrentCustomer.AddAddress(address);
                _workContext.CurrentCustomer.SetShippingAddress(address);
                _customerService.UpdateCustomer(_workContext.CurrentCustomer);

                return RedirectToRoute("CheckoutShippingMethod");
            }


            //If we got this far, something failed, redisplay form
            model = PrepareShippingAddressModel(model.NewAddress.CountryId);
            return View(model);
        }
        

        public ActionResult ShippingMethod()
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (UseOnePageCheckout())
                return RedirectToRoute("CheckoutOnePage");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            if (!cart.RequiresShipping())
            {
                _customerService.SaveCustomerAttribute<ShippingOption>(_workContext.CurrentCustomer, SystemCustomerAttributeNames.LastShippingOption, null);
                return RedirectToRoute("CheckoutPaymentMethod");
            }
            
            
            //model
            var model = PrepareShippingMethodModel(cart);
            return View(model);
        }
        [HttpPost, ActionName("ShippingMethod")]
        [FormValueRequired("nextstep")]
        [ValidateInput(false)]
        public ActionResult SelectShippingMethod(string shippingoption)
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (UseOnePageCheckout())
                return RedirectToRoute("CheckoutOnePage");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            if (!cart.RequiresShipping())
            {
                _customerService.SaveCustomerAttribute<ShippingOption>(_workContext.CurrentCustomer, SystemCustomerAttributeNames.LastShippingOption, null);
                return RedirectToRoute("CheckoutPaymentMethod");
            }

            //parse selected method 
            if (String.IsNullOrEmpty(shippingoption))
                return ShippingMethod();
            var splittedOption = shippingoption.Split(new string[] { "___" }, StringSplitOptions.RemoveEmptyEntries);
            if (splittedOption.Length != 2)
                return ShippingMethod();
            string selectedName = splittedOption[0];
            string shippingRateComputationMethodSystemName = splittedOption[1];

            //find it
            var shippingOptions = _shippingService.GetShippingOptions(cart, _workContext.CurrentCustomer.ShippingAddress, shippingRateComputationMethodSystemName);
            var shippingOption = shippingOptions.ShippingOptions.ToList()
                .Find(so => !String.IsNullOrEmpty(so.Name) && so.Name.Equals(selectedName, StringComparison.InvariantCultureIgnoreCase));
            if (shippingOption == null)
                return ShippingMethod();

            //save
            _customerService.SaveCustomerAttribute<ShippingOption>(_workContext.CurrentCustomer, SystemCustomerAttributeNames.LastShippingOption, shippingOption);
            
            return RedirectToRoute("CheckoutPaymentMethod");
        }
        
        
        public ActionResult PaymentMethod()
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (UseOnePageCheckout())
                return RedirectToRoute("CheckoutOnePage");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            //Check whether payment workflow is required
            //we ignore reward points during cart total calculation
            bool isPaymentWorkflowRequired = IsPaymentWorkflowRequired(cart, true);
            if (!isPaymentWorkflowRequired)
            {
                _workContext.CurrentCustomer.SelectedPaymentMethodSystemName = "";
                _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                return RedirectToRoute("CheckoutPaymentInfo");
            }

            //model
            var model = PreparePaymentMethodModel(cart);

            //if we have only one payment method and reward points are disabled or the current customer doesn't have any reward points
            if (_paymentSettings.BypassPaymentMethodSelectionIfOnlyOne &&
                model.PaymentMethods.Count == 1 && !model.DisplayRewardPoints)
            {
                _workContext.CurrentCustomer.SelectedPaymentMethodSystemName = model.PaymentMethods[0].PaymentMethodSystemName;
                _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                return RedirectToRoute("CheckoutPaymentInfo");
            }

            return View(model);
        }
        [HttpPost, ActionName("PaymentMethod")]
        [FormValueRequired("nextstep")]
        [ValidateInput(false)]
        public ActionResult SelectPaymentMethod(string paymentmethod, CheckoutPaymentMethodModel model)
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (UseOnePageCheckout())
                return RedirectToRoute("CheckoutOnePage");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            //reward points
            _workContext.CurrentCustomer.UseRewardPointsDuringCheckout = model.UseRewardPoints;
            _customerService.UpdateCustomer(_workContext.CurrentCustomer);

            //Check whether payment workflow is required
            bool isPaymentWorkflowRequired = IsPaymentWorkflowRequired(cart);
            if (!isPaymentWorkflowRequired)
            {
                _workContext.CurrentCustomer.SelectedPaymentMethodSystemName = "";
                _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                return RedirectToRoute("CheckoutPaymentInfo");
            }
            //payment method 
            if (String.IsNullOrEmpty(paymentmethod))
                return PaymentMethod();

            var paymentMethodInst = _paymentService.LoadPaymentMethodBySystemName(paymentmethod);
            if (paymentMethodInst == null || !paymentMethodInst.IsPaymentMethodActive(_paymentSettings))
                return PaymentMethod();

            //save
            _workContext.CurrentCustomer.SelectedPaymentMethodSystemName = paymentmethod;
            _customerService.UpdateCustomer(_workContext.CurrentCustomer);

            return RedirectToRoute("CheckoutPaymentInfo");
        }


        public ActionResult PaymentInfo()
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (UseOnePageCheckout())
                return RedirectToRoute("CheckoutOnePage");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            //Check whether payment workflow is required
            bool isPaymentWorkflowRequired = IsPaymentWorkflowRequired(cart);
            if (!isPaymentWorkflowRequired)
            {
                return RedirectToRoute("CheckoutConfirm");
            }
            
            var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(_workContext.CurrentCustomer.SelectedPaymentMethodSystemName);
            if (paymentMethod == null)
                return RedirectToRoute("CheckoutPaymentMethod");

            //model
            var model = PreparePaymentInfoModel(paymentMethod);
            return View(model);
        }
        [HttpPost, ActionName("PaymentInfo")]
        [FormValueRequired("nextstep")]
        [ValidateInput(false)]
        public ActionResult EnterPaymentInfo(FormCollection form)
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (UseOnePageCheckout())
                return RedirectToRoute("CheckoutOnePage");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            //Check whether payment workflow is required
            bool isPaymentWorkflowRequired = IsPaymentWorkflowRequired(cart);
            if (!isPaymentWorkflowRequired)
            {
                return RedirectToRoute("CheckoutConfirm");
            }

            var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(_workContext.CurrentCustomer.SelectedPaymentMethodSystemName);
            if (paymentMethod == null)
                return RedirectToRoute("CheckoutPaymentMethod");

            var paymentControllerType = paymentMethod.GetControllerType();
            var paymentController = DependencyResolver.Current.GetService(paymentControllerType) as BaseNopPaymentController;
            var warnings = paymentController.ValidatePaymentForm(form);
            foreach (var warning in warnings)
                ModelState.AddModelError("", warning);
            if (ModelState.IsValid)
            {
                //get payment info
                var paymentInfo = paymentController.GetPaymentInfo(form);
                //session save
                _httpContext.Session["OrderPaymentInfo"] = paymentInfo;
                return RedirectToRoute("CheckoutConfirm");
            }

            //If we got this far, something failed, redisplay form
            //model
            var model = PreparePaymentInfoModel(paymentMethod);
            return View(model);
        }
        

        public ActionResult Confirm()
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (UseOnePageCheckout())
                return RedirectToRoute("CheckoutOnePage");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            //model
            var model = PrepareConfirmOrderModel(cart);
            return View(model);
        }
        [HttpPost, ActionName("Confirm")]
        [ValidateInput(false)]
        public ActionResult ConfirmOrder()
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (UseOnePageCheckout())
                return RedirectToRoute("CheckoutOnePage");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();


            //model
            var model = new CheckoutConfirmModel();
            try
            {
                var processPaymentRequest = _httpContext.Session["OrderPaymentInfo"] as ProcessPaymentRequest;
                if (processPaymentRequest == null)
                {
                    //Check whether payment workflow is required
                    if (IsPaymentWorkflowRequired(cart))
                        return RedirectToRoute("CheckoutPaymentInfo");
                    else
                        processPaymentRequest = new ProcessPaymentRequest();
                }
                
                //prevent 2 orders being placed within an X seconds time frame
                if (!IsMinimumOrderPlacementIntervalValid(_workContext.CurrentCustomer))
                    throw new Exception(_localizationService.GetResource("Checkout.MinOrderPlacementInterval"));

                //place order
                processPaymentRequest.CustomerId = _workContext.CurrentCustomer.Id;
                processPaymentRequest.PaymentMethodSystemName = _workContext.CurrentCustomer.SelectedPaymentMethodSystemName;
                var placeOrderResult = _orderProcessingService.PlaceOrder(processPaymentRequest);
                if (placeOrderResult.Success)
                {
                    _httpContext.Session["OrderPaymentInfo"] = null;
                    var postProcessPaymentRequest = new PostProcessPaymentRequest()
                    {
                        Order = placeOrderResult.PlacedOrder
                    };
                    _paymentService.PostProcessPayment(postProcessPaymentRequest);

                    if (this.Response.IsRequestBeingRedirected)
                    {
                        //redirection has been done in PostProcessPayment
                        return Content("Redirected");
                    }
                    else
                    {
                        //if no redirection has been done (to a third-party payment page)
                        //theoretically it's not possible
                        return RedirectToRoute("CheckoutCompleted");
                    }
                }
                else
                {
                    foreach (var error in placeOrderResult.Errors)
                        model.Warnings.Add(error);
                }
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc);
                model.Warnings.Add(exc.Message);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }


        public ActionResult Completed()
        {
            //validation
            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            //model
            var model = new CheckoutCompletedModel();

            var orders = _orderService.GetOrdersByCustomerId(_workContext.CurrentCustomer.Id);
            if (orders.Count == 0)
                return RedirectToAction("Index", "Home");
            else
            {
                var lastOrder = orders[0];
                model.OrderId = lastOrder.Id;
            }

            return View(model);
        }
        
        [ChildActionOnly]
        public ActionResult CheckoutProgress(CheckoutProgressStep step)
        {
            var model = new CheckoutProgressModel() {CheckoutProgressStep = step};
            return PartialView(model);
        }
        #endregion

        #region Methods (one page checkout)

        public ActionResult OnePageCheckout()
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (cart.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (!UseOnePageCheckout())
                return RedirectToRoute("Checkout");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            var model = new OnePageCheckoutModel()
            {
                ShippingRequired = cart.RequiresShipping()
            };
            return View(model);
        }

        [ChildActionOnly]
        public ActionResult OpcBillingForm()
        {
            var billingAddressModel = PrepareBillingAddressModel();
            return PartialView("OpcBillingAddress", billingAddressModel);
        }

        [ValidateInput(false)]
        public ActionResult OpcSaveBilling(FormCollection form)
        {
            try
            {
                //validation
                var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
                if (cart.Count == 0)
                    throw new Exception("Your cart is empty");

                if (!UseOnePageCheckout())
                    throw new Exception("One page checkout is disabled");

                if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                    throw new Exception("Anonymous checkout is not allowed");

                int billingAddressId = 0;
                int.TryParse(form["billing_address_id"], out billingAddressId);

                if (billingAddressId > 0)
                {
                    //existing address
                    var address = _workContext.CurrentCustomer.Addresses.Where(a => a.Id == billingAddressId).FirstOrDefault();
                    if (address == null)
                        throw new Exception("Address can't be loaded");

                    _workContext.CurrentCustomer.SetBillingAddress(address);
                    _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                }
                else
                {
                    //new address
                    var model = new CheckoutBillingAddressModel();
                    TryUpdateModel(model.NewAddress, "BillingNewAddress");
                    //validate model
                    TryValidateModel(model.NewAddress);
                    if (!ModelState.IsValid)
                    {
                        //model is not valid. redisplay the form with errors
                        var billingAddressModel = PrepareBillingAddressModel(model.NewAddress.CountryId);
                        billingAddressModel.NewAddressPreselected = true;
                        return Json(new
                        {
                            update_section = new UpdateSectionJsonModel()
                            {
                                name = "billing",
                                html = RenderPartialViewToString("OpcBillingAddress", billingAddressModel)
                            }
                        });
                    }

                    //try to find an address with the same values (don't duplicate records)
                    var address = _workContext.CurrentCustomer.Addresses.ToList().FindAddress(
                        model.NewAddress.FirstName, model.NewAddress.LastName, model.NewAddress.PhoneNumber,
                        model.NewAddress.Email, model.NewAddress.FaxNumber, model.NewAddress.Company,
                        model.NewAddress.Address1, model.NewAddress.Address2, model.NewAddress.City,
                        model.NewAddress.StateProvinceId, model.NewAddress.ZipPostalCode, model.NewAddress.CountryId);
                    if (address == null)
                    {
                        //address is not found. let's create a new one
                        address = model.NewAddress.ToEntity();
                        address.CreatedOnUtc = DateTime.UtcNow;
                        //some validation
                        if (address.CountryId == 0)
                            address.CountryId = null;
                        if (address.StateProvinceId == 0)
                            address.StateProvinceId = null;
                        _workContext.CurrentCustomer.AddAddress(address);
                    }
                    _workContext.CurrentCustomer.SetBillingAddress(address);
                    _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                }

                if (cart.RequiresShipping())
                {
                    //shipping is required
                    var shippingAddressModel = PrepareShippingAddressModel();
                    return Json(new
                    {
                        update_section = new UpdateSectionJsonModel()
                        {
                            name = "shipping",
                            html = RenderPartialViewToString("OpcShippingAddress", shippingAddressModel)
                        },
                        goto_section = "shipping"
                    });
                }
                else
                {
                    //shipping is not required
                    _customerService.SaveCustomerAttribute<ShippingOption>(_workContext.CurrentCustomer, SystemCustomerAttributeNames.LastShippingOption, null);


                    //Check whether payment workflow is required
                    //we ignore reward points during cart total calculation
                    bool isPaymentWorkflowRequired = IsPaymentWorkflowRequired(cart, true);
                    if (isPaymentWorkflowRequired)
                    {
                        //payment is required
                        var paymentMethodModel = PreparePaymentMethodModel(cart);
                        return Json(new
                        {
                            update_section = new UpdateSectionJsonModel()
                            {
                                name = "payment-method",
                                html = RenderPartialViewToString("OpcPaymentMethods", paymentMethodModel)
                            },
                            goto_section = "payment_method"
                        });
                    }
                    else
                    {
                        //payment is not required
                        _workContext.CurrentCustomer.SelectedPaymentMethodSystemName = "";
                        _customerService.UpdateCustomer(_workContext.CurrentCustomer);

                        var confirmOrderModel = PrepareConfirmOrderModel(cart);
                        return Json(new
                        {
                            update_section = new UpdateSectionJsonModel()
                            {
                                name = "confirm-order",
                                html = RenderPartialViewToString("OpcConfirmOrder", confirmOrderModel)
                            },
                            goto_section = "confirm_order"
                        });
                    }
                }
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Json(new { error = 1, message = exc.Message });
            }
        }

        [ValidateInput(false)]
        public ActionResult OpcSaveShipping(FormCollection form)
        {
            try
            {
                //validation
                var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
                if (cart.Count == 0)
                    throw new Exception("Your cart is empty");

                if (!UseOnePageCheckout())
                    throw new Exception("One page checkout is disabled");

                if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                    throw new Exception("Anonymous checkout is not allowed");

                if (!cart.RequiresShipping())
                    throw new Exception("Shipping is not required");

                int shippingAddressId = 0;
                int.TryParse(form["shipping_address_id"], out shippingAddressId);

                if (shippingAddressId > 0)
                {
                    //existing address
                    var address = _workContext.CurrentCustomer.Addresses.Where(a => a.Id == shippingAddressId).FirstOrDefault();
                    if (address == null)
                        throw new Exception("Address can't be loaded");

                    _workContext.CurrentCustomer.SetShippingAddress(address);
                    _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                }
                else
                {
                    //new address
                    var model = new CheckoutShippingAddressModel();
                    TryUpdateModel(model.NewAddress, "ShippingNewAddress");
                    //validate model
                    TryValidateModel(model.NewAddress);
                    if (!ModelState.IsValid)
                    {
                        //model is not valid. redisplay the form with errors
                        var shippingAddressModel = PrepareShippingAddressModel(model.NewAddress.CountryId);
                        shippingAddressModel.NewAddressPreselected = true;
                        return Json(new
                        {
                            update_section = new UpdateSectionJsonModel()
                            {
                                name = "shipping",
                                html = RenderPartialViewToString("OpcShippingAddress", shippingAddressModel)
                            }
                        });
                    }

                    //try to find an address with the same values (don't duplicate records)
                    var address = _workContext.CurrentCustomer.Addresses.ToList().FindAddress(
                        model.NewAddress.FirstName, model.NewAddress.LastName, model.NewAddress.PhoneNumber,
                        model.NewAddress.Email, model.NewAddress.FaxNumber, model.NewAddress.Company,
                        model.NewAddress.Address1, model.NewAddress.Address2, model.NewAddress.City,
                        model.NewAddress.StateProvinceId, model.NewAddress.ZipPostalCode, model.NewAddress.CountryId);
                    if (address == null)
                    {
                        address = model.NewAddress.ToEntity();
                        address.CreatedOnUtc = DateTime.UtcNow;
                        //some validation
                        if (address.CountryId == 0)
                            address.CountryId = null;
                        if (address.StateProvinceId == 0)
                            address.StateProvinceId = null;
                        _workContext.CurrentCustomer.AddAddress(address);
                    }
                    _workContext.CurrentCustomer.SetShippingAddress(address);
                    _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                }

                var shippingMethodModel = PrepareShippingMethodModel(cart);
                return Json(new
                {
                    update_section = new UpdateSectionJsonModel()
                    {
                        name = "shipping-method",
                        html = RenderPartialViewToString("OpcShippingMethods", shippingMethodModel)
                    },
                    goto_section = "shipping_method"
                });
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Json(new { error = 1, message = exc.Message });
            }
        }

        [ValidateInput(false)]
        public ActionResult OpcSaveShippingMethod(FormCollection form)
        {
            try
            {
                //validation
                var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
                if (cart.Count == 0)
                    throw new Exception("Your cart is empty");

                if (!UseOnePageCheckout())
                    throw new Exception("One page checkout is disabled");

                if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                    throw new Exception("Anonymous checkout is not allowed");
                
                if (!cart.RequiresShipping())
                    throw new Exception("Shipping is not required");

                //parse selected method 
                string shippingoption = form["shippingoption"];
                if (String.IsNullOrEmpty(shippingoption))
                    throw new Exception("Selected shipping method can't be parsed");
                var splittedOption = shippingoption.Split(new string[] { "___" }, StringSplitOptions.RemoveEmptyEntries);
                if (splittedOption.Length != 2)
                    throw new Exception("Selected shipping method can't be parsed");
                string selectedName = splittedOption[0];
                string shippingRateComputationMethodSystemName = splittedOption[1];
                
                //find it
                var shippingOptions = _shippingService.GetShippingOptions(cart, _workContext.CurrentCustomer.ShippingAddress, shippingRateComputationMethodSystemName);
                var shippingOption = shippingOptions.ShippingOptions.ToList()
                    .Find(so => !String.IsNullOrEmpty(so.Name) && so.Name.Equals(selectedName, StringComparison.InvariantCultureIgnoreCase));
                if (shippingOption == null)
                    throw new Exception("Selected shipping method can't be loaded");

                //save
                _customerService.SaveCustomerAttribute<ShippingOption>(_workContext.CurrentCustomer, SystemCustomerAttributeNames.LastShippingOption, shippingOption);


                bool isPaymentWorkflowRequired = IsPaymentWorkflowRequired(cart, true);
                if (isPaymentWorkflowRequired)
                {
                    //payment is required
                    var paymentMethodModel = PreparePaymentMethodModel(cart);
                    return Json(new
                    {
                        update_section = new UpdateSectionJsonModel()
                        {
                            name = "payment-method",
                            html = RenderPartialViewToString("OpcPaymentMethods", paymentMethodModel)
                        },
                        goto_section = "payment_method"
                    });
                }
                else
                {
                    //payment is not required
                    _workContext.CurrentCustomer.SelectedPaymentMethodSystemName = "";
                    _customerService.UpdateCustomer(_workContext.CurrentCustomer);

                    var confirmOrderModel = PrepareConfirmOrderModel(cart);
                    return Json(new
                    {
                        update_section = new UpdateSectionJsonModel()
                        {
                            name = "confirm-order",
                            html = RenderPartialViewToString("OpcConfirmOrder", confirmOrderModel)
                        },
                        goto_section = "confirm_order"
                    });
                }
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Json(new { error = 1, message = exc.Message });
            }
        }

        [ValidateInput(false)]
        public ActionResult OpcSavePaymentMethod(FormCollection form)
        {
            try
            {
                //validation
                var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
                if (cart.Count == 0)
                    throw new Exception("Your cart is empty");

                if (!UseOnePageCheckout())
                    throw new Exception("One page checkout is disabled");

                if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                    throw new Exception("Anonymous checkout is not allowed");

                string paymentmethod = form["paymentmethod"];
                //payment method 
                if (String.IsNullOrEmpty(paymentmethod))
                    throw new Exception("Selected payment method can't be parsed");


                var model = new CheckoutPaymentMethodModel();
                TryUpdateModel(model);

                //reward points
                _workContext.CurrentCustomer.UseRewardPointsDuringCheckout = model.UseRewardPoints;
                _customerService.UpdateCustomer(_workContext.CurrentCustomer);

                //Check whether payment workflow is required
                //we ignore reward points during cart total calculation
                bool isPaymentWorkflowRequired = IsPaymentWorkflowRequired(cart);
                if (!isPaymentWorkflowRequired)
                {
                    //payment is not required
                    _workContext.CurrentCustomer.SelectedPaymentMethodSystemName = "";
                    _customerService.UpdateCustomer(_workContext.CurrentCustomer);

                    var confirmOrderModel = PrepareConfirmOrderModel(cart);
                    return Json(new
                    {
                        update_section = new UpdateSectionJsonModel()
                        {
                            name = "confirm-order",
                            html = RenderPartialViewToString("OpcConfirmOrder", confirmOrderModel)
                        },
                        goto_section = "confirm_order"
                    });
                }

                var paymentMethodInst = _paymentService.LoadPaymentMethodBySystemName(paymentmethod);
                if (paymentMethodInst == null || !paymentMethodInst.IsPaymentMethodActive(_paymentSettings))
                    throw new Exception("Selected payment method can't be parsed");

                //save
                _workContext.CurrentCustomer.SelectedPaymentMethodSystemName = paymentmethod;
                _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                

                var paymenInfoModel = PreparePaymentInfoModel(paymentMethodInst);
                return Json(new
                {
                    update_section = new UpdateSectionJsonModel()
                    {
                        name = "payment-info",
                        html = RenderPartialViewToString("OpcPaymentInfo", paymenInfoModel)
                    },
                    goto_section = "payment_info"
                });
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Json(new { error = 1, message = exc.Message });
            }
        }

        [ValidateInput(false)]
        public ActionResult OpcSavePaymentInfo(FormCollection form)
        {
            try
            {
                //validation
                var cart =
                    _workContext.CurrentCustomer.ShoppingCartItems.Where(
                        sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
                if (cart.Count == 0)
                    throw new Exception("Your cart is empty");

                if (!UseOnePageCheckout())
                    throw new Exception("One page checkout is disabled");

                if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                    throw new Exception("Anonymous checkout is not allowed");

                var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(
                        _workContext.CurrentCustomer.SelectedPaymentMethodSystemName);
                if (paymentMethod == null)
                    throw new Exception("Payment method is not selected");

                var paymentControllerType = paymentMethod.GetControllerType();
                var paymentController =
                    DependencyResolver.Current.GetService(paymentControllerType) as BaseNopPaymentController;
                var warnings = paymentController.ValidatePaymentForm(form);
                foreach (var warning in warnings)
                    ModelState.AddModelError("", warning);
                if (ModelState.IsValid)
                {
                    //get payment info
                    var paymentInfo = paymentController.GetPaymentInfo(form);
                    //session save
                    _httpContext.Session["OrderPaymentInfo"] = paymentInfo;

                    var confirmOrderModel = PrepareConfirmOrderModel(cart);
                    return Json(new
                    {
                        update_section = new UpdateSectionJsonModel()
                        {
                            name = "confirm-order",
                            html = RenderPartialViewToString("OpcConfirmOrder", confirmOrderModel)
                        },
                        goto_section = "confirm_order"
                    });
                }

                //If we got this far, something failed, redisplay form
                var paymenInfoModel = PreparePaymentInfoModel(paymentMethod);
                return Json(new
                {
                    update_section = new UpdateSectionJsonModel()
                    {
                        name = "payment-info",
                        html = RenderPartialViewToString("OpcPaymentInfo", paymenInfoModel)
                    }
                });
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Json(new { error = 1, message = exc.Message });
            }
        }

        [ValidateInput(false)]
        public ActionResult OpcConfirmOrder()
        {
            try
            {
                //validation
                var cart = _workContext.CurrentCustomer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
                if (cart.Count == 0)
                    throw new Exception("Your cart is empty");

                if (!UseOnePageCheckout())
                    throw new Exception("One page checkout is disabled");

                if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                    throw new Exception("Anonymous checkout is not allowed");

                //prevent 2 orders being placed within an X seconds time frame
                if (!IsMinimumOrderPlacementIntervalValid(_workContext.CurrentCustomer))
                    throw new Exception(_localizationService.GetResource("Checkout.MinOrderPlacementInterval"));

                //place order
                var processPaymentRequest = _httpContext.Session["OrderPaymentInfo"] as ProcessPaymentRequest;
                if (processPaymentRequest == null)
                {
                    //Check whether payment workflow is required
                    if (IsPaymentWorkflowRequired(cart))
                    {
                        throw new Exception("Payment information is not entered");
                    }
                    else
                        processPaymentRequest = new ProcessPaymentRequest();
                }

                processPaymentRequest.CustomerId = _workContext.CurrentCustomer.Id;
                processPaymentRequest.PaymentMethodSystemName = _workContext.CurrentCustomer.SelectedPaymentMethodSystemName;
                var placeOrderResult = _orderProcessingService.PlaceOrder(processPaymentRequest);
                if (placeOrderResult.Success)
                {
                    _httpContext.Session["OrderPaymentInfo"] = null;
                    var postProcessPaymentRequest = new PostProcessPaymentRequest()
                    {
                        Order = placeOrderResult.PlacedOrder
                    };


                    var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(placeOrderResult.PlacedOrder.PaymentMethodSystemName);
                    if (paymentMethod != null)
                    {
                        if (paymentMethod.PaymentMethodType == PaymentMethodType.Redirection)
                        {
                            //Redirection will not work because it's AJAX request.
                            //That's why we don't process it here (we redirect a user to another page where he'll be redirected)

                            //redirect
                            return Json(new { redirect = string.Format("{0}checkout/OpcCompleteRedirectionPayment", _webHelper.GetStoreLocation()) });
                        }
                        else
                        {
                            _paymentService.PostProcessPayment(postProcessPaymentRequest);
                            //success
                            return Json(new { success = 1 });
                        }
                    }
                    else
                    {
                        //payment method could be null if order total is 0

                        //success
                        return Json(new { success = 1 });
                    }
                }
                else
                {
                    //error
                    var confirmOrderModel = new CheckoutConfirmModel();
                    foreach (var error in placeOrderResult.Errors)
                        confirmOrderModel.Warnings.Add(error); 
                    
                    return Json(new
                        {
                            update_section = new UpdateSectionJsonModel()
                            {
                                name = "confirm-order",
                                html = RenderPartialViewToString("OpcConfirmOrder", confirmOrderModel)
                            },
                            goto_section = "confirm_order"
                        });
                }
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Json(new { error = 1, message = exc.Message });
            }
        }

        public ActionResult OpcCompleteRedirectionPayment()
        {
            try
            {
                //validation
                if (!UseOnePageCheckout())
                    return RedirectToAction("Index", "Home");

                if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                    return new HttpUnauthorizedResult();

                //get the order
                var orders = _orderService.GetOrdersByCustomerId(_workContext.CurrentCustomer.Id);
                if (orders.Count == 0)
                    return RedirectToAction("Index", "Home");

                
                var order = orders[0];
                var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(order.PaymentMethodSystemName);
                if (paymentMethod == null)
                    return RedirectToAction("Index", "Home");
                if (paymentMethod.PaymentMethodType != PaymentMethodType.Redirection)
                    return RedirectToAction("Index", "Home");

                //ensure that order has been just placed
                if ((DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes > 3)
                    return RedirectToAction("Index", "Home");


                //Redirection will not work on one page checkout page because it's AJAX request.
                //That's why we process it here
                var postProcessPaymentRequest = new PostProcessPaymentRequest()
                {
                    Order = order
                };

                _paymentService.PostProcessPayment(postProcessPaymentRequest);

                if (this.Response.IsRequestBeingRedirected)
                {
                    //redirection has been done in PostProcessPayment
                    return Content("Redirected");
                }
                else
                {
                    //if no redirection has been done (to a third-party payment page)
                    //theoretically it's not possible
                    return RedirectToRoute("CheckoutCompleted");
                }
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Content(exc.Message);
            }
        }

        #endregion
    }
}
