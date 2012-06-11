﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Admin.Models.Catalog;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Discounts;
using Nop.Services.Catalog;
using Nop.Services.Discounts;
using Nop.Services.ExportImport;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Telerik.Web.Mvc;
using Telerik.Web.Mvc.UI;

namespace Nop.Admin.Controllers
{
    [AdminAuthorize]
    public class CategoryController : BaseNopController
    {
        #region Fields

        private readonly ICategoryService _categoryService;
        private readonly ICategoryTemplateService _categoryTemplateService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IProductService _productService;
        private readonly IPictureService _pictureService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IDiscountService _discountService;
        private readonly IPermissionService _permissionService;
        private readonly IExportManager _exportManager;
        private readonly IWorkContext _workContext;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly AdminAreaSettings _adminAreaSettings;
        private readonly CatalogSettings _catalogSettings;

        #endregion
        
        #region Constructors

        public CategoryController(ICategoryService categoryService, ICategoryTemplateService categoryTemplateService,
            IManufacturerService manufacturerService, IProductService productService, 
            IPictureService pictureService, ILanguageService languageService,
            ILocalizationService localizationService, ILocalizedEntityService localizedEntityService,
            IDiscountService discountService, IPermissionService permissionService,
            IExportManager exportManager, IWorkContext workContext,
            ICustomerActivityService customerActivityService, AdminAreaSettings adminAreaSettings,
            CatalogSettings catalogSettings)
        {
            this._categoryService = categoryService;
            this._categoryTemplateService = categoryTemplateService;
            this._manufacturerService = manufacturerService;
            this._productService = productService;
            this._pictureService = pictureService;
            this._languageService = languageService;
            this._localizationService = localizationService;
            this._localizedEntityService = localizedEntityService;
            this._discountService = discountService;
            this._permissionService = permissionService;
            this._exportManager = exportManager;
            this._workContext = workContext;
            this._customerActivityService = customerActivityService;
            this._adminAreaSettings = adminAreaSettings;
            this._catalogSettings = catalogSettings;
        }

        #endregion
        
        #region Utilities

        [NonAction]
        public void UpdateLocales(Category category, CategoryModel model)
        {
            foreach (var localized in model.Locales)
            {
                _localizedEntityService.SaveLocalizedValue(category,
                                                               x => x.Name,
                                                               localized.Name,
                                                               localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(category,
                                                           x => x.Description,
                                                           localized.Description,
                                                           localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(category,
                                                           x => x.MetaKeywords,
                                                           localized.MetaKeywords,
                                                           localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(category,
                                                           x => x.MetaDescription,
                                                           localized.MetaDescription,
                                                           localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(category,
                                                           x => x.MetaTitle,
                                                           localized.MetaTitle,
                                                           localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(category,
                                                           x => x.SeName,
                                                           localized.SeName,
                                                           localized.LanguageId);
            }
        }

        [NonAction]
        private void UpdatePictureSeoNames(Category category)
        {
            var picture = _pictureService.GetPictureById(category.PictureId);
            if (picture != null)
                _pictureService.SetSeoFilename(picture.Id, _pictureService.GetPictureSeName(category.Name));
        }

        [NonAction]
        private void PrepareTemplatesModel(CategoryModel model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            var templates = _categoryTemplateService.GetAllCategoryTemplates();
            foreach (var template in templates)
            {
                model.AvailableCategoryTemplates.Add(new SelectListItem()
                {
                    Text = template.Name,
                    Value = template.Id.ToString()
                });
            }
        }

        #endregion
        
        #region List / tree

        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        public ActionResult List()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var categories = _categoryService.GetAllCategories(0, _adminAreaSettings.GridPageSize, true);
            var gridModel = new GridModel<CategoryModel>
            {
                Data = categories.Select(x =>
                {
                    var model = x.ToModel();
                    model.Breadcrumb = x.GetCategoryBreadCrumb(_categoryService);
                    return model;
                }),
                Total = categories.TotalCount
            };
            return View(gridModel);
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult List(GridCommand command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var categories = _categoryService.GetAllCategories(command.Page - 1, command.PageSize, true);
            var gridModel = new GridModel<CategoryModel>
            {
                Data = categories.Select(x =>
                {
                    var model = x.ToModel();
                    model.Breadcrumb = x.GetCategoryBreadCrumb(_categoryService);
                    return model;
                }),
                Total = categories.TotalCount
            };
            return new JsonResult
            {
                Data = gridModel
            };
        }

        //ajax
        public ActionResult AllCategories(string text, int selectedId)
        {
            var categories = _categoryService.GetAllCategories(true);
            categories.Insert(0, new Category { Name = "[None]", Id = 0 });
            var selectList = new List<SelectListItem>();
            foreach (var c in categories)
                selectList.Add(new SelectListItem()
                    {
                         Value = c.Id.ToString(),
                         Text = c.GetCategoryBreadCrumb(_categoryService),
                         Selected = c.Id == selectedId
                    });

            //var selectList = new SelectList(categories, "Id", "Name", selectedId);
            return new JsonResult { Data = selectList, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        public ActionResult Tree()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var rootCategories = _categoryService.GetAllCategoriesByParentCategoryId(0, true);
            return View(rootCategories);
        }

        //ajax
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult TreeLoadChildren(TreeViewItem node)
        {
            var parentId = !string.IsNullOrEmpty(node.Value) ? Convert.ToInt32(node.Value) : 0;

            var children = _categoryService.GetAllCategoriesByParentCategoryId(parentId, true).Select(x =>
                new TreeViewItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString(),
                    LoadOnDemand = _categoryService.GetAllCategoriesByParentCategoryId(x.Id, true).Count > 0,
                    Enabled = true,
                    ImageUrl = Url.Content("~/Administration/Content/images/ico-content.png")
                });

            return new JsonResult { Data = children };
        }

        //ajax
        public ActionResult TreeDrop(int item, int destinationitem, string position)
        {
            var categoryItem = _categoryService.GetCategoryById(item);
            var categoryDestinationItem = _categoryService.GetCategoryById(destinationitem);

            switch (position)
            {
                case "over":
                    categoryItem.ParentCategoryId = categoryDestinationItem.Id;
                    break;
                case "before":
                    categoryItem.ParentCategoryId = categoryDestinationItem.ParentCategoryId;
                    categoryItem.DisplayOrder = categoryDestinationItem.DisplayOrder - 1;
                    break;
                case "after":
                    categoryItem.ParentCategoryId = categoryDestinationItem.ParentCategoryId;
                    categoryItem.DisplayOrder = categoryDestinationItem.DisplayOrder + 1;
                    break;
            }

            _categoryService.UpdateCategory(categoryItem);

            return Json(new { success = true });
        }

        #endregion

        #region Create / Edit / Delete

        public ActionResult Create()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var model = new CategoryModel();
            //parent categories
            model.ParentCategories = new List<DropDownItem> { new DropDownItem { Text = "[None]", Value = "0" } };
            //locales
            AddLocales(_languageService, model.Locales);
            //templates
            PrepareTemplatesModel(model);
            //discounts
            PrepareDiscountModel(model, null, true);
            //default values
            model.PageSize = 4;
            model.Published = true;

            model.AllowCustomersToSelectPageSize = true;            
            model.PageSizeOptions = _catalogSettings.DefaultCategoryPageSizeOptions;

            return View(model);
        }

        [HttpPost, FormValueExists("save", "save-continue", "continueEditing")]
        public ActionResult Create(CategoryModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            if (ModelState.IsValid)
            {
                var category = model.ToEntity();
                category.CreatedOnUtc = DateTime.UtcNow;
                category.UpdatedOnUtc = DateTime.UtcNow;
                _categoryService.InsertCategory(category);
                //locales
                UpdateLocales(category, model);
                //disounts
                var allDiscounts = _discountService.GetAllDiscounts(DiscountType.AssignedToCategories, true);
                foreach (var discount in allDiscounts)
                {
                    if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
                        category.AppliedDiscounts.Add(discount);
                }
                _categoryService.UpdateCategory(category);
                //update picture seo file name
                UpdatePictureSeoNames(category);

                //activity log
                _customerActivityService.InsertActivity("AddNewCategory", _localizationService.GetResource("ActivityLog.AddNewCategory"), category.Name);

                SuccessNotification(_localizationService.GetResource("Admin.Catalog.Categories.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = category.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            //templates
            PrepareTemplatesModel(model);
            //parent categories
            model.ParentCategories = new List<DropDownItem> { new DropDownItem { Text = "[None]", Value = "0" } };
            if (model.ParentCategoryId > 0)
            {
                var parentCategory = _categoryService.GetCategoryById(model.ParentCategoryId);
                if (parentCategory != null && !parentCategory.Deleted)
                    model.ParentCategories.Add(new DropDownItem { Text = parentCategory.GetCategoryBreadCrumb(_categoryService), Value = parentCategory.Id.ToString() });
                else
                    model.ParentCategoryId = 0;
            }
            //discounts
            PrepareDiscountModel(model, null, true);
            return View(model);
        }

        public ActionResult Edit(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var category = _categoryService.GetCategoryById(id);
            if (category == null || category.Deleted) 
                //No category found with the specified id
                return RedirectToAction("List");

            var model = category.ToModel();
            //parent categories
            model.ParentCategories = new List<DropDownItem> { new DropDownItem { Text = "[None]", Value = "0" } };
            if (model.ParentCategoryId > 0)
            {
                var parentCategory = _categoryService.GetCategoryById(model.ParentCategoryId);
                if (parentCategory != null && !parentCategory.Deleted)
                    model.ParentCategories.Add(new DropDownItem { Text = parentCategory.GetCategoryBreadCrumb(_categoryService), Value = parentCategory.Id.ToString() });
                else
                    model.ParentCategoryId = 0;
            }
            //locales
            AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = category.GetLocalized(x => x.Name, languageId, false, false);
                locale.Description = category.GetLocalized(x => x.Description, languageId, false, false);
                locale.MetaKeywords = category.GetLocalized(x => x.MetaKeywords, languageId, false, false);
                locale.MetaDescription = category.GetLocalized(x => x.MetaDescription, languageId, false, false);
                locale.MetaTitle = category.GetLocalized(x => x.MetaTitle, languageId, false, false);
                locale.SeName = category.GetLocalized(x => x.SeName, languageId, false, false);
            });
            //templates
            PrepareTemplatesModel(model);
            //discounts
            PrepareDiscountModel(model, category, false);

            return View(model);
        }

        [HttpPost, FormValueExists("save", "save-continue", "continueEditing")]
        public ActionResult Edit(CategoryModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var category = _categoryService.GetCategoryById(model.Id);
            if (category == null || category.Deleted)
                //No category found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                category = model.ToEntity(category);
                category.UpdatedOnUtc = DateTime.UtcNow;
                _categoryService.UpdateCategory(category);
                //locales
                UpdateLocales(category, model);
                //discounts
                var allDiscounts = _discountService.GetAllDiscounts(DiscountType.AssignedToCategories, true);
                foreach (var discount in allDiscounts)
                {
                    if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
                    {
                        //new role
                        if (category.AppliedDiscounts.Where(d => d.Id == discount.Id).Count() == 0)
                            category.AppliedDiscounts.Add(discount);
                    }
                    else
                    {
                        //removed role
                        if (category.AppliedDiscounts.Where(d => d.Id == discount.Id).Count() > 0)
                            category.AppliedDiscounts.Remove(discount);
                    }
                }
                _categoryService.UpdateCategory(category);
                //update picture seo file name
                UpdatePictureSeoNames(category);

                //activity log
                _customerActivityService.InsertActivity("EditCategory", _localizationService.GetResource("ActivityLog.EditCategory"), category.Name);

                SuccessNotification(_localizationService.GetResource("Admin.Catalog.Categories.Updated"));
                return continueEditing ? RedirectToAction("Edit", category.Id) : RedirectToAction("List");
            }


            //If we got this far, something failed, redisplay form
            //parent categories
            model.ParentCategories = new List<DropDownItem> { new DropDownItem { Text = "[None]", Value = "0" } };
            if (model.ParentCategoryId > 0)
            {
                var parentCategory = _categoryService.GetCategoryById(model.ParentCategoryId);
                if (parentCategory != null && !parentCategory.Deleted)
                    model.ParentCategories.Add(new DropDownItem { Text = parentCategory.GetCategoryBreadCrumb(_categoryService), Value = parentCategory.Id.ToString() });
                else
                    model.ParentCategoryId = 0;
            }
            //templates
            PrepareTemplatesModel(model);
            //discounts
            PrepareDiscountModel(model, category, true);
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var category = _categoryService.GetCategoryById(id);
            if (category == null)
                //No category found with the specified id
                return RedirectToAction("List");

            _categoryService.DeleteCategory(category);

            //activity log
            _customerActivityService.InsertActivity("DeleteCategory", _localizationService.GetResource("ActivityLog.DeleteCategory"), category.Name);

            SuccessNotification(_localizationService.GetResource("Admin.Catalog.Categories.Deleted"));
            return RedirectToAction("List");
        }
        
        [NonAction]
        private void PrepareDiscountModel(CategoryModel model, Category category, bool excludeProperties)
        {
            if (model == null)
                throw new ArgumentNullException("model");
            
            var discounts = _discountService.GetAllDiscounts(DiscountType.AssignedToCategories, true);
            model.AvailableDiscounts = discounts.ToList();

            if (!excludeProperties)
            {
                model.SelectedDiscountIds = category.AppliedDiscounts.Select(d => d.Id).ToArray();
            }
        }

        #endregion

        #region Export / Import

        public ActionResult ExportXml()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            try
            {
                var fileName = string.Format("categories_{0}.xml", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
                var xml = _exportManager.ExportCategoriesToXml();
                return new XmlDownloadResult(xml, fileName);
            }
            catch (Exception exc)
            {
                ErrorNotification(exc);
                return RedirectToAction("List");
            }
        }

        #endregion

        #region Products

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult ProductList(GridCommand command, int categoryId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var productCategories = _categoryService.GetProductCategoriesByCategoryId(categoryId, true);
            var productCategoriesModel = productCategories
                .Select(x =>
                {
                    return new CategoryModel.CategoryProductModel()
                    {
                        Id = x.Id,
                        CategoryId = x.CategoryId,
                        ProductId = x.ProductId,
                        ProductName = _productService.GetProductById(x.ProductId).Name,
                        IsFeaturedProduct = x.IsFeaturedProduct,
                        DisplayOrder1 = x.DisplayOrder
                    };
                })
                .ToList();

            var model = new GridModel<CategoryModel.CategoryProductModel>
            {
                Data = productCategoriesModel,
                Total = productCategoriesModel.Count
            };

            return new JsonResult
            {
                Data = model
            };
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult ProductUpdate(GridCommand command, CategoryModel.CategoryProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var productCategory = _categoryService.GetProductCategoryById(model.Id);
            if (productCategory == null)
                throw new ArgumentException("No product category mapping found with the specified id");

            productCategory.IsFeaturedProduct = model.IsFeaturedProduct;
            productCategory.DisplayOrder = model.DisplayOrder1;
            _categoryService.UpdateProductCategory(productCategory);

            return ProductList(command, productCategory.CategoryId);
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult ProductDelete(int id, GridCommand command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var productCategory = _categoryService.GetProductCategoryById(id);
            if (productCategory == null)
                throw new ArgumentException("No product category mapping found with the specified id");

            var categoryId = productCategory.CategoryId;
            _categoryService.DeleteProductCategory(productCategory);

            return ProductList(command, categoryId);
        }

        public ActionResult ProductAddPopup(int categoryId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            IList<int> filterableSpecificationAttributeOptionIds = null;
            var products = _productService.SearchProducts(0, 0, null, null, null, 0, string.Empty, false,
                _workContext.WorkingLanguage.Id, new List<int>(),
                ProductSortingEnum.Position, 0, _adminAreaSettings.GridPageSize,
                false, out filterableSpecificationAttributeOptionIds, true);

            var model = new CategoryModel.AddCategoryProductModel();
            model.Products = new GridModel<ProductModel>
            {
                Data = products.Select(x => x.ToModel()),
                Total = products.TotalCount
            };
            //categories
            model.AvailableCategories.Add(new SelectListItem() { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            foreach (var c in _categoryService.GetAllCategories(true))
                model.AvailableCategories.Add(new SelectListItem() { Text = c.GetCategoryNameWithPrefix(_categoryService), Value = c.Id.ToString() });

            //manufacturers
            model.AvailableManufacturers.Add(new SelectListItem() { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            foreach (var m in _manufacturerService.GetAllManufacturers(true))
                model.AvailableManufacturers.Add(new SelectListItem() { Text = m.Name, Value = m.Id.ToString() });

            return View(model);
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult ProductAddPopupList(GridCommand command, CategoryModel.AddCategoryProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var gridModel = new GridModel();
            IList<int> filterableSpecificationAttributeOptionIds = null;
            var products = _productService.SearchProducts(model.SearchCategoryId,
                model.SearchManufacturerId, null, null, null, 0, model.SearchProductName, false,
                _workContext.WorkingLanguage.Id, new List<int>(),
                ProductSortingEnum.Position, command.Page - 1, command.PageSize,
                false, out filterableSpecificationAttributeOptionIds, true);
            gridModel.Data = products.Select(x => x.ToModel());
            gridModel.Total = products.TotalCount;
            return new JsonResult
            {
                Data = gridModel
            };
        }
        
        [HttpPost]
        [FormValueRequired("save")]
        public ActionResult ProductAddPopup(string btnId, string formId, CategoryModel.AddCategoryProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            if (model.SelectedProductIds != null)
            {
                foreach (int id in model.SelectedProductIds)
                {
                    var product = _productService.GetProductById(id);
                    if (product != null)
                    {
                        var existingProductCategories = _categoryService.GetProductCategoriesByCategoryId(model.CategoryId);
                        if (existingProductCategories.FindProductCategory(id, model.CategoryId) == null)
                        {
                            _categoryService.InsertProductCategory(
                                new ProductCategory()
                                {
                                    CategoryId = model.CategoryId,
                                    ProductId = id,
                                    IsFeaturedProduct = false,
                                    DisplayOrder = 1
                                });
                        }
                    }
                }
            }

            ViewBag.RefreshPage = true;
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;
            model.Products = new GridModel<ProductModel>();
            return View(model);
        }

        #endregion
    }
}
