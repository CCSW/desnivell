﻿using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Catalog
{
    public class ProductSpecificationModel : BaseNopModel
    {
        public int SpecificationAttributeId { get; set; }

        public string SpecificationAttributeName { get; set; }

        public string SpecificationAttributeOption { get; set; }
    }
}