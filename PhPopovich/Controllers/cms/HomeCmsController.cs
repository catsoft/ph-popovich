﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhPopovich.EntityFrameworkPaginateCore;
using PhPopovich.Models;
using PhPopovich.StaticHelpers;
using PhPopovich.ViewModels.Cms;

namespace PhPopovich.Controllers.cms
{
    public class HomeCmsController : CommonController
    {
        private readonly IWebHostEnvironment _appEnvironment;

        public HomeCmsController(Context context, IWebHostEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            Context = context;
        }

        [Authorize]
        public async Task<IActionResult> GetList(string type, int page = 0, string sorted = null, string filter = null)
        {
            var t = GetTypeByName(type);
            if (CheckIsSingle(t))
                return RedirectToAction("EditFirst", new { type });

            ViewBag.Type = t;
            ViewBag.NameType = type;
            ViewBag.Page = page;
            dynamic dynamicObject = Activator.CreateInstance(t);
            var set = Context.GetDbSet(dynamicObject);

            var includeSets = Provider(ReflectionHelper.InsertInclude<ShowTitleAttribute>(set, t), dynamicObject);

            var localFilter = filter;
            if (string.IsNullOrEmpty(localFilter))
            {
                localFilter = CookieHelper.GetFilter(HttpContext);
            }
            else
            {
                CookieHelper.SetFilter(localFilter, HttpContext);
            }

            var pageResult = await Paginate(dynamicObject, includeSets, t, page + 1, 20, null, localFilter);
            var result = ProviderPage(dynamicObject, pageResult);
            var sortedSet = result.Results;
            ViewBag.PageCount = result.PageCount;

            var cmsModel = Context.CmsModels.FirstOrDefault(w => w.Class == type);
            if (cmsModel != null)
            {
                cmsModel.NewCount = 0;
                Context.SaveChanges();
            }

            return View(sortedSet);
        }

        private bool CheckIsSingle(Type type)
        {
            return type.GetCustomAttribute<SingleObjectAttribute>() != null;
        }

        // ReSharper disable once UnusedParameter.Local
        private IQueryable<T> Provider<T>(IQueryable<IEntity> list, T dynamicObject) where T : ISortFilterEntity<T>
        {
            return list.Cast<T>();
        }

        // ReSharper disable once UnusedParameter.Local
        private Page<T> ProviderPage<T>(T objectType, Page<T> page) where T : ISortFilterEntity<T>
        {
            return page;
        }

        public Task<Page<T>> Paginate<T>(T objectType, IQueryable<T> list, Type type, int pageNumber, int pageSize,
            string sorteds = null, string filter = null)
            where T : ISortFilterEntity<T>
        {
            var filters = objectType.GetDefaultFilters(filter) ?? new Filters<T>();

            var sorted = objectType.GetDefaultSorted() ?? new Sorts<T>();
            //TODO Сортировка отвалилась чего-то
            //sorted.Add(true, arg => (arg as IEntity).DateCreated, true);

            return list.PaginateAsync(pageNumber, pageSize, sorted, filters);
        }

        #region Воин начало

        public enum DirectionSort
        {
            Asc,
            Desc,
        }

        public IQueryable<IEntity> AddOrder<T, TE>(IQueryable<IEntity> list, T typeObject, TE propertyTypeObject,
            string propertyName, DirectionSort desc) where T : IEntity
        {
            var orderExpression = GetExpression(typeObject, propertyTypeObject, propertyName);
            IQueryable<T> c;

            switch (desc)
            {
                case DirectionSort.Asc:
                    c = ((IQueryable<T>)list).OrderBy(orderExpression);
                    break;
                case DirectionSort.Desc:
                    c = ((IQueryable<T>)list).OrderByDescending(orderExpression);
                    break;
                default:
                    c = ((IQueryable<T>)list).OrderBy(orderExpression);
                    break;
            }

            return c as IQueryable<IEntity>;
        }

        public Expression<Func<T, TE>> GetExpression<T, TE>(T typeObject, TE propertyTypeObject, string propertyName)
        {
            var parameter = Expression.Parameter(typeof(T), "member");
            var member = Expression.Property(parameter, propertyName);
            var lambda = Expression.Lambda<Func<T, TE>>(member, parameter);
            return lambda;
        }

        #endregion

        [Authorize]
        public IActionResult Delete(string type, Guid id)
        {
            var t = GetTypeByName(type);

            if (CheckIsSingle(t))
                return RedirectToAction("EditFirst", new { type });

            dynamic dynamicObject = Activator.CreateInstance(t);
            Delete(dynamicObject, id);

            return RedirectToAction("GetList", new { type });
        }

        // ReSharper disable once UnusedParameter.Local
        private void Delete<T>(T dynamicObject, Guid id) where T : Entity<T>
        {
            var set = Context.Set<T>().AsQueryable();

            var first = set.FirstOrDefault(w => w.Id == id);

            if (first == null) return;

            Context.Remove(first);
            Context.SaveChanges();
        }

        [Authorize]
        public IActionResult Details(string type, Guid id)
        {
            var t = GetTypeByName(type);
            dynamic dynamicObject = Activator.CreateInstance(t);

            ViewBag.Type = t;
            ViewBag.NameType = type;

            return View(GetObject(dynamicObject, id));
        }

        private T GetObject<T>(T type, Guid id) where T : Entity<T>
        {
            var set = Context.Set<T>().AsQueryable();

            var classes = type.GetType().GetProperties().Where(w =>
                (w.PropertyType.IsClass || w.PropertyType.IsArray) && 
                w.PropertyType != Types.String && 
                w.GetCustomAttribute<NotMappedAttribute>() == null);

            var classesSet = classes.Aggregate(set, (current, property) =>
            {
                var ccc = current.Include(property.Name);

                if (property.PropertyType.IsArray ||
                    property.PropertyType.GetInterfaces().Any(w => w == Types.Enumerable))
                {
                    var types = property.PropertyType.GenericTypeArguments;

                    foreach (var type2 in types)
                    {
                        ccc = type2.GetProperties()
                            .Where(w => (w.PropertyType.IsClass || w.PropertyType.IsArray) && w.PropertyType != Types.String)
                            .Aggregate(ccc, (cc, pro) =>
                            {
                                var a = cc.Include($"{property.Name}.{pro.Name}");
                                return a;
                            });
                    }
                }

                return ccc;
            });

            return classesSet.FirstOrDefault(w => w.Id == id);
        }

        [HttpGet]
        [Authorize]
        public IActionResult Create(string type)
        {
            var t = GetTypeByName(type);

            if (CheckIsSingle(t))
                return RedirectToAction("EditFirst", new { type });

            dynamic dynamicObject = Activator.CreateInstance(t);
            ViewBag.NameType = type;

            return View(dynamicObject);
        }

        [HttpGet]
        [Authorize]
        public IActionResult EditFirst(string type)
        {
            var t = GetTypeByName(type);
            dynamic dynamicObject = Activator.CreateInstance(t);
            var first = GetObjectToFirstEdit(dynamicObject);

            if (first == null) return NotFound();

            return RedirectToAction("Edit", new { type, id = first.Id });
        }

        // ReSharper disable once UnusedParameter.Local
        private T GetObjectToFirstEdit<T>(T type) where T : Entity<T>
        {
            var first = Context.Set<T>().FirstOrDefault();
            return first;
        }

        [HttpGet]
        [Authorize]
        public IActionResult Edit(string type, Guid id)
        {
            var t = GetTypeByName(type);
            dynamic dynamicObject = Activator.CreateInstance(t);
            ViewBag.NameType = type;
            ViewBag.Type = t;

            return View(GetObject(dynamicObject, id));
        }

        [HttpPost]
        [Authorize]
        public IActionResult Edit()
        {
            var keys = Request.Form.Keys?.Select(w => w.ToLower()).ToList() ?? new List<string>();

            if (!keys.Contains("type") || !keys.Contains("id")) RedirectToAction("GetList", new { type = "Order" });

            var typeName = Request.Form["type"];
            var id = Request.Form["id"];
            var type = GetTypeByName(typeName);
            var properties = type.GetProperties();
            dynamic newObject = Activator.CreateInstance(type);
            var editObject = GetObject(newObject, Guid.Parse(id));

            var existsProperties = properties.Where(w =>
            {
                var propertyName = w.Name.ToLower();
                return propertyName != "id" && keys.Contains(propertyName);
            });

            foreach (var key in existsProperties)
            {
                var value = Request.Form[key.Name];
                var strValue = value.ToString();

                //TODO Костыль ебаный
                if (key.PropertyType == Types.Bool && strValue == "true,false")
                {
                    strValue = "true";
                }

                var typeConvert = key.PropertyType;
                if (typeConvert.IsGenericType && typeConvert.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    if (!string.IsNullOrEmpty(value.FirstOrDefault()))
                    {
                        typeConvert = Nullable.GetUnderlyingType(typeConvert);

                        object changedType;

                        if (typeConvert == typeof(Guid))
                        {
                            changedType = Guid.Parse(strValue);
                        }
                        else
                        {
                            changedType = Convert.ChangeType(strValue, typeConvert);
                        }
                            
                        key.SetValue(editObject, changedType);
                    }
                }
                else
                {
                    var changedType = Convert.ChangeType(strValue, key.PropertyType);

                    key.SetValue(editObject, changedType);
                }
            }

            Context.Update(editObject);
            Context.SaveChanges();

            return CheckIsSingle(type)
                ? RedirectToAction("EditFirst", new { type = typeName })
                : RedirectToAction("GetList", new { type = typeName });
        }

        [HttpPost]
        [Authorize]
        public IActionResult Create()
        {
            var keys = Request.Form.Keys?.Select(w => w.ToLower()).ToList() ?? new List<string>();

            if (!keys.Contains("type")) RedirectToAction("GetList", new { type = "Order" });

            var typeName = Request.Form["type"];
            var type = GetTypeByName(typeName);

            if (CheckIsSingle(type))
                return RedirectToAction("EditFirst", new { type });

            var properties = type.GetProperties();
            var newObject = Activator.CreateInstance(type);

            var existsProperties = properties.Where(w =>
            {
                var propertyName = w.Name.ToLower();
                return keys.Contains(propertyName);
            });

            foreach (var key in existsProperties)
            {
                var value = Request.Form[key.Name];

                var strValue = value.ToString();

                //TODO Костыль ебаный
                if (key.PropertyType == Types.Bool && strValue == "true,false")
                {
                    strValue = "true";
                }

                var typeConvert = key.PropertyType;
                if (typeConvert.IsGenericType && typeConvert.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    if (!string.IsNullOrEmpty(value.FirstOrDefault()))
                    {
                        typeConvert = Nullable.GetUnderlyingType(typeConvert);

                        object changedType = null;

                        if (typeConvert == typeof(Guid))
                        {
                            changedType = Guid.Parse(strValue);
                        }
                        else
                        {
                            changedType = Convert.ChangeType(strValue, typeConvert);
                        }

                        key.SetValue(newObject, changedType);
                    }
                }
                else
                {
                    var changedType = Convert.ChangeType(strValue, key.PropertyType);

                    key.SetValue(newObject, changedType);
                }
            }

            Context.Add(newObject);
            Context.SaveChanges();

            return RedirectToAction("GetList", new { type = typeName });
        }

        #region FileWork

        [HttpPost]
        public Guid AddFile(IFormFile uploadedFile)
        {
            if (uploadedFile != null)
            {
                var file = new FileModel
                {
                    Name = uploadedFile.FileName,
                    FileType = uploadedFile.ContentType,
                };
                Context.Files.Add(file);
                Context.SaveChanges();

                var indexOf = uploadedFile.ContentType.IndexOf('/');
                var extension = indexOf == -1 ? "file" : uploadedFile.ContentType.Substring(indexOf + 1);

                string path = "/Files/" + file.Id + "." + extension;
                // сохраняем файл в папку Files в каталоге wwwroot
                using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                {
                    uploadedFile.CopyTo(fileStream);
                }

                file.Path = path;
                Context.SaveChanges();

                return file.Id;
            }

            return Guid.Empty;
        }

        [HttpGet]
        public IActionResult AddFile(Guid id, Guid idFileProperty)
        {
            var fileViewModel = new FileViewModel()
            {
                Id = idFileProperty,
                FileId = id,
            };

            return View("File", fileViewModel);
        }

        public IActionResult RemoveFile(Guid id, Guid idFileProperty)
        {
            var fileViewModel = new FileViewModel()
            {
                Id = idFileProperty
            };

            var file = Context.Files.FirstOrDefault(w => w.Id == id);
            if (file != null)
            {
                Context.Files.Remove(file);
                Context.SaveChanges();
            }

            return View("File", fileViewModel);
        }

        #endregion

        #region ImageWork

        [HttpPost]
        public Guid AddImage(IFormFile uploadedImage)
        {
            if (uploadedImage != null)
            {
                var indexOf = uploadedImage.ContentType.IndexOf('/');
                var extension = indexOf == -1 ? "file" : uploadedImage.ContentType.Substring(indexOf + 1);

                var image = new ImageModel
                {
                    Title = uploadedImage.FileName,
                    ImageType = uploadedImage.ContentType,
                    Extension = extension,
                };

                Context.Images.Add(image);
                Context.SaveChanges();

                var path = "/UploadImages/" + image.Id + "." + extension;
                using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                {
                    uploadedImage.CopyTo(fileStream);
                }

                image.Url = path;
                Context.SaveChanges();

                return image.Id;
            }

            return Guid.Empty;
        }

        [HttpGet]
        public IActionResult AddImage(Guid id, string idImageProperty)
        {
            var image = Context.Images.FirstOrDefault(w => w.Id == id);

            var imageViewModel = new ImageViewModel()
            {
                ImageId = id,
                PropertyName = idImageProperty,
                Url = image?.Url,
            };

            return View("Image", imageViewModel);
        }

        [HttpPost]
        public IActionResult RemoveImage(Guid id)
        {
            var imageViewModel = new ImageViewModel();

            var image = Context.Images.FirstOrDefault(w => w.Id == id);
            if (image != null)
            {
                Context.Images.Remove(image);
                Context.SaveChanges();
            }

            return View("Image", imageViewModel);
        }

        #endregion


        //Регион для добавления изображения в связи один ко многим

        #region ManyImageWork

        [HttpPost]
        public Guid AddOneToManyImageModel(IFormFile uploadedImageModel, string propertyName, Guid id)
        {
            if (uploadedImageModel != null && !string.IsNullOrEmpty(propertyName))
            {
                var indexOf = uploadedImageModel.ContentType.IndexOf('/');
                var extension = indexOf == -1 ? "file" : uploadedImageModel.ContentType.Substring(indexOf + 1);

                var image = new ImageModel
                {
                    Title = uploadedImageModel.FileName,
                    ImageType = uploadedImageModel.ContentType,
                    Extension = extension,
                };

                var property = Types.Image.GetProperty(propertyName);
                property.SetValue(image, id);

                Context.Images.Add(image);
                Context.SaveChanges();

                var path = "/UploadImages/" + image.Id + "." + extension;
                using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                {
                    uploadedImageModel.CopyTo(fileStream);
                }

                image.Url = path;
                Context.SaveChanges();

                return image.Id;
            }

            return Guid.Empty;
        }

        [HttpGet]
        public IActionResult AddOneToManyImageModel(Guid id, string propertyName)
        {
            var image = Context.Images.FirstOrDefault(w => w.Id == id);

            var imageViewModel = new OneToManySingleImage()
            {
                Id = id,
                LinkPropertyName = propertyName,
                Url = image?.Url,
            };

            return View("OneToManySingleImage", imageViewModel);
        }

        public IActionResult RemoveOneToManyImage(Guid id)
        {
            var image = Context.Images.FirstOrDefault(w => w.Id == id);
            if (image != null)
            {
                Context.Images.Remove(image);
                Context.SaveChanges();
            }

            return new OkResult();
        }

        #endregion

        private Type GetTypeByName(string name)
        {
            return Type.GetType(App.Name + ".Models." + name);
        }
    }
}