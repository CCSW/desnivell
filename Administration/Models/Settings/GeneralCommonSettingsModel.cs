﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Nop.Core.Domain.Common;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Settings
{
    public class GeneralCommonSettingsModel : BaseNopModel
    {
        public GeneralCommonSettingsModel()
        {
            StoreInformationSettings = new StoreInformationSettingsModel();
            SeoSettings = new SeoSettingsModel();
            SecuritySettings = new SecuritySettingsModel();
            PdfSettings = new PdfSettingsModel();
            LocalizationSettings = new LocalizationSettingsModel();
        }
        public StoreInformationSettingsModel StoreInformationSettings { get; set; }
        public SeoSettingsModel SeoSettings { get; set; }
        public SecuritySettingsModel SecuritySettings { get; set; }
        public PdfSettingsModel PdfSettings { get; set; }
        public LocalizationSettingsModel LocalizationSettings { get; set; }

        #region Nested classes

        public class StoreInformationSettingsModel
        {
            public StoreInformationSettingsModel()
            {
                this.AvailableStoreThemesForDesktops = new List<SelectListItem>();
                this.AvailableStoreThemesForMobileDevices = new List<SelectListItem>();
            }
            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.StoreName")]
            [AllowHtml]
            public string StoreName { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.StoreUrl")]
            [AllowHtml]
            public string StoreUrl { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.MobileDevicesSupported")]
            public bool MobileDevicesSupported { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.StoreClosed")]
            public bool StoreClosed { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.StoreClosedAllowForAdmins")]
            public bool StoreClosedAllowForAdmins { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.DefaultStoreThemeForDesktops")]
            [AllowHtml]
            public string DefaultStoreThemeForDesktops { get; set; }
            public IList<SelectListItem> AvailableStoreThemesForDesktops { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.DefaultStoreThemeForMobileDevices")]
            [AllowHtml]
            public string DefaultStoreThemeForMobileDevices { get; set; }
            public IList<SelectListItem> AvailableStoreThemesForMobileDevices { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.AllowCustomerToSelectTheme")]
            public bool AllowCustomerToSelectTheme { get; set; }
        }

        public class SeoSettingsModel
        {
            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.PageTitleSeparator")]
            [AllowHtml]
            public string PageTitleSeparator { get; set; }
            
            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.PageTitleSeoAdjustment")]
            public PageTitleSeoAdjustment PageTitleSeoAdjustment { get; set; }
            public SelectList PageTitleSeoAdjustmentValues { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.DefaultTitle")]
            [AllowHtml]
            public string DefaultTitle { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.DefaultMetaKeywords")]
            [AllowHtml]
            public string DefaultMetaKeywords { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.DefaultMetaDescription")]
            [AllowHtml]
            public string DefaultMetaDescription { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.ConvertNonWesternChars")]
            public bool ConvertNonWesternChars { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.CanonicalUrlsEnabled")]
            public bool CanonicalUrlsEnabled { get; set; }
        }

        public class SecuritySettingsModel
        {
            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.EncryptionKey")]
            [AllowHtml]
            public string EncryptionKey { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.AdminAreaAllowedIpAddresses")]
            [AllowHtml]
            public string AdminAreaAllowedIpAddresses { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.HideAdminMenuItemsBasedOnPermissions")]
            public bool HideAdminMenuItemsBasedOnPermissions { get; set; }




            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.CaptchaEnabled")]
            public bool CaptchaEnabled { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.CaptchaShowOnLoginPage")]
            public bool CaptchaShowOnLoginPage { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.CaptchaShowOnRegistrationPage")]
            public bool CaptchaShowOnRegistrationPage { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.CaptchaShowOnContactUsPage")]
            public bool CaptchaShowOnContactUsPage { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.CaptchaShowOnEmailWishlistToFriendPage")]
            public bool CaptchaShowOnEmailWishlistToFriendPage { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.CaptchaShowOnEmailProductToFriendPage")]
            public bool CaptchaShowOnEmailProductToFriendPage { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.reCaptchaPublicKey")]
            [AllowHtml]
            public string ReCaptchaPublicKey { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.reCaptchaPrivateKey")]
            [AllowHtml]
            public string ReCaptchaPrivateKey { get; set; }




            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.UseSSL")]
            public bool UseSsl { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.SharedSSLUrl")]
            [AllowHtml]
            public string SharedSslUrl { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.NonSharedSSLUrl")]
            [AllowHtml]
            public string NonSharedSslUrl { get; set; }
        }

        public class PdfSettingsModel
        {
            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.PdfEnabled")]
            public bool Enabled { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.PdfLetterPageSizeEnabled")]
            public bool LetterPageSizeEnabled { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.PdfLogo")]
            [UIHint("Picture")]
            public int LogoPictureId { get; set; }
        }

        public class LocalizationSettingsModel
        {
            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.UseImagesForLanguageSelection")]
            public bool UseImagesForLanguageSelection { get; set; }

            [NopResourceDisplayName("Admin.Configuration.Settings.GeneralCommon.SeoFriendlyUrlsForLanguagesEnabled")]
            public bool SeoFriendlyUrlsForLanguagesEnabled { get; set; }
        }

        #endregion
    }
}