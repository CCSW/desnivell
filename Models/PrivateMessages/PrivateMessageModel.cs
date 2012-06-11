﻿using System;
using System.Web.Mvc;
using FluentValidation.Attributes;
using Nop.Web.Framework.Mvc;
using Nop.Web.Validators.PrivateMessages;

namespace Nop.Web.Models.PrivateMessages
{
    [Validator(typeof(SendPrivateMessageValidator))]
    public class PrivateMessageModel : BaseNopEntityModel
    {
        public int FromCustomerId { get; set; }
        public string CustomerFromName { get; set; }
        public bool AllowViewingFromProfile { get; set; }

        public int ToCustomerId { get; set; }
        public string CustomerToName { get; set; }
        public bool AllowViewingToProfile { get; set; }

        public string Subject { get; set; }

        public string Message { get; set; }
        
        public DateTime CreatedOnUtc { get; set; }

        public bool IsRead { get; set; }
    }
}