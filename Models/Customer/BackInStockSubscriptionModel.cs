﻿using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Customer
{
    public class BackInStockSubscriptionModel : BaseNopEntityModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string SeName { get; set; }
    }
}
