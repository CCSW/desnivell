﻿using System;
using FluentValidation.Attributes;
using Nop.Admin.Models.Common;
using Nop.Admin.Validators.Affiliates;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Affiliates
{
    [Validator(typeof(AffiliateValidator))]
    public class AffiliateModel : BaseNopEntityModel
    {
        public AffiliateModel()
        {
            Address = new AddressModel();
        }

        [NopResourceDisplayName("Admin.Affiliates.Fields.ID")]
        public override int Id { get; set; }

        [NopResourceDisplayName("Admin.Affiliates.Fields.URL")]
        public string Url { get; set; }
        
        [NopResourceDisplayName("Admin.Affiliates.Fields.Active")]
        public bool Active { get; set; }

        public AddressModel Address { get; set; }

        #region Nested classes
        
        public class AffiliatedOrderModel : BaseNopEntityModel
        {
            [NopResourceDisplayName("Admin.Affiliates.Orders.Order")]
            public override int Id { get; set; }

            [NopResourceDisplayName("Admin.Affiliates.Orders.OrderStatus")]
            public string OrderStatus { get; set; }

            [NopResourceDisplayName("Admin.Affiliates.Orders.PaymentStatus")]
            public string PaymentStatus { get; set; }

            [NopResourceDisplayName("Admin.Affiliates.Orders.ShippingStatus")]
            public string ShippingStatus { get; set; }

            [NopResourceDisplayName("Admin.Affiliates.Orders.OrderTotal")]
            public string OrderTotal { get; set; }

            [NopResourceDisplayName("Admin.Affiliates.Orders.CreatedOn")]
            public DateTime CreatedOn { get; set; }
        }

        public class AffiliatedCustomerModel : BaseNopEntityModel
        {
            [NopResourceDisplayName("Admin.Affiliates.Customers.Name")]
            public string Name { get; set; }
        }

        #endregion
    }
}