﻿using Nop.Web.Framework.Mvc;
using Nop.Web.Models.Common;

namespace Nop.Web.Models.Customer
{
    public class CustomerAddressEditModel : BaseNopModel
    {
        public AddressModel Address { get; set; }
        public CustomerNavigationModel NavigationModel { get; set; }
    }
}