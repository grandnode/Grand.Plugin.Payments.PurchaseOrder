﻿using System.Collections.Generic;
using System.Web.Mvc;
using Grand.Core;
using Grand.Plugin.Payments.PurchaseOrder.Models;
using Grand.Services.Configuration;
using Grand.Services.Localization;
using Grand.Services.Payments;
using Grand.Services.Stores;
using Grand.Web.Framework.Controllers;
using System;

namespace Grand.Plugin.Payments.PurchaseOrder.Controllers
{
    public class PaymentPurchaseOrderController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;

        public PaymentPurchaseOrderController(IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService,
            ILocalizationService localizationService)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._localizationService = localizationService;
        }
        
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var purchaseOrderPaymentSettings = _settingService.LoadSetting<PurchaseOrderPaymentSettings>(storeScope);

            var model = new ConfigurationModel();
            model.AdditionalFee = purchaseOrderPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = purchaseOrderPaymentSettings.AdditionalFeePercentage;
            model.ShippableProductRequired = purchaseOrderPaymentSettings.ShippableProductRequired;

            model.ActiveStoreScopeConfiguration = storeScope;
            if (!String.IsNullOrEmpty(storeScope))
            {
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(purchaseOrderPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(purchaseOrderPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.ShippableProductRequired_OverrideForStore = _settingService.SettingExists(purchaseOrderPaymentSettings, x => x.ShippableProductRequired, storeScope);
            }

            return View("~/Plugins/Payments.PurchaseOrder/Views/PaymentPurchaseOrder/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var purchaseOrderPaymentSettings = _settingService.LoadSetting<PurchaseOrderPaymentSettings>(storeScope);

            //save settings
            purchaseOrderPaymentSettings.AdditionalFee = model.AdditionalFee;
            purchaseOrderPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            purchaseOrderPaymentSettings.ShippableProductRequired = model.ShippableProductRequired;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.AdditionalFee_OverrideForStore || String.IsNullOrEmpty(storeScope))
                _settingService.SaveSetting(purchaseOrderPaymentSettings, x => x.AdditionalFee, storeScope, false);
            else if (!String.IsNullOrEmpty(storeScope))
                _settingService.DeleteSetting(purchaseOrderPaymentSettings, x => x.AdditionalFee, storeScope);

            if (model.AdditionalFeePercentage_OverrideForStore || String.IsNullOrEmpty(storeScope))
                _settingService.SaveSetting(purchaseOrderPaymentSettings, x => x.AdditionalFeePercentage, storeScope, false);
            else if (!String.IsNullOrEmpty(storeScope))
                _settingService.DeleteSetting(purchaseOrderPaymentSettings, x => x.AdditionalFeePercentage, storeScope);

            if (model.ShippableProductRequired_OverrideForStore || String.IsNullOrEmpty(storeScope))
                _settingService.SaveSetting(purchaseOrderPaymentSettings, x => x.ShippableProductRequired, storeScope, false);
            else if (!String.IsNullOrEmpty(storeScope))
                _settingService.DeleteSetting(purchaseOrderPaymentSettings, x => x.ShippableProductRequired, storeScope);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();
            
            //set postback values
            var form = this.Request.Form;
            model.PurchaseOrderNumber = form["PurchaseOrderNumber"];

            return View("~/Plugins/Payments.PurchaseOrder/Views/PaymentPurchaseOrder/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            paymentInfo.CustomValues.Add(_localizationService.GetResource("Plugins.Payment.PurchaseOrder.PurchaseOrderNumber"), form["PurchaseOrderNumber"]);
            return paymentInfo;
        }
    }
}