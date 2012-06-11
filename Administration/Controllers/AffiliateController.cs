﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Admin.Models.Affiliates;
using Nop.Core;
using Nop.Core.Domain.Affiliates;
using Nop.Core.Domain.Directory;
using Nop.Services.Affiliates;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Telerik.Web.Mvc;

namespace Nop.Admin.Controllers
{
    [AdminAuthorize]
    public class AffiliateController : BaseNopController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IWebHelper _webHelper;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IAffiliateService _affiliateService;
        private readonly IPermissionService _permissionService;

        #endregion

        #region Constructors

        public AffiliateController(ILocalizationService localizationService,
            IWorkContext workContext, IDateTimeHelper dateTimeHelper, IWebHelper webHelper,
            ICountryService countryService, IStateProvinceService stateProvinceService,
            IPriceFormatter priceFormatter, IAffiliateService affiliateService,
            IPermissionService permissionService)
        {
            this._localizationService = localizationService;
            this._workContext = workContext;
            this._dateTimeHelper = dateTimeHelper;
            this._webHelper = webHelper;
            this._countryService = countryService;
            this._stateProvinceService = stateProvinceService;
            this._priceFormatter = priceFormatter;
            this._affiliateService = affiliateService;
            this._permissionService = permissionService;
        }

        #endregion

        #region Utilities

        [NonAction]
        private void PrepareAffiliateModel(AffiliateModel model, Affiliate affiliate, bool excludeProperties)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            if (affiliate != null)
            {
                model.Id = affiliate.Id;
                model.Url = _webHelper.ModifyQueryString(_webHelper.GetStoreLocation(false), "affiliateid=" + affiliate.Id, null);
                if (!excludeProperties)
                {
                    model.Active = affiliate.Active;
                    model.Address = affiliate.Address.ToModel();
                }
            }

            //address
            model.Address.AvailableCountries.Add(new SelectListItem() { Text = _localizationService.GetResource("Admin.Address.SelectCountry"), Value = "0" });
            foreach (var c in _countryService.GetAllCountries(true))
                model.Address.AvailableCountries.Add(new SelectListItem() { Text = c.Name, Value = c.Id.ToString(), Selected = (affiliate != null && c.Id == affiliate.Address.CountryId) });
            
            var states = model.Address.CountryId.HasValue ? _stateProvinceService.GetStateProvincesByCountryId(model.Address.CountryId.Value, true).ToList() : new List<StateProvince>();
            if (states.Count > 0)
            {
                foreach (var s in states)
                    model.Address.AvailableStates.Add(new SelectListItem() { Text = s.Name, Value = s.Id.ToString(), Selected = (affiliate != null && s.Id == affiliate.Address.StateProvinceId) });
            }
            else
                model.Address.AvailableStates.Add(new SelectListItem() { Text = _localizationService.GetResource("Admin.Address.OtherNonUS"), Value = "0" });
        }
        
        #endregion

        #region Methods

        //list
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        public ActionResult List()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            var gridModel = new GridModel<AffiliateModel>();
            return View(gridModel);
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult List(GridCommand command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            var affiliates = _affiliateService.GetAllAffiliates(true);
            var gridModel = new GridModel<AffiliateModel>
            {
                Data = affiliates.PagedForCommand(command).Select(x =>
                {
                    var m = new AffiliateModel();
                    PrepareAffiliateModel(m, x, false);
                    return m;
                }),
                Total = affiliates.Count,
            };
            return new JsonResult
            {
                Data = gridModel
            };
        }

        //create

        public ActionResult Create()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            var model = new AffiliateModel();
            PrepareAffiliateModel(model, null, false);
            return View(model);
        }

        [HttpPost, FormValueExists("save", "save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        public ActionResult Create(AffiliateModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            if (ModelState.IsValid)
            {
                var affiliate = new Affiliate();

                affiliate.Active = model.Active;
                affiliate.Address = model.Address.ToEntity();
                affiliate.Address.CreatedOnUtc = DateTime.UtcNow;
                //some validation
                if (affiliate.Address.CountryId == 0)
                    affiliate.Address.CountryId = null;
                if (affiliate.Address.StateProvinceId == 0)
                    affiliate.Address.StateProvinceId = null;
                _affiliateService.InsertAffiliate(affiliate);

                SuccessNotification(_localizationService.GetResource("Admin.Affiliates.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = affiliate.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            PrepareAffiliateModel(model, null, true);
            return View(model);

        }


        //edit
        public ActionResult Edit(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            var affiliate = _affiliateService.GetAffiliateById(id);
            if (affiliate == null || affiliate.Deleted)
                //No affiliate found with the specified id
                return RedirectToAction("List");

            var model = new AffiliateModel();
            PrepareAffiliateModel(model, affiliate, false);
            return View(model);
        }

        [HttpPost, FormValueExists("save", "save-continue", "continueEditing")]
        public ActionResult Edit(AffiliateModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            var affiliate = _affiliateService.GetAffiliateById(model.Id);
            if (affiliate == null || affiliate.Deleted)
                //No affiliate found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                affiliate.Active = model.Active;
                affiliate.Address = model.Address.ToEntity(affiliate.Address);
                //some validation
                if (affiliate.Address.CountryId == 0)
                    affiliate.Address.CountryId = null;
                if (affiliate.Address.StateProvinceId == 0)
                    affiliate.Address.StateProvinceId = null;
                _affiliateService.UpdateAffiliate(affiliate);

                SuccessNotification(_localizationService.GetResource("Admin.Affiliates.Updated"));
                return continueEditing ? RedirectToAction("Edit", affiliate.Id) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            PrepareAffiliateModel(model, affiliate, true);
            return View(model);
        }

        //delete
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            var affiliate = _affiliateService.GetAffiliateById(id);
            if (affiliate == null)
                //No affiliate found with the specified id
                return RedirectToAction("List");

            _affiliateService.DeleteAffiliate(affiliate);
            SuccessNotification(_localizationService.GetResource("Admin.Affiliates.Deleted"));
            return RedirectToAction("List");
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult AffiliatedOrderList(int affiliateId, GridCommand command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            var affiliate = _affiliateService.GetAffiliateById(affiliateId);
            if (affiliate == null)
                throw new ArgumentException("No affiliate found with the specified id");

            var orders = affiliate.AffiliatedOrders
                .Where(o => !o.Deleted)
                .OrderBy(x => x.CreatedOnUtc)
                .ToList();
            var model = new GridModel<AffiliateModel.AffiliatedOrderModel>
            {
                Data = orders.PagedForCommand(command)
                    .Select(order =>
                    {
                        var orderModel = new AffiliateModel.AffiliatedOrderModel();
                        orderModel.Id = order.Id;
                        orderModel.OrderStatus = order.OrderStatus.GetLocalizedEnum(_localizationService, _workContext);
                        orderModel.PaymentStatus = order.PaymentStatus.GetLocalizedEnum(_localizationService, _workContext);
                        orderModel.ShippingStatus = order.ShippingStatus.GetLocalizedEnum(_localizationService, _workContext);
                        orderModel.OrderTotal = _priceFormatter.FormatPrice(order.OrderTotal, true, false);
                        orderModel.CreatedOn = _dateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc);
                        return orderModel;
                    }),
                Total = orders.Count
            };

            return new JsonResult
            {
                Data = model
            };
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult AffiliatedCustomerList(int affiliateId, GridCommand command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            var affiliate = _affiliateService.GetAffiliateById(affiliateId);
            if (affiliate == null)
                throw new ArgumentException("No affiliate found with the specified id");

            var customers = affiliate.AffiliatedCustomers
                .Where(c => !c.Deleted)
                .OrderBy(x => x.CreatedOnUtc)
                .ToList();
            var model = new GridModel<AffiliateModel.AffiliatedCustomerModel>
            {
                Data = customers.PagedForCommand(command)
                    .Select(customer =>
                    {
                        var customerModel = new AffiliateModel.AffiliatedCustomerModel();
                        customerModel.Id = customer.Id;
                        customerModel.Name = customer.Email;
                        return customerModel;
                    }),
                Total = customers.Count
            };

            return new JsonResult
            {
                Data = model
            };
        }
        #endregion
    }
}
